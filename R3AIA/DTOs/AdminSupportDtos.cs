using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class UserReportDto
{
	public int Id { get; set; }
	public string ReporterName { get; set; } = string.Empty;
	public string ReporterId { get; set; } = string.Empty;
	public string ReportedUserName { get; set; } = string.Empty;
	public string ReportedUserId { get; set; } = string.Empty;
	public string Reason { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string? AdminActionNotes { get; set; }
	public DateTime CreatedAt { get; set; }
}

public class ResolveReportDto
{
	[Required]
	public int ReportId { get; set; }

	/// <summary>
	/// "Resolved" or "Dismissed"
	/// </summary>
	[Required]
	public string NewStatus { get; set; } = "Resolved";

	public string? AdminComment { get; set; }
}

public class BroadcastNotificationDto
{
	[Required]
	public string Title { get; set; } = string.Empty;
	
	[Required]
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// Target role (Patient, Doctor, etc.) or null/empty for All users.
	/// </summary>
	public string? TargetRole { get; set; }
}

