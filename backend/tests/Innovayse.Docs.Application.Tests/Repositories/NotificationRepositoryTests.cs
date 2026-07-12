using Innovayse.Docs.Domain.Notifications;
using Innovayse.Docs.Infrastructure.Persistence;
using Innovayse.Docs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Repositories;

public class NotificationRepositoryTests
{
    private static DocsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DocsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocsDbContext(options);
    }

    private static Notification NewNotification(Guid recipientId, DateTimeOffset? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        RecipientUserId = recipientId,
        Type = NotificationType.NewComment,
        ActorUserId = Guid.NewGuid(),
        ActorName = "Ada Lovelace",
        DocumentId = Guid.NewGuid(),
        PreviewText = "Looks good",
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task ListForUserAsync_ReturnsOnlyThatUsersNotifications_NewestFirst()
    {
        await using var context = CreateContext();
        var repo = new NotificationRepository(context);
        var userId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var older = NewNotification(userId, DateTimeOffset.UtcNow.AddMinutes(-10));
        var newer = NewNotification(userId, DateTimeOffset.UtcNow);
        var stranger = NewNotification(strangerId);
        await repo.CreateAsync(older);
        await repo.CreateAsync(newer);
        await repo.CreateAsync(stranger);

        var result = await repo.ListForUserAsync(userId);

        Assert.Equal(2, result.Count);
        Assert.Equal(newer.Id, result[0].Id);
        Assert.Equal(older.Id, result[1].Id);
        Assert.DoesNotContain(result, n => n.Id == stranger.Id);
    }

    [Fact]
    public async Task ListForUserAsync_RespectsLimit()
    {
        await using var context = CreateContext();
        var repo = new NotificationRepository(context);
        var userId = Guid.NewGuid();
        for (var i = 0; i < 5; i++) await repo.CreateAsync(NewNotification(userId));

        var result = await repo.ListForUserAsync(userId, limit: 3);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task MarkReadAsync_SetsReadAt()
    {
        await using var context = CreateContext();
        var repo = new NotificationRepository(context);
        var notification = NewNotification(Guid.NewGuid());
        await repo.CreateAsync(notification);

        await repo.MarkReadAsync(notification.Id);

        var updated = await repo.GetByIdAsync(notification.Id);
        Assert.NotNull(updated!.ReadAt);
    }

    [Fact]
    public async Task MarkReadAsync_AlreadyRead_IsNoOp()
    {
        await using var context = CreateContext();
        var repo = new NotificationRepository(context);
        var notification = NewNotification(Guid.NewGuid());
        await repo.CreateAsync(notification);
        await repo.MarkReadAsync(notification.Id);
        var firstReadAt = (await repo.GetByIdAsync(notification.Id))!.ReadAt;

        await repo.MarkReadAsync(notification.Id);

        var secondReadAt = (await repo.GetByIdAsync(notification.Id))!.ReadAt;
        Assert.Equal(firstReadAt, secondReadAt);
    }

    [Fact]
    public async Task MarkAllReadAsync_MarksOnlyThatUsersUnreadNotifications()
    {
        await using var context = CreateContext();
        var repo = new NotificationRepository(context);
        var userId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var mine1 = NewNotification(userId);
        var mine2 = NewNotification(userId);
        var stranger = NewNotification(strangerId);
        await repo.CreateAsync(mine1);
        await repo.CreateAsync(mine2);
        await repo.CreateAsync(stranger);

        await repo.MarkAllReadAsync(userId);

        Assert.NotNull((await repo.GetByIdAsync(mine1.Id))!.ReadAt);
        Assert.NotNull((await repo.GetByIdAsync(mine2.Id))!.ReadAt);
        Assert.Null((await repo.GetByIdAsync(stranger.Id))!.ReadAt);
    }
}
