using Innovayse.Docs.Domain.Notifications;

namespace Innovayse.Docs.Application.Notifications;

public interface INotificationRepository
{
    Task CreateAsync(Notification notification);

    /// <summary>Most recent notifications for a user, newest first, capped at <paramref name="limit"/>.</summary>
    Task<List<Notification>> ListForUserAsync(Guid userId, int limit = 50);

    Task<Notification?> GetByIdAsync(Guid id);

    /// <summary>Sets ReadAt to now if not already set. No-op if already read.</summary>
    Task MarkReadAsync(Guid id);

    /// <summary>Sets ReadAt to now for every currently-unread notification belonging to userId.</summary>
    Task MarkAllReadAsync(Guid userId);
}
