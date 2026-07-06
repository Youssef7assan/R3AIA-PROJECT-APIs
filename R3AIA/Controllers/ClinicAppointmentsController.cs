using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.Models;
using static R3AIA.Models.Enums;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicAppointmentsController : ControllerBase
{
	private readonly AppDbContext _context;

	public ClinicAppointmentsController(AppDbContext context)
	{
		_context = context;
	}

	private string? GetUserId()
	{
		return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
	}

	/// <summary>
	/// Book an appointment (public, no auth required).
	/// </summary>
	[HttpPost]
	public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentDto dto)
	{
		if (string.IsNullOrWhiteSpace(dto.PatientName) || string.IsNullOrWhiteSpace(dto.PatientPhone))
			return BadRequest("اسم المريض ورقم الهاتف مطلوبان.");

		var doctor = await _context.Doctors.FindAsync(dto.DoctorId);
		if (doctor is null) return NotFound("الطبيب غير موجود.");

		var appointment = new ClinicAppointment
		{
			DoctorId = dto.DoctorId,
			PatientName = dto.PatientName,
			PatientPhone = dto.PatientPhone,
			Notes = dto.Notes,
			Status = AppointmentStatus.Pending,
			CreatedAt = DateTime.Now
		};

		// If the user is authenticated, link the appointment
		var userId = GetUserId();
		if (!string.IsNullOrEmpty(userId))
		{
			appointment.PatientId = userId;
		}

		_context.ClinicAppointments.Add(appointment);
		await _context.SaveChangesAsync();

		return Ok(new { message = "تم الحجز بنجاح! الدفع سيكون في العيادة.", appointmentId = appointment.Id });
	}

	/// <summary>
	/// Doctor: get all my clinic appointments.
	/// </summary>
	[HttpGet("my-appointments")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> GetMyAppointments()
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor is null) return NotFound("Doctor profile not found.");

		var appointments = await _context.ClinicAppointments
			.Where(a => a.DoctorId == doctor.Id)
			.OrderByDescending(a => a.CreatedAt)
			.Select(a => new
			{
				a.Id,
				a.PatientName,
				a.PatientPhone,
				a.Notes,
				status = a.Status.ToString(),
				a.CreatedAt
			})
			.ToListAsync();

		return Ok(appointments);
	}

	/// <summary>
	/// Doctor: update appointment status (Confirm / Cancel).
	/// </summary>
	[HttpPut("{id}/status")]
	[Authorize(Roles = "Doctor")]
	public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
	{
		var userId = GetUserId();
		if (userId is null) return Unauthorized();

		var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.IdentityUserId == userId);
		if (doctor is null) return NotFound("Doctor profile not found.");

		var appointment = await _context.ClinicAppointments
			.FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctor.Id);

		if (appointment is null) return NotFound("الحجز غير موجود.");

		if (Enum.TryParse<AppointmentStatus>(dto.Status, true, out var status))
		{
			appointment.Status = status;
			await _context.SaveChangesAsync();
			return Ok(new { message = "تم تحديث حالة الحجز." });
		}

		return BadRequest("حالة غير صالحة.");
	}
}

// ── DTOs ──
public class BookAppointmentDto
{
	public int DoctorId { get; set; }
	public string PatientName { get; set; } = string.Empty;
	public string PatientPhone { get; set; } = string.Empty;
	public string? Notes { get; set; }
}

public class UpdateAppointmentStatusDto
{
	public string Status { get; set; } = string.Empty;
}
