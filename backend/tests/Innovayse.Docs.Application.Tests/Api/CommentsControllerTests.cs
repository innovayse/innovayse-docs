using Innovayse.Docs.API.Comments;
using Innovayse.Docs.Application.Comments;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Comments;
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
        var controller = new CommentsController(commentRepo.Object, permService.Object);
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
        var controller = new CommentsController(commentRepo.Object, permService.Object);
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
        var controller = new CommentsController(commentRepo.Object, permService.Object);
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
}
