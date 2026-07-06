using R3AIA.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class Pharmacy
	{
		public int Id { get; set; }

		[Required]
		public string IdentityUserId { get; set; }
		[ForeignKey("IdentityUserId")]
		public ApplicationUser User { get; set; }

		[Required]
		public string PharmacyName { get; set; }
		public string PhoneNumber { get; set; }

		public int GovernorateId { get; set; }
		[ForeignKey("GovernorateId")]
		public virtual Governorate Governorate { get; set; }
		public int CityId { get; set; }
		[ForeignKey("CityId")]
		public virtual City City { get; set; }
		public string Address { get; set; }

	

		public bool IsVerified { get; set; } = false;
	}
}