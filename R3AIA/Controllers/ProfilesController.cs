using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Services;
using static R3AIA.Models.Enums;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfilesController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly IFileService _fileService;

	public ProfilesController(AppDbContext context, IFileService fileService)
	{
		_context = context;
		_fileService = fileService;
	}

	private string? GetUserId()
	{
		return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
	}

	/// <summary>
	/// Get the current user's profile data based on their role.
	/// </summary>
	[HttpGet("me")]
	[Authorize]
	public async Task<IActionResult> GetMyProfile()
	{
		try
		{
			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (user is null) return NotFound("User not found.");

			var roles = await _context.UserRoles
				.Where(ur => ur.UserId == userId)
				.Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
				.ToListAsync();
			var role = roles.FirstOrDefault() ?? "";

			object? profileData = null;

			if (role.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
			{
				var doctor = await _context.Doctors
					.Include(d => d.Specialty)
					.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
				if (doctor != null)
				{
					var governorate = await _context.Governorates.FindAsync(doctor.GovernorateId);
					profileData = new
					{
						phone = doctor.PhoneNumber,
						specialty = doctor.Specialty?.Name ?? "",
						specialtyId = doctor.SpecialtyId,
						clinicAddress = doctor.ClinicAddress,
						governorate = governorate?.Name ?? "",
						governorateId = doctor.GovernorateId,
						cityId = doctor.CityId,
						consultationType = doctor.ConsultationType?.ToString(),
						originalPrice = doctor.OriginalPrice,
						discountedPrice = doctor.DiscountedPrice,
						clinicPhone = doctor.ClinicPhone,
						workingHours = doctor.WorkingHours,
						description = doctor.Description,
						profileImage = doctor.ProfileImage
					};
				}
			}
			else if (role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
			{
				var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
				if (patient != null)
				{
					var governorate = await _context.Governorates.FindAsync(patient.GovernorateId);
					var city = await _context.Cities.FindAsync(patient.CityId);
					profileData = new
					{
						phone = patient.PhoneNumber,
						address = patient.Address,
						governorate = governorate?.Name ?? "",
						governorateId = patient.GovernorateId,
						city = city?.Name ?? "",
						cityId = patient.CityId,
						hasChronicDisease = patient.HasChronicDisease
					};
				}
			}
			else if (role.Equals("Pharmacist", StringComparison.OrdinalIgnoreCase))
			{
				var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
				if (pharmacy != null)
				{
					var governorate = await _context.Governorates.FindAsync(pharmacy.GovernorateId);
					profileData = new
					{
						phone = pharmacy.PhoneNumber,
						pharmacyName = pharmacy.PharmacyName,
						address = pharmacy.Address,
						governorate = governorate?.Name ?? "",
						governorateId = pharmacy.GovernorateId,
						cityId = pharmacy.CityId
					};
				}
			}
			else if (role.Equals("Volunteer", StringComparison.OrdinalIgnoreCase))
			{
				var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
				if (volunteer != null)
				{
					var governorate = await _context.Governorates.FindAsync(volunteer.GovernorateId);
					profileData = new
					{
						phone = volunteer.PhoneNumber,
						governorate = governorate?.Name ?? "",
						governorateId = volunteer.GovernorateId
					};
				}
			}

			// Normalize role name for frontend (Pharmacist -> pharmacy)
			var displayRole = role.ToLower();
			if (displayRole == "pharmacist") displayRole = "pharmacy";

			return Ok(new
			{
				id = user.Id,
				name = user.FullName,
				email = user.Email,
				role = displayRole,
				profileCompleted = user.HasCompletedProfile,
				isVerified = user.IsVerified,
				profile = profileData
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace });
		}
	}

	/// <summary>
	/// إكمال بروفايل المريض بعد التسجيل (الخطوة الثانية).
	/// </summary>
	[HttpPost("patient")]
	[Authorize(Roles = "Patient")]
	[RequestSizeLimit(20_000_000)] // 20 MB limit for 2 images
	public async Task<IActionResult> CompletePatient([FromForm] CompletePatientProfileDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (user is null) return BadRequest("User not found.");

			// Validate foreign keys exist
			var governorateExists = await _context.Governorates.AnyAsync(g => g.Id == dto.GovernorateId);
			if (!governorateExists) return BadRequest("Invalid GovernorateId.");

			var cityExists = await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
			if (!cityExists) return BadRequest("Invalid CityId.");

			// حفظ الصور
			var nidFrontImagePath = await _fileService.SaveImageAsync(dto.NIDFrontImage, "Uploads");
			var nidBackImagePath = await _fileService.SaveImageAsync(dto.NIDBackImage, "Uploads");
			var socialProofImagePath = await _fileService.SaveImageAsync(dto.SocialProofImage, "Uploads");

			// إنشاء أو تحديث بروفايل المريض
			var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
			if (patient is null)
			{
				patient = new Patient
				{
					IdentityUserId = user.Id,
					FullName = dto.FullName,
					NationalID = user.NationalID,
					PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!, // Use DTO if provided, else User's phone
					GovernorateId = dto.GovernorateId,
					CityId = dto.CityId,
					Address = dto.Address,
					NIDFrontImage = nidFrontImagePath,
					NIDBackImage = nidBackImagePath,
					SocialProofImage = socialProofImagePath,
					HasChronicDisease = dto.HasChronicDisease,
					IsVerified = false
				};
				_context.Patients.Add(patient);
			}
		else
		{
			patient.FullName = dto.FullName;
			patient.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!;
			patient.GovernorateId = dto.GovernorateId;
			patient.CityId = dto.CityId;
			patient.Address = dto.Address;
			patient.NIDFrontImage = nidFrontImagePath;
			patient.NIDBackImage = nidBackImagePath;
			patient.SocialProofImage = socialProofImagePath;
			patient.HasChronicDisease = dto.HasChronicDisease;
		}

		user.FullName = dto.FullName;
		user.HasCompletedProfile = true;

		await _context.SaveChangesAsync();
		return Ok(new { message = "Profile completed successfully" });
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
		}
	}

	/// <summary>
	/// تحديث بروفايل المريض (بدون صور).
	/// </summary>
	[HttpPut("patient")]
	[Authorize(Roles = "Patient")]
	public async Task<IActionResult> UpdatePatient([FromBody] UpdatePatientProfileDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (user is null) return BadRequest("User not found.");

			// Validate foreign keys exist
			var governorateExists = await _context.Governorates.AnyAsync(g => g.Id == dto.GovernorateId);
			if (!governorateExists) return BadRequest("Invalid GovernorateId.");

			var cityExists = await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
			if (!cityExists) return BadRequest("Invalid CityId.");

			var patient = await _context.Patients.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
			if (patient is null) return BadRequest("Patient profile not found. Please complete your profile first.");

			patient.FullName = dto.FullName;
			patient.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!;
			patient.GovernorateId = dto.GovernorateId;
			patient.CityId = dto.CityId;
			patient.Address = dto.Address;
			patient.HasChronicDisease = dto.HasChronicDisease;

			user.FullName = dto.FullName;

			await _context.SaveChangesAsync();
			return Ok();
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
		}
	}

	/// <summary>
	/// إكمال بروفايل الدكتور بعد التسجيل.
	/// </summary>
	[HttpPost("doctor")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> CompleteDoctor([FromForm] CompleteDoctorProfileDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (user is null) return BadRequest("User not found.");

			// Validate foreign keys exist
			var specialtyExists = await _context.Specialties.AnyAsync(s => s.Id == dto.SpecialtyId);
			if (!specialtyExists) return BadRequest("Invalid SpecialtyId.");

			var governorateExists = await _context.Governorates.AnyAsync(g => g.Id == dto.GovernorateId);
			if (!governorateExists) return BadRequest("Invalid GovernorateId.");

			var cityExists = await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
			if (!cityExists) return BadRequest("Invalid CityId.");

			var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
			
			// Optional image upload
			string? imagePath = doctor?.ProfileImage;
			if (dto.ProfileImage is not null)
			{
				imagePath = await _fileService.SaveImageAsync(dto.ProfileImage, "Uploads/Doctors");
			}

			// License image upload
			string? licensePath = doctor?.LicenseImage;
			if (dto.LicenseImage is not null)
			{
				licensePath = await _fileService.SaveImageAsync(dto.LicenseImage, "Uploads/Doctors/Licenses");
			}

		if (doctor is null)
		{
			doctor = new Doctor
			{
				IdentityUserId = user.Id,
				FullName = dto.FullName,
				PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!,
				SpecialtyId = dto.SpecialtyId,
				GovernorateId = dto.GovernorateId,
				CityId = dto.CityId,
				ClinicAddress = dto.ClinicAddress,
				IsVerified = false,
				ClinicPhone = dto.ClinicPhone,
				WorkingHours = dto.WorkingHours,
				Description = dto.Description,
				ProfileImage = imagePath,
				LicenseImage = licensePath
			};

			if (!string.IsNullOrEmpty(dto.ConsultationType) && Enum.TryParse<Enums.ConsultationType>(dto.ConsultationType, true, out var ct))
			{
				doctor.ConsultationType = ct;
				if (ct == Enums.ConsultationType.Discounted)
				{
					doctor.OriginalPrice = dto.OriginalPrice;
					doctor.DiscountedPrice = dto.DiscountedPrice;
				}
			}

			_context.Doctors.Add(doctor);
		}
		else
		{
			doctor.FullName = dto.FullName;
			doctor.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!;
			doctor.SpecialtyId = dto.SpecialtyId;
			doctor.GovernorateId = dto.GovernorateId;
			doctor.CityId = dto.CityId;
			doctor.ClinicAddress = dto.ClinicAddress;
			doctor.ClinicPhone = dto.ClinicPhone;
			doctor.WorkingHours = dto.WorkingHours;
			doctor.Description = dto.Description;
			if (imagePath is not null) doctor.ProfileImage = imagePath;
			if (licensePath is not null) doctor.LicenseImage = licensePath;

			if (!string.IsNullOrEmpty(dto.ConsultationType) && Enum.TryParse<Enums.ConsultationType>(dto.ConsultationType, true, out var ct2))
			{
				doctor.ConsultationType = ct2;
				if (ct2 == Enums.ConsultationType.Discounted)
				{
					doctor.OriginalPrice = dto.OriginalPrice;
					doctor.DiscountedPrice = dto.DiscountedPrice;
				}
				else
				{
					doctor.OriginalPrice = null;
					doctor.DiscountedPrice = null;
				}
			}
		}

			user.FullName = dto.FullName;
			user.HasCompletedProfile = true;

			await _context.SaveChangesAsync();
			return Ok();
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
		}
	}

	/// <summary>
	/// إكمال بروفايل الصيدلية بعد التسجيل.
	/// </summary>
	[HttpPost("pharmacy")]
	[Authorize(Roles = "Pharmacist")]
	public async Task<IActionResult> CompletePharmacy([FromBody] CompletePharmacyProfileDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (user is null) return BadRequest("User not found.");

			// Validate foreign keys exist
			var governorateExists = await _context.Governorates.AnyAsync(g => g.Id == dto.GovernorateId);
			if (!governorateExists) return BadRequest("Invalid GovernorateId.");

			var cityExists = await _context.Cities.AnyAsync(c => c.Id == dto.CityId);
			if (!cityExists) return BadRequest("Invalid CityId.");

			var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
			if (pharmacy is null)
			{
				pharmacy = new Pharmacy
				{
					IdentityUserId = user.Id,
					PharmacyName = dto.PharmacyName,
					PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!,
					GovernorateId = dto.GovernorateId,
					CityId = dto.CityId,
					Address = dto.Address,
					IsVerified = false
				};
				_context.Pharmacies.Add(pharmacy);
			}
			else
			{
				pharmacy.PharmacyName = dto.PharmacyName;
				pharmacy.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!;
				pharmacy.GovernorateId = dto.GovernorateId;
				pharmacy.CityId = dto.CityId;
				pharmacy.Address = dto.Address;
			}

			user.FullName = dto.PharmacyName;
			user.HasCompletedProfile = true;

			await _context.SaveChangesAsync();
			return Ok();
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
		}
	}

	/// <summary>
	/// إكمال بروفايل المتطوع بعد التسجيل.
	/// </summary>
	[HttpPost("volunteer")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> CompleteVolunteer([FromBody] CompleteVolunteerProfileDto dto)
	{
		try
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var userId = GetUserId();
			if (userId is null) return Unauthorized();

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (user is null) return BadRequest("User not found.");

			// Validate foreign keys exist
			var governorateExists = await _context.Governorates.AnyAsync(g => g.Id == dto.GovernorateId);
			if (!governorateExists) return BadRequest("Invalid GovernorateId.");

			var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
			if (volunteer is null)
			{
				volunteer = new Volunteer
				{
					IdentityUserId = user.Id,
					FullName = dto.FullName,
					NationalID = dto.NationalID,
					PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!,
					GovernorateId = dto.GovernorateId
				};
				_context.Volunteers.Add(volunteer);
			}
			else
			{
				volunteer.FullName = dto.FullName;
				volunteer.NationalID = dto.NationalID;
				volunteer.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber!;
				volunteer.GovernorateId = dto.GovernorateId;
			}

			user.FullName = dto.FullName;
			user.HasCompletedProfile = true;

			await _context.SaveChangesAsync();
			return Ok();
		}
		catch (Exception ex)
		{
			return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
		}
	}
}

