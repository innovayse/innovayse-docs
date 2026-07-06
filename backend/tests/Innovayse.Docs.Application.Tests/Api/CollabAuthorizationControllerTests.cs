using Innovayse.Docs.API.Collab;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class CollabAuthorizationControllerTests
{
    [Fact]
    public async Task Authorize_UserWithEditorRole_ReturnsRole()
    {
        var documentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(documentId, userId)).ReturnsAsync(DocumentRole.Editor);
        var controller = new CollabAuthorizationController(permRepo.Object);
        controller.SetCallerIdForTesting(userId);

        var result = await controller.Authorize(documentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<CollabAuthorizationController.AuthorizeResponse>(ok.Value);
        Assert.Equal(DocumentRole.Editor, body.Role);
    }

    [Fact]
    public async Task Authorize_UserWithoutRole_ReturnsForbid()
    {
        var documentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(documentId, userId)).ReturnsAsync((DocumentRole?)null);
        var controller = new CollabAuthorizationController(permRepo.Object);
        controller.SetCallerIdForTesting(userId);

        var result = await controller.Authorize(documentId);

        Assert.IsType<ForbidResult>(result.Result);
    }
}
