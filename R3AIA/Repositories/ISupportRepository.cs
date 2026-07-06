using Microsoft.EntityFrameworkCore;
using R3AIA.DTOs;
using R3AIA.Models;

namespace R3AIA.Repositories;

public interface ISupportRepository
{
	Task<UserReport> AddReportAsync(CreateReportDto dto, string reporterUserId);
	Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId);
	Task PushNotificationAsync(string userId, string message, string title = "R3AIA", string? actionUrl = null);
	Task<bool> MarkAsReadAsync(int notificationId, string userId);
	Task MarkAllAsReadAsync(string userId);
	Task<bool> DeleteNotificationAsync(int notificationId, string userId);
}

public class SupportRepository : ISupportRepository
{
	private readonly AppDbContext _context;

	public SupportRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<UserReport> AddReportAsync(CreateReportDto dto, string reporterUserId)
	{
		var report = new UserReport
		{
			ReporterId = reporterUserId,
			ReportedUserId = dto.ReportedUserId,
			Reason = dto.Reason
		};

		_context.UserReports.Add(report);

		// إشعار للمستخدم المُبلغ عنه
		await PushNotificationAsync(dto.ReportedUserId, "لديك بلاغ جديد.");

		await _context.SaveChangesAsync();
		return report;
	}

	public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId)
	{
		return await _context.Notifications
			.Where(n => n.UserId == userId)
			.OrderByDescending(n => n.CreatedAt)
			.ToListAsync();
	}

	public async Task PushNotificationAsync(string userId, string message, string title = "R3AIA", string? actionUrl = null)
	{
		var notification = new Notification
		{
			UserId = userId,
			Title = title,
			Message = message,
			ActionUrl = actionUrl
		};

		_context.Notifications.Add(notification);
		await _context.SaveChangesAsync();
	}

	public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
	{
		var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
		if (notif == null) return false;

		notif.IsRead = true;
		await _context.SaveChangesAsync();
		return true;
	}

	public async Task MarkAllAsReadAsync(string userId)
	{
		var unreadNotifs = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
		foreach (var n in unreadNotifs)
		{
			n.IsRead = true;
		}
		await _context.SaveChangesAsync();
	}

	public async Task<bool> DeleteNotificationAsync(int notificationId, string userId)
	{
		var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
		if (notif == null) return false;

		_context.Notifications.Remove(notif);
		await _context.SaveChangesAsync();
		return true;
	}
}


