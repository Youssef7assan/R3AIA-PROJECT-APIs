using R3AIA.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class Volunteer
	{
		public int Id { get; set; }

		[Required]
		public string IdentityUserId { get; set; }
		[ForeignKey("IdentityUserId")]
		public ApplicationUser User { get; set; }

		public string FullName { get; set; }
		public string NationalID { get; set; }
		public string PhoneNumber { get; set; }

		public int GovernorateId { get; set; }
	}
}