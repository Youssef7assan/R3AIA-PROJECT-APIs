using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models
{
	// تعريف حالات البلاغ (يجب أن يكون خارج الكلاس أو في ملف منفصل)
	public enum ReportStatus
	{
		Pending,          // معلق
		UnderProcessing,   // قيد المعالجة
		Resolved,         // تم حله
		Dismissed         // تم تجاهله
	}

	public class UserReport
	{
		public int Id { get; set; }

		[Required]
		public string ReporterId { get; set; }
		[ForeignKey("ReporterId")]
		public virtual ApplicationUser Reporter { get; set; }

		[Required]
		public string ReportedUserId { get; set; }
		[ForeignKey("ReportedUserId")]
		public virtual ApplicationUser ReportedUser { get; set; }

		[Required]
		public string Reason { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		// هنا قمنا باستخدام الـ Enum الذي عرفناه بالأعلى
		public ReportStatus Status { get; set; } = ReportStatus.Pending;

		public string? AdminActionNotes { get; set; }
	}
}