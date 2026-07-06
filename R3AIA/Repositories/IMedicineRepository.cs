using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using static R3AIA.Models.Enums;

namespace R3AIA.Repositories;

public interface IMedicineRepository
{
	Task<MedicineRequest> CreateRequestAsync(int patientId, string imageUrl, bool needDelivery);
	Task<IEnumerable<MedicineRequest>> GetOpenRequestsForPharmacyAsync(string pharmacyUserId);
	
	/// <summary>
	/// قبول طلب دواء من قبل الصيدلية
	/// </summary>
	Task<MedicineRequest?> AcceptRequestAsync(int requestId, string pharmacyUserId, string? pharmacyNotes);
	
	/// <summary>
	/// جلب طلبات المريض الشخصية
	/// </summary>
	Task<IEnumerable<MedicineRequest>> GetMyRequestsAsync(string patientUserId);
	
	/// <summary>
	/// جلب مهام التوصيل المتاحة للمتطوعين
	/// </summary>
	Task<IEnumerable<MedicineRequest>> GetDeliveryTasksAsync(string volunteerUserId);
	
	/// <summary>
	/// متطوع يأخذ مهمة توصيل
	/// </summary>
	Task<MedicineRequest?> TakeDeliveryTaskAsync(int requestId, string volunteerUserId);
	
	/// <summary>
	/// تأكيد اكتمال التوصيل
	/// </summary>
	Task<MedicineRequest?> MarkAsDeliveredAsync(int requestId, string volunteerUserId);
}

public class MedicineRepository : IMedicineRepository
{
	private readonly AppDbContext _context;

	public MedicineRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<MedicineRequest> CreateRequestAsync(int patientId, string imageUrl, bool needDelivery)
	{
		var request = new MedicineRequest
		{
			PatientId = patientId,
			PrescriptionImage = imageUrl,
			NeedDelivery = needDelivery,
			RequestStatus = RequestStatus.Pending
		};

		_context.MedicineRequests.Add(request);
		await _context.SaveChangesAsync();

		// جلب الطلب مرة أخرى مع البيانات المرتبطة لضمان صحة الـ Mapping في الرد
		return await _context.MedicineRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.FirstAsync(r => r.Id == request.Id);
	}

	public async Task<IEnumerable<MedicineRequest>> GetOpenRequestsForPharmacyAsync(string pharmacyUserId)
	{
		var pharmacy = await _context.Pharmacies
			.FirstOrDefaultAsync(p => p.IdentityUserId == pharmacyUserId);

		if (pharmacy is null)
		{
			return Enumerable.Empty<MedicineRequest>();
		}

		return await _context.MedicineRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Include(r => r.Patient)
				.ThenInclude(p => p.Governorate)
			.Where(r =>
				r.RequestStatus == RequestStatus.Pending &&
				r.Patient.GovernorateId == pharmacy.GovernorateId)
			.ToListAsync();
	}

	public async Task<MedicineRequest?> AcceptRequestAsync(int requestId, string pharmacyUserId, string? pharmacyNotes)
	{
		// 1. جلب معلومات الصيدلية
		var pharmacy = await _context.Pharmacies
			.FirstOrDefaultAsync(p => p.IdentityUserId == pharmacyUserId);
		
		if (pharmacy is null) return null;
		
		// 2. جلب الطلب والتحقق من الصلاحيات
		var request = await _context.MedicineRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.User)
			.Where(r => 
				r.Id == requestId &&
				r.Patient.GovernorateId == pharmacy.GovernorateId &&
				r.RequestStatus == RequestStatus.Pending)
			.FirstOrDefaultAsync();
		
		if (request is null) return null;
		
		// Double-check to prevent race condition
		if (request.PharmacyId != null || request.RequestStatus != RequestStatus.Pending)
			return null; // Already accepted by another pharmacy
		
		// 3. تحديث الطلب
		request.PharmacyId = pharmacy.Id;
		request.PharmacyNotes = pharmacyNotes;
		request.RequestStatus = RequestStatus.Fulfilled; // تم التوفير
		
		if (request.NeedDelivery)
		{
			var deliveryTask = new DeliveryTask
			{
				MedicineRequest = request,
				TaskStatus = DeliveryStatus.Available,
				CreatedAt = DateTime.Now
			};
			_context.DeliveryTasks.Add(deliveryTask);
			request.DeliveryStatus = DeliveryStatus.Available;
		}
		
		await _context.SaveChangesAsync();
		return request;
	}

	public async Task<IEnumerable<MedicineRequest>> GetMyRequestsAsync(string patientUserId)
	{
		var patient = await _context.Patients
			.FirstOrDefaultAsync(p => p.IdentityUserId == patientUserId);
		
		if (patient is null) return Enumerable.Empty<MedicineRequest>();
		
		return await _context.MedicineRequests
			.Include(r => r.Pharmacy)
			.Include(r => r.Volunteer)
			.Where(r => r.PatientId == patient.Id)
			.OrderByDescending(r => r.CreatedAt)
			.ToListAsync();
	}

	public async Task<IEnumerable<MedicineRequest>> GetDeliveryTasksAsync(string volunteerUserId)
	{
		var volunteer = await _context.Volunteers
			.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);
		
		if (volunteer is null) return Enumerable.Empty<MedicineRequest>();
		
		// البحث عن طلبات مكتملة من الصيدلية وتحتاج توصيل ولم يتم أخذها بعد
		// نبحث في MedicineRequests لكن نتأكد أنها غير موجودة في DeliveryTasks
		var takenRequestIds = await _context.DeliveryTasks
			.Select(dt => dt.MedicineRequestId)
			.ToListAsync();
		
		return await _context.MedicineRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Include(r => r.Patient)
				.ThenInclude(p => p.Governorate)
			.Include(r => r.Pharmacy)
			.Where(r => 
				r.NeedDelivery &&
				r.RequestStatus == RequestStatus.Fulfilled &&
				!takenRequestIds.Contains(r.Id) &&
				r.Patient.GovernorateId == volunteer.GovernorateId)
			.ToListAsync();
	}

	public async Task<MedicineRequest?> TakeDeliveryTaskAsync(int requestId, string volunteerUserId)
	{
		var volunteer = await _context.Volunteers
			.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);
		
		if (volunteer is null) return null;
		
		var request = await _context.MedicineRequests
			.Include(r => r.Patient)
				.ThenInclude(p => p.User)
			.Include(r => r.Patient)
				.ThenInclude(p => p.City)
			.Include(r => r.Patient)
				.ThenInclude(p => p.Governorate)
			.Include(r => r.Pharmacy)
			.Where(r => 
				r.Id == requestId &&
				r.NeedDelivery &&
				r.RequestStatus == RequestStatus.Fulfilled &&
				r.Patient.GovernorateId == volunteer.GovernorateId)
			.FirstOrDefaultAsync();
		
		if (request is null) return null;
		
		// التحقق من عدم وجود مهمة توصيل لنفس الطلب (race condition prevention)
		var existingTask = await _context.DeliveryTasks
			.FirstOrDefaultAsync(dt => dt.MedicineRequestId == requestId);
		
		if (existingTask != null)
			return null; // Already taken by another volunteer
		
		// إنشاء مهمة توصيل جديدة
		var deliveryTask = new DeliveryTask
		{
			MedicineRequestId = requestId,
			VolunteerId = volunteer.Id,
			TaskStatus = DeliveryStatus.Taken,
			CreatedAt = DateTime.Now
		};
		
		// تحديث بيانات المتطوع في طلب الدواء حتى يراها المريض
		request.VolunteerId = volunteer.Id;
		request.DeliveryStatus = DeliveryStatus.Taken;
		
		_context.DeliveryTasks.Add(deliveryTask);
		await _context.SaveChangesAsync();
		
		return request;
	}

	public async Task<MedicineRequest?> MarkAsDeliveredAsync(int requestId, string volunteerUserId)
	{
		var volunteer = await _context.Volunteers
			.FirstOrDefaultAsync(v => v.IdentityUserId == volunteerUserId);
		
		if (volunteer is null) return null;
		
		// البحث عن مهمة التوصيل الخاصة بهذا الطلب والمتطوع
		var deliveryTask = await _context.DeliveryTasks
			.Include(dt => dt.MedicineRequest)
				.ThenInclude(r => r.Patient)
					.ThenInclude(p => p.User)
			.Where(dt => 
				dt.MedicineRequestId == requestId &&
				dt.VolunteerId == volunteer.Id &&
				dt.TaskStatus == DeliveryStatus.Taken)
			.FirstOrDefaultAsync();
		
		if (deliveryTask is null) return null;
		
		// تحديث حالة المهمة إلى "تم التوصيل"
		deliveryTask.TaskStatus = DeliveryStatus.Delivered;
		deliveryTask.MedicineRequest.DeliveryStatus = DeliveryStatus.Delivered;
		await _context.SaveChangesAsync();
		
		return deliveryTask.MedicineRequest;
	}
}
