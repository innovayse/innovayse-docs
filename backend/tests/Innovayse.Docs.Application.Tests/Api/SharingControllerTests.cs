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
        }, CancellationToken.None);

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
        }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
        permissionRepository.Verify(r => r.UpsertAsync(It.IsAny<DocumentPermission>()), Times.Never);
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
        permissionRepository.Setup(r => r.UpsertAsync(It.IsAny<DocumentPermission>()))
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
        }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(granted);
        Assert.Equal(resolvedUserId, granted!.UserId);
        Assert.Equal(documentId, granted.DocumentId);
        Assert.Equal(DocumentRole.Editor, granted.Role);
        Assert.Equal(callerId, granted.GrantedBy);
    }

    [Fact]
    public async Task InviteUser_OwnerRole_ReturnsBadRequestWithoutLookup()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(s => s.AuthorizeAsync(documentId, callerId, DocumentRole.Owner)).ReturnsAsync(true);
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
            Role = DocumentRole.Owner,
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        lookup.Verify(l => l.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLink_OwnerRole_ReturnsBadRequest()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(s => s.AuthorizeAsync(documentId, callerId, DocumentRole.Owner)).ReturnsAsync(true);
        var shareLinkRepository = new Mock<IShareLinkRepository>();
        var controller = new SharingController(
            new Mock<IPermissionRepository>().Object,
            shareLinkRepository.Object,
            permissionService.Object,
            new Mock<ISsoUserLookupService>().Object,
            new Mock<Innovayse.Docs.Application.Documents.IDocumentRepository>().Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.CreateLink(documentId, new SharingController.CreateLinkRequest
        {
            Role = DocumentRole.Owner,
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        shareLinkRepository.Verify(r => r.CreateAsync(It.IsAny<ShareLink>()), Times.Never);
    }
}
