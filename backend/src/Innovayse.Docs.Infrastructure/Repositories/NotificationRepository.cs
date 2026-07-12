using Innovayse.Docs.Application.Notifications;
using Innovayse.Docs.Domain.Notifications;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly DocsDbContext _context;
    public NotificationRepository(DocsDbContext context) => _context = context;

    public async Task CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public Task<List<Notification>> ListForUserAsync(Guid userId, int limit = 50) =>
        _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();

    public Task<Notification?> GetByIdAsync(Guid id) =>
        _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);

    public async Task MarkReadAsync(Guid id)
    {
        var notification = await GetByIdAsync(id);
        if (notification is null || notification.ReadAt.HasValue) return;
        notification.ReadAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .ToListAsync();
        var now = DateTimeOffset.UtcNow;
        foreach (var notification in unread) notification.ReadAt = now;
        await _context.SaveChangesAsync();
    }
}
