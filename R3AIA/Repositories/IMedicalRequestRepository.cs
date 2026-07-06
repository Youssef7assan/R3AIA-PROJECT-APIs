using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using static R3AIA.Models.Enums;

namespace R3AIA.Repositories;

public interface IMedicalRequestRepository
{
	Task<IEnumerable<MedicalRequest>> GetRequestsForDoctorAsync(string doctorUserId);
	Task<IEnumerable<MedicalRequest>> GetAcceptedRequestsForDoctorAsync(string doctorUserId);
	Task<MedicalRequest> CreateRequestAsync(int patientId, int specialtyId, string description, string? medicalImages);
	
	/// <summary>
	/// جلب التفاصيل الكاملة لطلب محدد (للطبيب)
	/// </summary>
	Task<MedicalRequest?> GetRequestDetailAsync(int requestId, string doctorUserId);
	
	/// <summary>
	/// رد الدكتور على طلب مريض (قبول + تحديد ميعاد)
	/// </summary>
	Task<MedicalRequest?> RespondToRequestAsync(int requestId, string doctorUserId, DateTime appointmentDate, string? doctorNotes);
	
	/// <summary>
	/// جلب طلبات المريض الشخصية (كل طلباته)
	/// </summary>
	Task<IEnumerable<MedicalRequest>> GetMyRequestsAsync(string patientUserId);
	
	/// <summary>
	/// إلغاء طلب من قبل المريض
	/// </summary>
	Task<MedicalRequest?> CancelRequestAsync(int requestId, string patientUserId, string? cancellationReason);
	
	/// <summary>
	/// تأكيد إتمام الكشف من قبل المريض
	/// </summary>
	Task<MedicalRequest?> CompleteRequestAsync(int requestId, string patientUserId);
	
	/// <summary>
	/// إلغاء طلب من قبل الدكتور
	/// </summary>
	Task<MedicalRequest?> CancelRequestByDoctorAsync(int requestId, string doctorUserId, string? cancellationReason);
}

public class MedicalRequestRepository : IMedicalRequestRepository
{
	private readonly AppDbContext _context;

	public MedicalRequestRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<IEnumerable<MedicalRequest>> GetRequestsForDoctorAsync(string doctorUserId)
	{
		var doctor = await _context.Doctors
			.FirstOrDefaultAsync(d => d.IdentityUserId == doctorUserId);

		if (doctor is null)
		{
			return Enumerable.Empty<MedicalRequest>();
		}

		return await _context.MedicalRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Where(r =>
				r.RequestStatus == RequestStatus.Pending &&
				r.Patient.GovernorateId == doctor.GovernorateId &&
				r.SpecialtyId == doctor.SpecialtyId)
			.ToListAsync();
	}

	public async Task<IEnumerable<MedicalRequest>> GetAcceptedRequestsForDoctorAsync(string doctorUserId)
	{
		var doctor = await _context.Doctors
			.FirstOrDefaultAsync(d => d.IdentityUserId == doctorUserId);

		if (doctor is null)
		{
			return Enumerable.Empty<MedicalRequest>();
		}

		return await _context.MedicalRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Where(r =>
				(r.RequestStatus == RequestStatus.Accepted || r.RequestStatus == RequestStatus.Completed) &&
				r.DoctorId == doctor.Id)
			.OrderByDescending(r => r.AppointmentDate)
			.ToListAsync();
	}

	public async Task<MedicalRequest> CreateRequestAsync(int patientId, int specialtyId, string description, string? medicalImages)
	{
		var request = new MedicalRequest
		{
			PatientId = patientId,
			SpecialtyId = specialtyId,
			Description = description,
			RequestStatus = RequestStatus.Pending,
			MedicalImages = medicalImages,
			HasAttachments = !string.IsNullOrEmpty(medicalImages)
		};

		_context.MedicalRequests.Add(request);
		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<MedicalRequest?> GetRequestDetailAsync(int requestId, string doctorUserId)
	{
		// 1. جلب معلومات الطبيب
		var doctor = await _context.Doctors
			.FirstOrDefaultAsync(d => d.IdentityUserId == doctorUserId);
		
		if (doctor is null) return null;
		
		// 2. جلب الطلب مع جميع البيانات المرتبطة
		var request = await _context.MedicalRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.Governorate)
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Include(r => r.Specialty)
			.Where(r => 
				r.Id == requestId &&
				r.Patient.GovernorateId == doctor.GovernorateId &&
				r.SpecialtyId == doctor.SpecialtyId)
			.FirstOrDefaultAsync();
		
		return request;
	}

	public async Task<MedicalRequest?> RespondToRequestAsync(int requestId, string doctorUserId, DateTime appointmentDate, string? doctorNotes)
	{
		// 1. جلب معلومات الدكتور
		var doctor = await _context.Doctors
			.FirstOrDefaultAsync(d => d.IdentityUserId == doctorUserId);
		
		if (doctor is null) return null;
		
		// 2. جلب الطلب والتحقق من الصلاحيات
		var request = await _context.MedicalRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.User)
			.Include(r => r.Specialty)
			.Where(r => 
				r.Id == requestId &&
				r.Patient.GovernorateId == doctor.GovernorateId &&
				r.SpecialtyId == doctor.SpecialtyId &&
				r.RequestStatus == RequestStatus.Pending)
			.FirstOrDefaultAsync();
		
		if (request is null) return null;
		
		// 3. تحديث الطلب
		request.DoctorId = doctor.Id;
		request.AppointmentDate = appointmentDate;
		request.DoctorNotes = doctorNotes;
		request.RequestStatus = RequestStatus.Accepted;
		
		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<IEnumerable<MedicalRequest>> GetMyRequestsAsync(string patientUserId)
	{
		var patient = await _context.Patients
			.FirstOrDefaultAsync(p => p.IdentityUserId == patientUserId);
		
		if (patient is null) return Enumerable.Empty<MedicalRequest>();
		
		return await _context.MedicalRequests
			.Include(r => r.Specialty)
			.Include(r => r.Doctor)
			.Where(r => r.PatientId == patient.Id)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();
	}

	public async Task<MedicalRequest?> CancelRequestAsync(int requestId, string patientUserId, string? cancellationReason)
	{
		var patient = await _context.Patients
			.FirstOrDefaultAsync(p => p.IdentityUserId == patientUserId);
		
		if (patient is null) return null;
		
		// جلب الطلب مع بيانات الدكتور إذا كان موجود
		var request = await _context.MedicalRequests
			.Include(r => r.Doctor)
				.ThenInclude(d => d.User)
			.Include(r => r.Specialty)
			.Where(r => 
				r.Id == requestId &&
				r.PatientId == patient.Id &&
				(r.RequestStatus == RequestStatus.Pending || r.RequestStatus == RequestStatus.Accepted))
			.FirstOrDefaultAsync();
		
		if (request is null) return null;
		
		// حفظ سبب الإلغاء في DoctorNotes (أو يمكن إضافة حقل جديد)
		if (!string.IsNullOrEmpty(cancellationReason))
		{
			request.DoctorNotes = $"[إلغاء من المريض] {cancellationReason}";
		}
		
		request.RequestStatus = RequestStatus.Cancelled;
		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<MedicalRequest?> CancelRequestByDoctorAsync(int requestId, string doctorUserId, string? cancellationReason)
	{
		var doctor = await _context.Doctors
			.FirstOrDefaultAsync(d => d.IdentityUserId == doctorUserId);
		
		if (doctor is null) return null;
		
		// جلب الطلب مع بيانات المريض
		var request = await _context.MedicalRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.User)
			.Include(r => r.Specialty)
			.Where(r => 
				r.Id == requestId &&
				r.DoctorId == doctor.Id &&
				r.RequestStatus == RequestStatus.Accepted)
			.FirstOrDefaultAsync();
		
		if (request is null) return null;
		
		// حفظ سبب الإلغاء
		if (!string.IsNullOrEmpty(cancellationReason))
		{
			request.DoctorNotes = $"[إلغاء من الدكتور] {cancellationReason}";
		}
		
		request.RequestStatus = RequestStatus.Cancelled;
		request.DoctorId = null; // فك الربط مع الدكتور
		request.AppointmentDate = null;
		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<MedicalRequest?> CompleteRequestAsync(int requestId, string patientUserId)
	{
		var patient = await _context.Patients
			.FirstOrDefaultAsync(p => p.IdentityUserId == patientUserId);
		
		if (patient is null) return null;
		
		var request = await _context.MedicalRequests
			.Where(r => 
				r.Id == requestId &&
				r.PatientId == patient.Id &&
				r.RequestStatus == RequestStatus.Accepted)
			.FirstOrDefaultAsync();
		
		if (request is null) return null;
		
		request.RequestStatus = RequestStatus.Completed;
		await _context.SaveChangesAsync();
		return request;
	}
}


