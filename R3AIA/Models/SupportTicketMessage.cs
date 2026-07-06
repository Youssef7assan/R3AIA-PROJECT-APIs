using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace R3AIA.Models;

public class SupportTicketMessage
{
	[Key]
	public int Id { get; set; }

	[Required]
	public int TicketId { get; set; }

	[ForeignKey("TicketId")]
	public SupportTicket? Ticket { get; set; }

	[Required]
	public string SenderId { get; set; } = string.Empty;

	[ForeignKey("SenderId")]
	public ApplicationUser? Sender { get; set; }

	[Required]
	public string Message { get; set; } = string.Empty;

	public bool IsFromAdmin { get; set; } = false;

	public DateTime CreatedAt { get; set; } = DateTime.Now;
}
