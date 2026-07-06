using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using R3AIA.DTOs;
using R3AIA.Repositories;

namespace R3AIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupportController : ControllerBase
{
	private readonly ISupportRepository _supportRepository;
	private readonly IMapper _mapper;

	public SupportController(ISupportRepository supportRepository, IMapper mapper)
	{
		_supportRepository = supportRepository;
		_mapper = mapper;
	}

	[HttpPost("report")]
	[Authorize]
	public async Task<IActionResult> Report([FromBody] CreateReportDto dto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var report = await _supportRepository.AddReportAsync(dto, userId);
		return Ok(report);
	}

	[HttpGet("notifs")]
	[Authorize]
	public async Task<IActionResult> GetNotifications()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var notifications = await _supportRepository.GetUserNotificationsAsync(userId);
		var result = _mapper.Map<IEnumerable<NotificationDto>>(notifications);
		return Ok(result);
	}

	[HttpPatch("mark-read/{id}")]
	[Authorize]
	public async Task<IActionResult> MarkRead(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var ok = await _supportRepository.MarkAsReadAsync(id, userId);
		if (!ok) return NotFound();

		return Ok();
	}

	[HttpPatch("mark-all-read")]
	[Authorize]
	public async Task<IActionResult> MarkAllRead()
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		await _supportRepository.MarkAllAsReadAsync(userId);
		return Ok();
	}

	[HttpDelete("delete-notif/{id}")]
	[Authorize]
	public async Task<IActionResult> DeleteNotification(int id)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
		if (userId is null) return Unauthorized();

		var ok = await _supportRepository.DeleteNotificationAsync(id, userId);
		if (!ok) return NotFound();

		return Ok();
	}
}


