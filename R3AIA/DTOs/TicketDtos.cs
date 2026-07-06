using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R3AIA.DTOs;

public class CreateTicketDto
{
	[Required]
	[MaxLength(200)]
	public string Subject { get; set; } = string.Empty;

	[Required]
	public string Message { get; set; } = string.Empty;
}

public class ReplyTicketDto
{
	[Required]
	public string Message { get; set; } = string.Empty;
}

public class TicketMessageDto
{
	public int Id { get; set; }
	public string SenderId { get; set; } = string.Empty;
	public string SenderName { get; set; } = string.Empty;
	public string SenderRole { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public bool IsFromAdmin { get; set; }
	public DateTime CreatedAt { get; set; }
}

public class TicketSummaryDto
{
	public int Id { get; set; }
	public string UserId { get; set; } = string.Empty;
	public string UserName { get; set; } = string.Empty;
	public string UserRole { get; set; } = string.Empty;
	public string Status { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public bool IsDeletedByUser { get; set; }
	
	// Only returning messages when fetching a single ticket
	public IEnumerable<TicketMessageDto>? Messages { get; set; }
}
