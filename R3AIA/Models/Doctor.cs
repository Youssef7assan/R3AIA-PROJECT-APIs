using R3AIA.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class Doctor
	{
		public int Id { get; set; }

		[Required]
		public string IdentityUserId { get; set; }
		[ForeignKey("IdentityUserId")]
		public ApplicationUser User { get; set; }

		[Required]
		public string FullName { get; set; }
		public string PhoneNumber { get; set; }

		public int SpecialtyId { get; set; }
		[ForeignKey("SpecialtyId")]
		public Specialty Specialty { get; set; }

		public int GovernorateId { get; set; }
		[ForeignKey("GovernorateId")]
		public virtual Governorate Governorate { get; set; }
		public int CityId { get; set; }
		[ForeignKey("CityId")]
		public virtual City City { get; set; }
		public string? ClinicAddress { get; set; }

		public bool IsVerified { get; set; } = false;

		public Enums.ConsultationType? ConsultationType { get; set; }
		[Column(TypeName = "decimal(18,2)")]
		public decimal? OriginalPrice { get; set; }
		[Column(TypeName = "decimal(18,2)")]
		public decimal? DiscountedPrice { get; set; }
		public string? ClinicPhone { get; set; }
		public string? WorkingHours { get; set; }
		public string? ProfileImage { get; set; }
		public string? LicenseImage { get; set; }   // كارنيه مزاولة المهنة
		public string? Description { get; set; }
	}
}