using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All logged in users can use tickets
public class TicketsController : ControllerBase
{
	private readonly AppDbContext _context;
	private readonly INotificationService _notificationService;
	private readonly UserManager<ApplicationUser> _userManager;

	public TicketsController(AppDbContext context, INotificationService notificationService, UserManager<ApplicationUser> userManager)
	{
		_context = context;
		_notificationService = notificationService;
		_userManager = userManager;
	}

	/// <summary>
	/// Create a new support ticket
	/// </summary>
	[HttpPost]
	public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var ticket = new SupportTicket
		{
			UserId = userId,
			Subject = dto.Subject,
			Status = Enums.SupportTicketStatus.Open,
			CreatedAt = DateTime.Now,
			UpdatedAt = DateTime.Now
		};

		_context.SupportTickets.Add(ticket);
		await _context.SaveChangesAsync();

		// Add user's first message
		var userMessage = new SupportTicketMessage
		{
			TicketId = ticket.Id,
			SenderId = userId,
			Message = dto.Message,
			IsFromAdmin = false,
			CreatedAt = DateTime.Now
		};
		_context.SupportTicketMessages.Add(userMessage);

		// Add automated admin response
		var systemMessage = new SupportTicketMessage
		{
			TicketId = ticket.Id,
			SenderId = userId, // use same id just as placeholder for system, or leave empty if nullable. Since required, user's id is fine, but IsFromAdmin=true dictates UI
			Message = "تم استلام تذكرتك بنجاح، مديرين النظام هيردو عليك في اقرب وقت.",
			IsFromAdmin = true,
			CreatedAt = DateTime.Now.AddSeconds(1)
		};
		_context.SupportTicketMessages.Add(systemMessage);

		await _context.SaveChangesAsync();

		// Optional: Notify admins about new ticket

		return Ok(new { message = "Ticket created successfully", ticketId = ticket.Id });
	}

	/// <summary>
	/// Get all tickets for current user
	/// </summary>
	[HttpGet("my-tickets")]
	public async Task<IActionResult> GetMyTickets()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var tickets = await _context.SupportTickets
			.Include(t => t.User)
			.Where(t => t.UserId == userId && !t.IsDeletedByUser)
			.OrderByDescending(t => t.UpdatedAt)
			.Select(t => new TicketSummaryDto
			{
				Id = t.Id,
				UserId = t.UserId,
				UserName = t.User!.FullName,
				UserRole = t.User.UserType.ToString(),
				Status = t.Status.ToString(),
				Subject = t.Subject,
				CreatedAt = t.CreatedAt,
				UpdatedAt = t.UpdatedAt
			})
			.ToListAsync();

		return Ok(tickets);
	}

	/// <summary>
	/// Get single ticket with messages
	/// </summary>
	[HttpGet("{id}")]
	public async Task<IActionResult> GetTicket(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var userIsAdmin = User.IsInRole("Admin");

		var ticket = await _context.SupportTickets
			.Include(t => t.User)
			.Include(t => t.Messages)
			.ThenInclude(m => m.Sender)
			.FirstOrDefaultAsync(t => t.Id == id);

		if (ticket is null) return NotFound("Ticket not found.");

		if (userIsAdmin && ticket.IsDeletedByAdmin) return NotFound("Ticket deleted by admin.");
		if (!userIsAdmin && (ticket.UserId != userId || ticket.IsDeletedByUser)) return NotFound("Ticket not found.");

		if (!userIsAdmin && ticket.UserId != userId)
			return Forbid("You don't have access to this ticket.");

		var dto = new TicketSummaryDto
		{
			Id = ticket.Id,
			UserId = ticket.UserId,
			UserName = ticket.User!.FullName,
			UserRole = ticket.User.UserType.ToString(),
			Status = ticket.Status.ToString(),
			Subject = ticket.Subject,
			CreatedAt = ticket.CreatedAt,
			UpdatedAt = ticket.UpdatedAt,
			IsDeletedByUser = ticket.IsDeletedByUser,
			Messages = ticket.Messages.OrderBy(m => m.CreatedAt).Select(m => new TicketMessageDto
			{
				Id = m.Id,
				SenderId = m.SenderId,
				SenderName = m.Sender!.FullName,
				SenderRole = m.Sender.UserType.ToString(),
				Message = m.Message,
				IsFromAdmin = m.IsFromAdmin,
				CreatedAt = m.CreatedAt
			})
		};

		return Ok(dto);
	}

	/// <summary>
	/// Reply to a ticket
	/// </summary>
	[HttpPost("{id}/reply")]
	public async Task<IActionResult> ReplyToTicket(int id, [FromBody] ReplyTicketDto dto)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var userIsAdmin = User.IsInRole("Admin");

		var ticket = await _context.SupportTickets
			.FirstOrDefaultAsync(t => t.Id == id);

		if (ticket is null) return NotFound("Ticket not found.");
		if (!userIsAdmin && ticket.UserId != userId) return Forbid();
		
		if (ticket.IsDeletedByUser) return BadRequest("المستخدم قام بحذف هذه التذكرة ولا يمكن الرد عليها.");
		if (ticket.Status == Enums.SupportTicketStatus.Closed) return BadRequest("Cannot reply to a closed ticket.");

		var message = new SupportTicketMessage
		{
			TicketId = ticket.Id,
			SenderId = userId,
			Message = dto.Message,
			IsFromAdmin = userIsAdmin,
			CreatedAt = DateTime.Now
		};

		_context.SupportTicketMessages.Add(message);
		ticket.UpdatedAt = DateTime.Now;
		await _context.SaveChangesAsync();

		if (userIsAdmin)
		{
			// Notify the user
			await _notificationService.SendNotificationAsync(
				ticket.UserId,
				"رد جديد على تذكرة الدعم",
				$"تلقيت رداً جديداً بخصوص التذكرة: {ticket.Subject}. مدير النظام قام بالرد الآن.",
				$"ticket?id={ticket.Id}" // The action URL!
			);
		}

		return Ok(new { message = "Reply added successfully" });
	}

	/// <summary>
	/// Close a ticket
	/// </summary>
	[HttpPost("{id}/close")]
	public async Task<IActionResult> CloseTicket(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var userIsAdmin = User.IsInRole("Admin");

		var ticket = await _context.SupportTickets
			.FirstOrDefaultAsync(t => t.Id == id);

		if (ticket is null) return NotFound("Ticket not found.");
		if (!userIsAdmin && ticket.UserId != userId) return Forbid();

		ticket.Status = Enums.SupportTicketStatus.Closed;
		ticket.UpdatedAt = DateTime.Now;
		await _context.SaveChangesAsync();

		return Ok(new { message = "Ticket closed successfully" });
	}

	/// <summary>
	/// [Admin] Get all tickets
	/// </summary>
	[HttpGet("admin/all")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> GetAllTickets()
	{
		var tickets = await _context.SupportTickets
			.Include(t => t.User)
			.Where(t => !t.IsDeletedByAdmin)
			.OrderBy(t => t.Status == Enums.SupportTicketStatus.Open ? 0 : 1) // Open first
			.ThenByDescending(t => t.UpdatedAt)
			.Select(t => new TicketSummaryDto
			{
				Id = t.Id,
				UserId = t.UserId,
				UserName = t.User!.FullName,
				UserRole = t.User.UserType.ToString(),
				Status = t.Status.ToString(),
				Subject = t.Subject,
				CreatedAt = t.CreatedAt,
				UpdatedAt = t.UpdatedAt,
				IsDeletedByUser = t.IsDeletedByUser
			})
			.ToListAsync();

		return Ok(tickets);
	}

	/// <summary>
	/// Delete a ticket (soft or hard delete)
	/// </summary>
	[HttpDelete("{id}")]
	public async Task<IActionResult> DeleteTicket(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var userIsAdmin = User.IsInRole("Admin");

		var ticket = await _context.SupportTickets
			.FirstOrDefaultAsync(t => t.Id == id);

		if (ticket is null) return NotFound("Ticket not found.");
		if (!userIsAdmin && ticket.UserId != userId) return Forbid();

		if (userIsAdmin)
		{
			ticket.IsDeletedByAdmin = true;
		}
		else
		{
			ticket.IsDeletedByUser = true;
			ticket.Status = Enums.SupportTicketStatus.Closed; // Automatically close if user deletes
		}

		// Hard Delete Logic: If both parties deleted it, remove from DB to save space
		if (ticket.IsDeletedByUser && ticket.IsDeletedByAdmin)
		{
			_context.SupportTickets.Remove(ticket);
		}

		await _context.SaveChangesAsync();
		return Ok(new { message = "Ticket deleted successfully" });
	}
}
