using Innovayse.Docs.API.Comments;
using Innovayse.Docs.Application.Comments;
using Innovayse.Docs.Application.Notifications;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Comments;
using Innovayse.Docs.Domain.Notifications;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class CommentsControllerTests
{
    [Fact]
    public async Task Create_CommenterRole_Allowed()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var commentRepo = new Mock<ICommentRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Commenter))
            .ReturnsAsync(true);
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new CommentsController(commentRepo.Object, permService.Object, notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Create(documentId, new CommentsController.CreateCommentRequest
        {
            Text = "Looks good",
            AnchorPosition = 42
        });

        var created = Assert.IsType<CreatedResult>(result.Result);
        var comment = Assert.IsType<Comment>(created.Value);
        Assert.Equal("Looks good", comment.Text);
        Assert.Equal(callerId, comment.AuthorId);
    }

    [Fact]
    public async Task Create_ViewerRole_Forbidden()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var commentRepo = new Mock<ICommentRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Commenter))
            .ReturnsAsync(false);
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new CommentsController(commentRepo.Object, permService.Object, notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Create(documentId, new CommentsController.CreateCommentRequest
        {
            Text = "Looks good",
            AnchorPosition = 42
        });

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Create_PersistsAuthorNameAndParentCommentId()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var parentCommentId = Guid.NewGuid();
        var commentRepo = new Mock<ICommentRepository>();
        Comment? created = null;
        commentRepo.Setup(r => r.CreateAsync(It.IsAny<Comment>()))
            .Callback<Comment>(c => created = c)
            .Returns(Task.CompletedTask);
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Commenter))
            .ReturnsAsync(true);
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new CommentsController(commentRepo.Object, permService.Object, notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        await controller.Create(documentId, new CommentsController.CreateCommentRequest
        {
            Text = "Replying here",
            AuthorName = "Ada Lovelace",
            AnchorPosition = 10,
            ParentCommentId = parentCommentId,
        });

        Assert.NotNull(created);
        Assert.Equal("Ada Lovelace", created!.AuthorName);
        Assert.Equal(parentCommentId, created.ParentCommentId);
    }

    [Fact]
    public async Task Update_CommenterRole_TogglesResolved()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var comment = new Comment { Id = Guid.NewGuid(), DocumentId = documentId, Resolved = false };
        var commentRepo = new Mock<ICommentRepository>();
        commentRepo.Setup(r => r.GetByIdAsync(comment.Id)).ReturnsAsync(comment);
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Commenter))
            .ReturnsAsync(true);
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new CommentsController(commentRepo.Object, permService.Object, notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Update(documentId, comment.Id, new CommentsController.UpdateCommentRequest { Resolved = true });

        Assert.IsType<NoContentResult>(result);
        commentRepo.Verify(r => r.UpdateAsync(It.Is<Comment>(c => c.Id == comment.Id && c.Resolved)), Times.Once);
    }

    [Fact]
    public async Task Update_ViewerRole_ReturnsForbidWithoutTouchingComment()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var commentRepo = new Mock<ICommentRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Commenter))
            .ReturnsAsync(false);
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new CommentsController(commentRepo.Object, permService.Object, notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Update(documentId, Guid.NewGuid(), new CommentsController.UpdateCommentRequest { Resolved = true });

        Assert.IsType<ForbidResult>(result);
        commentRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Update_CommentBelongsToDifferentDocument_ReturnsNotFound()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var comment = new Comment { Id = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Resolved = false };
        var commentRepo = new Mock<ICommentRepository>();
        commentRepo.Setup(r => r.GetByIdAsync(comment.Id)).ReturnsAsync(comment);
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Commenter))
            .ReturnsAsync(true);
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new CommentsController(commentRepo.Object, permService.Object, notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Update(documentId, comment.Id, new CommentsController.UpdateCommentRequest { Resolved = true });

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_TopLevelComment_NotifiesOtherParticipantsNotAuthor()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var otherParticipantId = Guid.NewGuid();
        var commentRepo = new Mock<ICommentRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Commenter)).ReturnsAsync(true);
        permService.Setup(p => p.GetDocumentParticipantUserIdsAsync(documentId))
            .ReturnsAsync(new List<Guid> { callerId, otherParticipantId });
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new CommentsController(commentRepo.Object, permService.Object, notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        await controller.Create(documentId, new CommentsController.CreateCommentRequest
        {
            Text = "New comment", AuthorName = "Ada", AnchorPosition = 0,
        });

        notificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n =>
            n.RecipientUserId == otherParticipantId &&
            n.Type == NotificationType.NewComment &&
            n.DocumentId == documentId &&
            n.ActorUserId == callerId)), Times.Once);
        notificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.RecipientUserId == callerId)), Times.Never);
    }

    [Fact]
    public async Task Create_Reply_NotifiesWithReplyType()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var otherParticipantId = Guid.NewGuid();
        var commentRepo = new Mock<ICommentRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Commenter)).ReturnsAsync(true);
        permService.Setup(p => p.GetDocumentParticipantUserIdsAsync(documentId))
            .ReturnsAsync(new List<Guid> { callerId, otherParticipantId });
        var notificationRepo = new Mock<INotificationRepository>();
        var controller = new CommentsController(commentRepo.Object, permService.Object, notificationRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        await controller.Create(documentId, new CommentsController.CreateCommentRequest
        {
            Text = "A reply", AuthorName = "Ada", AnchorPosition = 0, ParentCommentId = Guid.NewGuid(),
        });

        notificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n => n.Type == NotificationType.NewReply)), Times.Once);
    }
}
