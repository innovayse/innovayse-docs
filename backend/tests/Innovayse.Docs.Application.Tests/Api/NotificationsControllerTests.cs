using Innovayse.Docs.API.Notifications;
using Innovayse.Docs.Application.Notifications;
using Innovayse.Docs.Domain.Notifications;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class NotificationsControllerTests
{
    [Fact]
    public async Task List_ReturnsCallersNotifications()
    {
        var callerId = Guid.NewGuid();
        var notifications = new List<Notification> { new() { Id = Guid.NewGuid(), RecipientUserId = callerId } };
        var notificationRepo = new Mock<INotificationRepository>();
        notificationRepo.Setup(r => r.ListForUserAsync(callerId, 50)).ReturnsAsync(notifications);
        var controller = new NotificationsController(notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(notifications, ok.Value);
    }

    [Fact]
    public async Task MarkRead_OwnedByCaller_MarksRead()
    {
        var callerId = Guid.NewGuid();
        var notification = new Notification { Id = Guid.NewGuid(), RecipientUserId = callerId };
        var notificationRepo = new Mock<INotificationRepository>();
        notificationRepo.Setup(r => r.GetByIdAsync(notification.Id)).ReturnsAsync(notification);
        var controller = new NotificationsController(notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.MarkRead(notification.Id);

        Assert.IsType<NoContentResult>(result);
        notificationRepo.Verify(r => r.MarkReadAsync(notification.Id), Times.Once);
    }

    [Fact]
    public async Task MarkRead_BelongsToDifferentUser_ReturnsNotFound()
    {
        var callerId = Guid.NewGuid();
        var notification = new Notification { Id = Guid.NewGuid(), RecipientUserId = Guid.NewGuid() };
        var notificationRepo = new Mock<INotificationRepository>();
        notificationRepo.Setup(r => r.GetByIdAsync(notification.Id)).ReturnsAsync(notification);
        var controller = new NotificationsController(notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.MarkRead(notification.Id);

        Assert.IsType<NotFoundResult>(result);
        notificationRepo.Verify(r => r.MarkReadAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task MarkRead_UnknownId_ReturnsNotFound()
    {
        var callerId = Guid.NewGuid();
        var notificationRepo = new Mock<INotificationRepository>();
        notificationRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Notification?)null);
        var controller = new NotificationsController(notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.MarkRead(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MarkAllRead_CallsRepositoryForCaller()
    {
        var callerId = Guid.NewGuid();
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new NotificationsController(notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.MarkAllRead();

        Assert.IsType<NoContentResult>(result);
        notificationRepo.Verify(r => r.MarkAllReadAsync(callerId), Times.Once);
    }
}
