using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models;

public class SupportTicket
{
	[Key]
	public int Id { get; set; }

	[Required]
	public string UserId { get; set; } = string.Empty;

	[ForeignKey("UserId")]
	public ApplicationUser? User { get; set; }

	[Required]
	public Enums.SupportTicketStatus Status { get; set; } = Enums.SupportTicketStatus.Open;

	[Required]
	[MaxLength(200)]
	public string Subject { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public DateTime UpdatedAt { get; set; } = DateTime.Now;

	public bool IsDeletedByUser { get; set; } = false;
	public bool IsDeletedByAdmin { get; set; } = false;

	public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
}
