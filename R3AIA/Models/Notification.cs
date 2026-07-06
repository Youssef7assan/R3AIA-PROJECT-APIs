using R3AIA.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	public class Notification
	{
		public int Id { get; set; }

		public string UserId { get; set; }
		[ForeignKey("UserId")]
		public ApplicationUser User { get; set; }

		public string Title { get; set; }
		public string Message { get; set; }
		public string? ActionUrl { get; set; }
		public bool IsRead { get; set; } = false;
		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}