using DevSync.Api.Data;
using DevSync.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevSync.Api.Controllers;

[ApiController]
[Route("api/users/{userId:int}/notifications")]
public sealed class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetNotifications(
        int userId,
        CancellationToken cancellationToken)
    {
        var userExists = await db.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return NotFound("사용자를 찾을 수 없습니다.");
        }

        return await db.Notifications
            .AsNoTracking()
            .Include(x => x.Room)
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedAt)
            .Take(50)
            .Select(x => new NotificationDto(
                x.Id,
                x.UserId,
                x.RoomId,
                x.Room!.Name,
                x.ChatMessageId,
                x.Type,
                x.Title,
                x.Body,
                x.IsRead,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    [HttpPatch("{notificationId:int}/read")]
    public async Task<ActionResult> MarkAsRead(
        int userId,
        int notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return NotFound("알림을 찾을 수 없습니다.");
        }

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult> MarkAllAsRead(
        int userId,
        CancellationToken cancellationToken)
    {
        var notifications = await db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
