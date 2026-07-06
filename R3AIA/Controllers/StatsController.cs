using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using static R3AIA.Models.Enums;
using R3AIA.Repositories;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly IAdminRepository _adminRepository;

	public StatsController(AppDbContext context, IAdminRepository adminRepository)
	{
		_context = context;
		_adminRepository = adminRepository;
	}

	/// <summary>
	/// إحصائيات داشبورد المريض
	/// </summary>
	[HttpGet("patient")]
	[Authorize(Roles = "Patient")]
	public async Task<IActionResult> GetPatientStats()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) return Unauthorized();

		var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		if (patient is null) return Ok(new { totalRequests = 0, pendingRequests = 0, acceptedRequests = 0, medicineRequests = 0 });

		var medicalRequests = await _context.MedicalRequests
			.Where(r => r.PatientId == patient.Id)
			.ToListAsync();

		var medicineRequests = await _context.MedicineRequests
			.Where(r => r.PatientId == patient.Id)
			.ToListAsync();

		return Ok(new
		{
			totalRequests = medicalRequests.Count + medicineRequests.Count,
			pendingRequests = medicalRequests.Count(r => r.RequestStatus == RequestStatus.Pending)
						   + medicineRequests.Count(r => r.RequestStatus == RequestStatus.Pending),
			acceptedRequests = medicalRequests.Count(r => r.RequestStatus == RequestStatus.Accepted)
						    + medicineRequests.Count(r => r.RequestStatus == RequestStatus.Accepted || r.RequestStatus == RequestStatus.Fulfilled),
			medicineRequests = medicineRequests.Count
		});
	}

	/// <summary>
	/// إحصائيات داشبورد الدكتور
	/// </summary>
	[HttpGet("doctor")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetDoctorStats()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) return Unauthorized();

		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor is null) return Ok(new { availableConsultations = 0, acceptedConsultations = 0, completedConsultations = 0 });

		// الطلبات المتاحة = نفس المحافظة + نفس التخصص + Pending
		var availableCount = await _context.MedicalRequests
			.Include(r => r.Patient)
			.CountAsync(r => r.RequestStatus == RequestStatus.Pending
						  && r.SpecialtyId == doctor.SpecialtyId
						  && r.Patient.GovernorateId == doctor.GovernorateId);

		var acceptedCount = await _context.MedicalRequests
			.CountAsync(r => r.DoctorId == doctor.Id && r.RequestStatus == RequestStatus.Accepted);

		var completedCount = await _context.MedicalRequests
			.CountAsync(r => r.DoctorId == doctor.Id && r.RequestStatus == RequestStatus.Completed);

		return Ok(new
		{
			availableConsultations = availableCount,
			acceptedConsultations = acceptedCount,
			completedConsultations = completedCount
		});
	}

	/// <summary>
	/// إحصائيات داشبورد الصيدلية
	/// </summary>
	[HttpGet("pharmacy")]
	[Authorize(Roles = "Pharmacist")]
	public async Task<IActionResult> GetPharmacyStats()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) return Unauthorized();

		var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		if (pharmacy is null) return Ok(new { availableRequests = 0, fulfilledRequests = 0, totalRequests = 0 });

		// الطلبات المفتوحة = نفس المحافظة + Pending
		var availableCount = await _context.MedicineRequests
			.Include(r => r.Patient)
			.CountAsync(r => r.RequestStatus == RequestStatus.Pending
						  && r.Patient.GovernorateId == pharmacy.GovernorateId);

		var fulfilledCount = await _context.MedicineRequests
			.CountAsync(r => r.PharmacyId == pharmacy.Id
						  && (r.RequestStatus == RequestStatus.Fulfilled || r.RequestStatus == RequestStatus.Accepted));

		var totalCount = await _context.MedicineRequests
			.CountAsync(r => r.PharmacyId == pharmacy.Id);

		return Ok(new
		{
			availableRequests = availableCount,
			fulfilledRequests = fulfilledCount,
			totalRequests = totalCount
		});
	}

	/// <summary>
	/// إحصائيات داشبورد المتطوع
	/// </summary>
	[HttpGet("volunteer")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetVolunteerStats()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (userId is null) return Unauthorized();

		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
		if (volunteer is null) return Ok(new { availableTasks = 0, myTasks = 0, completedTasks = 0 });

		var availableCount = await _context.DeliveryTasks
			.CountAsync(t => t.TaskStatus == DeliveryStatus.Available);

		var myTasksCount = await _context.DeliveryTasks
			.CountAsync(t => t.VolunteerId == volunteer.Id && t.TaskStatus == DeliveryStatus.Taken);

		var completedCount = await _context.DeliveryTasks
			.CountAsync(t => t.VolunteerId == volunteer.Id && t.TaskStatus == DeliveryStatus.Delivered);

		return Ok(new
		{
			availableTasks = availableCount,
			myTasks = myTasksCount,
			completedTasks = completedCount
		});
	}

	/// <summary>
	/// إحصائيات داشبورد الأدمن
	/// </summary>
	[HttpGet("admin")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> GetAdminStats()
	{
		var pendingCount = await _context.Users
			.CountAsync(u => u.AccountStatus == AccountStatus.Pending);

		var totalUsers = await _context.Users.CountAsync();

		var activeCases = await _context.DonationCases
			.CountAsync(c => !c.IsCompleted);

		var totalDonations = await _context.Donations
			.Where(d => d.Status == DonationStatus.Approved)
			.SumAsync(d => (decimal?)d.Amount) ?? 0;

		return Ok(new
		{
			pendingVerifications = pendingCount,
			totalUsers,
			activeCases,
			totalDonations
		});
	}

	[HttpGet("urgent-detail")]
	public async Task<IActionResult> GetUrgentDetail([FromQuery] string type, [FromQuery] int id)
	{
		var detail = await _adminRepository.GetRequestDetailAsync(type, id);
		if (detail is null) return NotFound();
		return Ok(detail);
	}

	[HttpGet("urgent-cases/doctor")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetDoctorUrgentCases()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var doc = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doc == null) return Unauthorized();

		var thresholdDate = DateTime.Now.Subtract(TimeSpan.FromMinutes(60));
		var urgent = await _context.MedicalRequests
			.Where(r => r.CreatedAt <= thresholdDate && r.DoctorId == null &&
						r.Patient.GovernorateId == doc.GovernorateId && r.SpecialtyId == doc.SpecialtyId)
			.OrderByDescending(r => r.CreatedAt)
			.Select(r => new
			{
				Id = r.Id, Type = "Medical", CreatedAt = r.CreatedAt, PatientName = r.Patient.FullName,
				PatientGovernorate = r.Patient.Governorate.Name, PatientCity = r.Patient.City.Name
			})
			.ToListAsync();
		return Ok(urgent);
	}

	[HttpGet("urgent-cases/pharmacist")]
	[Authorize(Roles = "Pharmacy")]
	public async Task<IActionResult> GetPharmacistUrgentCases()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var ph = await _context.Pharmacies.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
		if (ph == null) return Unauthorized();

		var thresholdDate = DateTime.Now.Subtract(TimeSpan.FromMinutes(60));
		var urgent = await _context.MedicineRequests
			.Where(r => r.CreatedAt <= thresholdDate && r.PharmacyId == null && r.Patient.GovernorateId == ph.GovernorateId)
			.OrderByDescending(r => r.CreatedAt)
			.Select(r => new
			{
				Id = r.Id, Type = "Medicine", CreatedAt = r.CreatedAt, PatientName = r.Patient.FullName,
				PatientGovernorate = r.Patient.Governorate.Name, PatientCity = r.Patient.City.Name
			})
			.ToListAsync();
		return Ok(urgent);
	}

	[HttpGet("urgent-cases/volunteer")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetVolunteerUrgentCases()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		var vol = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
		if (vol == null) return Unauthorized();

		var thresholdDate = DateTime.Now.Subtract(TimeSpan.FromMinutes(30));
		// Needs delivery, is available, and has been waiting for more than 30 mins since the pharmacist fulfilled it
		var urgent = await _context.DeliveryTasks
			.Where(t => t.CreatedAt <= thresholdDate && 
			            t.TaskStatus == Enums.DeliveryStatus.Available &&
			            t.MedicineRequest.Patient.GovernorateId == vol.GovernorateId)
			.OrderByDescending(t => t.CreatedAt)
			.Select(t => new
			{
				Id = t.MedicineRequestId, Type = "Medicine", CreatedAt = t.CreatedAt, PatientName = t.MedicineRequest.Patient.FullName,
				PatientGovernorate = t.MedicineRequest.Patient.Governorate.Name, PatientCity = t.MedicineRequest.Patient.City.Name
			})
			.ToListAsync();
		return Ok(urgent);
	}
}
