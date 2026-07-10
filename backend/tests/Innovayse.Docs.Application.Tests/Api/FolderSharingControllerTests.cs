using Innovayse.Docs.API.Folders;
using Innovayse.Docs.Application.Identity;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class FolderSharingControllerTests
{
    [Fact]
    public async Task InviteUser_Owner_UpsertsFolderPermission()
    {
        var folderId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var invitedUserId = Guid.NewGuid();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.AuthorizeFolderAsync(folderId, callerId, DocumentRole.Owner)).ReturnsAsync(true);
        var lookup = new Mock<ISsoUserLookupService>();
        lookup.Setup(l => l.FindByEmailAsync("friend@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SsoUserLookupResult(invitedUserId, "friend@example.com", "Friend"));
        var controller = new FolderSharingController(folderPermRepo.Object, permissionService.Object, lookup.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.InviteUser(folderId,
            new FolderSharingController.InviteUserRequest { Email = "friend@example.com", Role = DocumentRole.Editor },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        folderPermRepo.Verify(r => r.UpsertAsync(It.Is<FolderPermission>(p =>
            p.FolderId == folderId && p.UserId == invitedUserId && p.Role == DocumentRole.Editor && p.GrantedBy == callerId)),
            Times.Once);
    }

    [Fact]
    public async Task InviteUser_NotOwner_ReturnsForbid()
    {
        var folderId = Guid.NewGuid();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.AuthorizeFolderAsync(folderId, It.IsAny<Guid>(), DocumentRole.Owner)).ReturnsAsync(false);
        var lookup = new Mock<ISsoUserLookupService>();
        var controller = new FolderSharingController(folderPermRepo.Object, permissionService.Object, lookup.Object);
        controller.SetCallerIdForTesting(Guid.NewGuid());

        var result = await controller.InviteUser(folderId,
            new FolderSharingController.InviteUserRequest { Email = "friend@example.com", Role = DocumentRole.Editor },
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        folderPermRepo.Verify(r => r.UpsertAsync(It.IsAny<FolderPermission>()), Times.Never);
    }

    [Fact]
    public async Task InviteUser_OwnerRole_ReturnsBadRequest()
    {
        var folderId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.AuthorizeFolderAsync(folderId, callerId, DocumentRole.Owner)).ReturnsAsync(true);
        var lookup = new Mock<ISsoUserLookupService>();
        var controller = new FolderSharingController(folderPermRepo.Object, permissionService.Object, lookup.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.InviteUser(folderId,
            new FolderSharingController.InviteUserRequest { Email = "friend@example.com", Role = DocumentRole.Owner },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task InviteUser_UnknownEmail_ReturnsNotFound()
    {
        var folderId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.AuthorizeFolderAsync(folderId, callerId, DocumentRole.Owner)).ReturnsAsync(true);
        var lookup = new Mock<ISsoUserLookupService>();
        lookup.Setup(l => l.FindByEmailAsync("ghost@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SsoUserLookupResult?)null);
        var controller = new FolderSharingController(folderPermRepo.Object, permissionService.Object, lookup.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.InviteUser(folderId,
            new FolderSharingController.InviteUserRequest { Email = "ghost@example.com", Role = DocumentRole.Viewer },
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
