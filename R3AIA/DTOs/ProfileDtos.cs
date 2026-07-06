using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R3AIA.DTOs;

public class CompletePatientProfileDto
{
	public string FullName { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }


	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string Address { get; set; } = string.Empty;

	public bool HasChronicDisease { get; set; }

	[Required]
	public IFormFile NIDFrontImage { get; set; } = null!;

	[Required]
	public IFormFile NIDBackImage { get; set; } = null!;

	[Required]
	public IFormFile SocialProofImage { get; set; } = null!;
}

public class UpdatePatientProfileDto
{
	public string FullName { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }

	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string Address { get; set; } = string.Empty;

	public bool HasChronicDisease { get; set; }
}

public class CompleteDoctorProfileDto
{
	public string FullName { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }


	[Required]
	public int SpecialtyId { get; set; }

	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string ClinicAddress { get; set; } = string.Empty;

	// ── حقول الكشف المخفض ──
	public string? ConsultationType { get; set; }  // "Free" or "Discounted"
	public decimal? OriginalPrice { get; set; }
	public decimal? DiscountedPrice { get; set; }
	public string? ClinicPhone { get; set; }
	public string? WorkingHours { get; set; }
	public string? Description { get; set; }

	// صورة الدكتور الاختيارية
	public IFormFile? ProfileImage { get; set; }

	// كارنيه مزاولة المهنة
	public IFormFile? LicenseImage { get; set; }
}

public class CompletePharmacyProfileDto
{
	public string PharmacyName { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }


	[Required]
	public int GovernorateId { get; set; }

	[Required]
	public int CityId { get; set; }

	public string Address { get; set; } = string.Empty;
}

public class CompleteVolunteerProfileDto
{
	[Required]
	public string FullName { get; set; } = string.Empty;

	public string NationalID { get; set; } = string.Empty;

	public string? PhoneNumber { get; set; }


	[Required]
	public int GovernorateId { get; set; }
}

