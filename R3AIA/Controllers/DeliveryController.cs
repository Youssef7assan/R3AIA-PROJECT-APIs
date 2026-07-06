using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;
using R3AIA.Repositories;
using R3AIA.Services;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveryController : ControllerBase
{
	private readonly IDeliveryRepository _deliveryRepository;
	private readonly INotificationService _notificationService;
	private readonly AppDbContext _context;

	public DeliveryController(IDeliveryRepository deliveryRepository, INotificationService notificationService, AppDbContext context)
	{
		_deliveryRepository = deliveryRepository;
		_notificationService = notificationService;
		_context = context;
	}

	[HttpGet("available")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetAvailable()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var tasks = await _deliveryRepository.GetAvailableTasksForVolunteerAsync(userId);
		var result = tasks.Select(t => new {
			t.Id,
			RequestId = t.MedicineRequestId,
			PatientName = t.MedicineRequest?.Patient?.FullName,
			PatientPhone = t.MedicineRequest?.Patient?.PhoneNumber,
			PatientAddress = t.MedicineRequest?.Patient?.City != null 
				? $"{t.MedicineRequest.Patient.City.Name}, {t.MedicineRequest.Patient.Governorate?.Name}" 
				: null,
			PharmacyName = t.MedicineRequest?.Pharmacy?.PharmacyName,
			PharmacyAddress = t.MedicineRequest?.Pharmacy?.Address,
			PharmacyPhone = t.MedicineRequest?.Pharmacy?.PhoneNumber,
			TaskStatus = t.TaskStatus.ToString(),
			t.CreatedAt,
		});
		return Ok(result);
	}

	[HttpPost("accept")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> AcceptTask([FromBody] AcceptTaskDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var task = await _deliveryRepository.AcceptTaskAsync(dto, userId);
		if (task is null) return BadRequest("Cannot accept this task.");

		// إرسال إشعار للمريض
		var volunteer = await _context.Volunteers.FirstOrDefaultAsync(v => v.IdentityUserId == userId);
		var request = await _context.MedicineRequests
			.Include(r => r.Patient)
			.FirstOrDefaultAsync(r => r.Id == dto.RequestId);
		
		if (volunteer != null && request?.Patient != null)
		{
			await _notificationService.SendNotificationAsync(
				request.Patient.IdentityUserId,
				"متطوع قبل توصيل دوائك! 🚚",
				$"المتطوع {volunteer.FullName} سيقوم بتوصيل الدواء إليك. رقم الهاتف: {volunteer.PhoneNumber}",
				$"medicine?id={request.Id}"
			);
		}

		return Ok(new { message = "تم قبول المهمة بنجاح", taskId = task.Id });
	}

	[HttpPut("status")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> UpdateStatus([FromBody] UpdateTaskStatusDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var task = await _deliveryRepository.UpdateTaskStatusAsync(dto, userId);
		if (task is null) return BadRequest("Task not found or not assigned to you.");

		// إشعار المريض عند التوصيل
		if (dto.Status == Enums.DeliveryStatus.Delivered && task.MedicineRequest?.Patient != null)
		{
			task.MedicineRequest.DeliveryStatus = Enums.DeliveryStatus.Delivered;
			await _context.SaveChangesAsync();
			
			await _notificationService.SendNotificationAsync(
				task.MedicineRequest.Patient.IdentityUserId,
				"تم توصيل دوائك بنجاح! ✅",
				"تم توصيل الدواء إليك. نتمنى لك الشفاء العاجل",
				$"medicine?id={task.MedicineRequestId}"
			);
		}

		return Ok(new { message = "تم تحديث حالة المهمة", taskId = task.Id, status = task.TaskStatus.ToString() });
	}

	[HttpGet("my-tasks")]
	[Authorize(Roles = "Volunteer")]
	public async Task<IActionResult> GetMyTasks()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var tasks = await _deliveryRepository.GetMyTasksAsync(userId);
		var result = tasks.Select(t => new {
			t.Id,
			RequestId = t.MedicineRequestId,
			PatientName = t.MedicineRequest?.Patient?.FullName,
			PatientPhone = t.MedicineRequest?.Patient?.PhoneNumber,
			PatientAddress = t.MedicineRequest?.Patient?.City != null 
				? $"{t.MedicineRequest.Patient.City.Name}, {t.MedicineRequest.Patient.Governorate?.Name}" 
				: null,
			PharmacyName = t.MedicineRequest?.Pharmacy?.PharmacyName,
			PharmacyAddress = t.MedicineRequest?.Pharmacy?.Address,
			PharmacyPhone = t.MedicineRequest?.Pharmacy?.PhoneNumber,
			TaskStatus = t.TaskStatus.ToString(),
			t.CreatedAt,
		});
		return Ok(result);
	}
}
