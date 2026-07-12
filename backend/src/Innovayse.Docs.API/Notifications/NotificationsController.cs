using System.Security.Claims;
using Innovayse.Docs.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Notifications;

[ApiController]
[Route("notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;
    private Guid? _callerIdOverride;

    public NotificationsController(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    [HttpGet]
    public async Task<ActionResult<List<Innovayse.Docs.Domain.Notifications.Notification>>> List() =>
        Ok(await _notificationRepository.ListForUserAsync(CallerId));

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var notification = await _notificationRepository.GetByIdAsync(id);
        if (notification is null || notification.RecipientUserId != CallerId)
            return NotFound();

        await _notificationRepository.MarkReadAsync(id);
        return NoContent();
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notificationRepository.MarkAllReadAsync(CallerId);
        return NoContent();
    }
}
