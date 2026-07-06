using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Repositories;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
	private readonly IAdminRepository _adminRepository;
	private readonly IFileService _fileService;
	private readonly AppDbContext _context;
	private readonly INotificationService _notificationService;
	private readonly IMapper _mapper;

	public AdminController(IAdminRepository adminRepository, IFileService fileService, AppDbContext context, INotificationService notificationService, IMapper mapper)
	{
		_adminRepository = adminRepository;
		_fileService = fileService;
		_context = context;
		_notificationService = notificationService;
		_mapper = mapper;
	}

	[HttpGet("pending-users")]
	public async Task<IActionResult> GetPendingUsers()
	{
		var users = await _adminRepository.GetPendingUsersAsync();
		return Ok(users);
	}

	[HttpGet("all-users")]
	public async Task<IActionResult> GetAllUsers()
	{
		var users = await _adminRepository.GetAllUsersAsync();
		return Ok(users);
	}

	/// <summary>
	/// نسخة موسّعة تعرض بيانات البروفايل (محافظة، عنوان، تخصص، صور) إلى جانب بيانات المستخدم الأساسية.
	/// </summary>
	[HttpGet("pending-users-details")]
	public async Task<IActionResult> GetPendingUsersDetails()
	{
		var users = (await _adminRepository.GetPendingUsersAsync()).ToList();
		var result = new List<object>();

		foreach (var u in users)
		{
			var role = u.UserType switch
			{
				Models.Enums.UserType.Patient => "patient",
				Models.Enums.UserType.Doctor => "doctor",
				Models.Enums.UserType.Pharmacist => "pharmacy",
				Models.Enums.UserType.Volunteer => "volunteer",
				Models.Enums.UserType.Admin => "admin",
				_ => "unknown"
			};

			object profileData = role switch
			{
				"patient" => await GetPatientProfile(u.Id),
				"doctor" => await GetDoctorProfile(u.Id),
				"pharmacy" => await GetPharmacyProfile(u.Id),
				"volunteer" => await GetVolunteerProfile(u.Id),
				_ => null
			};

			var entry = new
			{
				id = u.Id,
				fullName = u.FullName,
				email = u.Email,
				role,
				nationalID = u.NationalID,
				createdAt = u.CreatedAt,
				hasCompletedProfile = u.HasCompletedProfile,
				profile = profileData
			};
			result.Add(entry);
		}

		return Ok(result);
	}

	[HttpGet("user-details/{userId}")]
	public async Task<IActionResult> GetUserDetails(string userId)
	{
		var u = await _context.Users.FindAsync(userId);
		if (u == null) return NotFound("User not found.");

		var role = u.UserType switch
		{
			Models.Enums.UserType.Patient => "patient",
			Models.Enums.UserType.Doctor => "doctor",
			Models.Enums.UserType.Pharmacist => "pharmacy",
			Models.Enums.UserType.Volunteer => "volunteer",
			Models.Enums.UserType.Admin => "admin",
			_ => "unknown"
		};

		object profileData = role switch
		{
			"patient" => await GetPatientProfile(u.Id),
			"doctor" => await GetDoctorProfile(u.Id),
			"pharmacy" => await GetPharmacyProfile(u.Id),
			"volunteer" => await GetVolunteerProfile(u.Id),
			_ => null
		};

		return Ok(new
		{
			id = u.Id,
			fullName = u.FullName,
			email = u.Email,
			phoneNumber = u.PhoneNumber,
			role,
			nationalID = u.NationalID,
			createdAt = u.CreatedAt,
			hasCompletedProfile = u.HasCompletedProfile,
			profile = profileData
		});
	}

	private async Task<object?> GetPatientProfile(string userId)
	{
		var p = await _context.Patients
			.Include(x => x.Governorate).Include(x => x.City)
			.FirstOrDefaultAsync(x => x.IdentityUserId == userId);
		if (p == null) return null;
		return new
		{
			p.PhoneNumber, p.Address,
			governorate = p.Governorate?.Name,
			city = p.City?.Name,
			p.HasChronicDisease, p.NIDFrontImage, p.NIDBackImage, p.SocialProofImage, p.NationalID
		};
	}

	private async Task<object?> GetDoctorProfile(string userId)
	{
		var d = await _context.Doctors
			.Include(x => x.Governorate).Include(x => x.City).Include(x => x.Specialty)
			.FirstOrDefaultAsync(x => x.IdentityUserId == userId);
		if (d == null) return null;
		return new
		{
			d.PhoneNumber, d.ClinicAddress,
			governorate = d.Governorate?.Name,
			city = d.City?.Name,
			specialty = d.Specialty?.Name,
			profileImage = d.ProfileImage,
			licenseImage = d.LicenseImage,
			consultationType = d.ConsultationType?.ToString(),
			d.OriginalPrice,
			d.DiscountedPrice
		};
	}

	private async Task<object?> GetPharmacyProfile(string userId)
	{
		var ph = await _context.Pharmacies
			.Include(x => x.Governorate).Include(x => x.City)
			.FirstOrDefaultAsync(x => x.IdentityUserId == userId);
		if (ph == null) return null;
		return new
		{
			ph.PharmacyName, ph.PhoneNumber, ph.Address,
			governorate = ph.Governorate?.Name,
			city = ph.City?.Name
		};
	}

	private async Task<object?> GetVolunteerProfile(string userId)
	{
		var v = await _context.Volunteers
			.FirstOrDefaultAsync(x => x.IdentityUserId == userId);
		if (v == null) return null;
		return new
		{
			v.PhoneNumber, v.NationalID,
			governorate = (await _context.Governorates.FindAsync(v.GovernorateId))?.Name
		};
	}

	[HttpPost("verify-user")]
	public async Task<IActionResult> VerifyUser([FromBody] UserVerificationDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var ok = await _adminRepository.VerifyUserAsync(dto);
		if (!ok) return BadRequest("Unable to verify user.");

		return Ok();
	}

	[HttpPost("create-case")]
	[RequestSizeLimit(10_000_000)]
	public async Task<IActionResult> CreateCase([FromForm] CreateDonationCaseDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var imageUrl = await _fileService.SaveImageAsync(dto.CaseImage, "Uploads/Cases");
		var donationCase = await _adminRepository.CreateDonationCaseAsync(dto, imageUrl);

		// إشعار لجميع المستخدمين بوجود حالة تبرع جديدة
		await _notificationService.BroadcastAsync("حالة تبرع جديدة", $"تمت إضافة حالة تبرع جديدة: {dto.Title}. ساهم معنا فى فعل الخير.");

		return Ok(donationCase);
	}

	[HttpGet("all-donation-cases")]
	public async Task<IActionResult> GetAllDonationCases()
	{
		var cases = await _adminRepository.GetAllDonationCasesAsync();
		return Ok(cases);
	}

	[HttpPut("donation-cases/{id}")]
	[RequestSizeLimit(10_000_000)]
	public async Task<IActionResult> EditDonationCase(int id, [FromForm] EditDonationCaseDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		string? imageUrl = null;
		if (dto.CaseImage != null)
		{
			imageUrl = await _fileService.SaveImageAsync(dto.CaseImage, "Uploads/Cases");
		}

		var updatedCase = await _adminRepository.EditDonationCaseAsync(id, dto, imageUrl);
		if (updatedCase == null) return NotFound("Donation case not found.");

		return Ok(updatedCase);
	}

	[HttpPost("broadcast-notification")]
	public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		if (string.IsNullOrEmpty(dto.TargetRole) || dto.TargetRole.Equals("All", StringComparison.OrdinalIgnoreCase))
		{
			await _notificationService.BroadcastAsync(dto.Title, dto.Message);
		}
		else
		{
			if (Enum.TryParse<Enums.UserType>(dto.TargetRole, true, out var role))
			{
				await _notificationService.SendToRoleAsync(role, dto.Title, dto.Message);
			}
			else
			{
				return BadRequest("Invalid target role.");
			}
		}

		return Ok(new { message = "Notification broadcasted successfully." });
	}

	[HttpGet("all-reports")]
	public async Task<IActionResult> GetAllReports()
	{
		var reports = await _adminRepository.GetAllReportsAsync();
		var result = _mapper.Map<IEnumerable<UserReportDto>>(reports);
		return Ok(result);
	}

	[HttpPut("resolve-report")]
	public async Task<IActionResult> ResolveReport([FromBody] ResolveReportDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var ok = await _adminRepository.ResolveReportAsync(dto);
		if (!ok) return BadRequest("Report not found.");

		return Ok();
	}

	/// <summary>
	/// طلبات متعثرة (تخطت زمن معين) من الاستشارات والأدوية.
	/// </summary>
	[HttpGet("stalled-requests")]
	public async Task<IActionResult> GetStalledRequests([FromQuery] int minutes = 60)
	{
		var stalled = await _adminRepository.GetStalledRequestsAsync(TimeSpan.FromMinutes(minutes));
		return Ok(stalled);
	}

	/// <summary>
	/// تفاصيل طلب محدد (Medical أو Medicine) مع بيانات المريض والأطراف المحتملة.
	/// </summary>
	[HttpGet("request-detail")]
	public async Task<IActionResult> GetRequestDetail([FromQuery] string type, [FromQuery] int id)
	{
		var detail = await _adminRepository.GetRequestDetailAsync(type, id);
		if (detail is null) return NotFound();
		return Ok(detail);
	}

	/// <summary>
	/// حظر مستخدم نهائياً (Ban) باستخدام الـ UserId.
	/// </summary>
	[HttpPost("ban-user/{userId}")]
	public async Task<IActionResult> BanUser(string userId)
	{
		var ok = await _adminRepository.BanUserAsync(userId);
		if (!ok) return BadRequest("Unable to ban user.");
		return Ok();
	}

	/// <summary>
	/// فك الحظر عن مستخدم (Unban) باستخدام الـ UserId.
	/// </summary>
	[HttpPost("unban-user/{userId}")]
	public async Task<IActionResult> UnbanUser(string userId)
	{
		var ok = await _adminRepository.UnbanUserAsync(userId);
		if (!ok) return BadRequest("Unable to unban user.");
		return Ok();
	}

	[HttpDelete("donation-cases/{id}")]
	public async Task<IActionResult> DeleteDonationCase(int id)
	{
		var ok = await _adminRepository.DeleteDonationCaseAsync(id);
		if (!ok) return NotFound();
		return Ok();
	}

	[HttpPost("broadcast-urgent")]
	public async Task<IActionResult> BroadcastUrgent([FromBody] BroadcastUrgentDto dto)
	{
		var ok = await _adminRepository.BroadcastUrgentSosAsync(dto.Type, dto.RequestId);
		if (!ok) return BadRequest("تعذر إرسال التعميم للمختصين.");
		return Ok();
	}
}

public class BroadcastUrgentDto
{
	public string Type { get; set; } = null!;
	public int RequestId { get; set; }
}


