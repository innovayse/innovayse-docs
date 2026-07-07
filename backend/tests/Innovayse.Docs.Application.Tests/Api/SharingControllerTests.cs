using Innovayse.Docs.API.Sharing;
using Innovayse.Docs.Application.Identity;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class SharingControllerInviteTests
{
    [Fact]
    public async Task InviteUser_CallerNotOwner_ReturnsForbid()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(s => s.AuthorizeAsync(documentId, callerId, DocumentRole.Owner)).ReturnsAsync(false);
        var lookup = new Mock<ISsoUserLookupService>();
        var controller = new SharingController(
            new Mock<IPermissionRepository>().Object,
            new Mock<IShareLinkRepository>().Object,
            permissionService.Object,
            lookup.Object,
            new Mock<Innovayse.Docs.Application.Documents.IDocumentRepository>().Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.InviteUser(documentId, new SharingController.InviteUserRequest
        {
            Email = "someone@example.com",
            Role = DocumentRole.Viewer,
        });

        Assert.IsType<ForbidResult>(result);
        lookup.Verify(l => l.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteUser_UnknownEmail_ReturnsNotFound()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(s => s.AuthorizeAsync(documentId, callerId, DocumentRole.Owner)).ReturnsAsync(true);
        var lookup = new Mock<ISsoUserLookupService>();
        lookup.Setup(l => l.FindByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SsoUserLookupResult?)null);
        var permissionRepository = new Mock<IPermissionRepository>();
        var controller = new SharingController(
            permissionRepository.Object,
            new Mock<IShareLinkRepository>().Object,
            permissionService.Object,
            lookup.Object,
            new Mock<Innovayse.Docs.Application.Documents.IDocumentRepository>().Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.InviteUser(documentId, new SharingController.InviteUserRequest
        {
            Email = "nobody@example.com",
            Role = DocumentRole.Viewer,
        });

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        permissionRepository.Verify(r => r.GrantAsync(It.IsAny<DocumentPermission>()), Times.Never);
    }

    [Fact]
    public async Task InviteUser_KnownEmail_GrantsPermissionForResolvedUserId()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var resolvedUserId = Guid.NewGuid();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(s => s.AuthorizeAsync(documentId, callerId, DocumentRole.Owner)).ReturnsAsync(true);
        var lookup = new Mock<ISsoUserLookupService>();
        lookup.Setup(l => l.FindByEmailAsync("known@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SsoUserLookupResult(resolvedUserId, "known@example.com", "Known User"));
        var permissionRepository = new Mock<IPermissionRepository>();
        DocumentPermission? granted = null;
        permissionRepository.Setup(r => r.GrantAsync(It.IsAny<DocumentPermission>()))
            .Callback<DocumentPermission>(p => granted = p)
            .Returns(Task.CompletedTask);
        var controller = new SharingController(
            permissionRepository.Object,
            new Mock<IShareLinkRepository>().Object,
            permissionService.Object,
            lookup.Object,
            new Mock<Innovayse.Docs.Application.Documents.IDocumentRepository>().Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.InviteUser(documentId, new SharingController.InviteUserRequest
        {
            Email = "known@example.com",
            Role = DocumentRole.Editor,
        });

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(granted);
        Assert.Equal(resolvedUserId, granted!.UserId);
        Assert.Equal(documentId, granted.DocumentId);
        Assert.Equal(DocumentRole.Editor, granted.Role);
        Assert.Equal(callerId, granted.GrantedBy);
    }
}
