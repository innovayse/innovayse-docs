using Innovayse.Docs.API.Folders;
using Innovayse.Docs.Application.Folders;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class FoldersControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedFolderWithCallerAsOwner()
    {
        var folderRepo = new Mock<IFolderRepository>();
        var permissionService = new Mock<IPermissionService>();
        var callerId = Guid.NewGuid();
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Create(new FoldersController.CreateFolderRequest { Name = "My Folder" });

        var created = Assert.IsType<CreatedResult>(result.Result);
        var folder = Assert.IsType<Folder>(created.Value);
        Assert.Equal("My Folder", folder.Name);
        Assert.Equal(callerId, folder.OwnerId);
    }

    [Fact]
    public async Task Create_WithParentFolder_SucceedsWhenCallerHasEditorAccess()
    {
        var folderRepo = new Mock<IFolderRepository>();
        var permissionService = new Mock<IPermissionService>();
        var callerId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        permissionService
            .Setup(p => p.AuthorizeFolderAsync(parentFolderId, callerId, DocumentRole.Editor))
            .ReturnsAsync(true);
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Create(new FoldersController.CreateFolderRequest
        {
            Name = "Subfolder",
            ParentFolderId = parentFolderId
        });

        var created = Assert.IsType<CreatedResult>(result.Result);
        var folder = Assert.IsType<Folder>(created.Value);
        Assert.Equal("Subfolder", folder.Name);
        Assert.Equal(callerId, folder.OwnerId);
        Assert.Equal(parentFolderId, folder.ParentFolderId);
        folderRepo.Verify(r => r.CreateAsync(It.IsAny<Folder>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithParentFolder_ForbidsWhenCallerLacksAccess()
    {
        var folderRepo = new Mock<IFolderRepository>();
        var permissionService = new Mock<IPermissionService>();
        var callerId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        permissionService
            .Setup(p => p.AuthorizeFolderAsync(parentFolderId, callerId, DocumentRole.Editor))
            .ReturnsAsync(false);
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Create(new FoldersController.CreateFolderRequest
        {
            Name = "Subfolder",
            ParentFolderId = parentFolderId
        });

        Assert.IsType<ForbidResult>(result.Result);
        folderRepo.Verify(r => r.CreateAsync(It.IsAny<Folder>()), Times.Never);
    }

    [Fact]
    public async Task List_ReturnsFoldersWithCallerRole()
    {
        var callerId = Guid.NewGuid();
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Mine", OwnerId = callerId };
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.ListForUserAsync(callerId)).ReturnsAsync(new List<Folder> { folder });
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.GetEffectiveFolderRoleAsync(folder.Id, callerId)).ReturnsAsync(DocumentRole.Owner);
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsType<List<FoldersController.FolderWithRoleResponse>>(ok.Value);
        var entry = Assert.Single(list);
        Assert.Equal(folder.Id, entry.Id);
        Assert.Equal(DocumentRole.Owner, entry.Role);
    }

    [Fact]
    public async Task Get_CallerHasNoRole_ReturnsForbid()
    {
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Someone else's", OwnerId = Guid.NewGuid() };
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.GetEffectiveFolderRoleAsync(folder.Id, It.IsAny<Guid>())).ReturnsAsync((DocumentRole?)null);
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(Guid.NewGuid());

        var result = await controller.Get(folder.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Get_CallerHasInheritedFolderRole_ReturnsOkWithRole()
    {
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Shared", OwnerId = Guid.NewGuid() };
        var callerId = Guid.NewGuid();
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
        var permissionService = new Mock<IPermissionService>();
        permissionService.Setup(p => p.GetEffectiveFolderRoleAsync(folder.Id, callerId)).ReturnsAsync(DocumentRole.Editor);
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Get(folder.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FoldersController.FolderWithRoleResponse>(ok.Value);
        Assert.Equal(DocumentRole.Editor, response.Role);
    }

    [Fact]
    public async Task Get_UnknownFolder_ReturnsNotFound()
    {
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Folder?)null);
        var permissionService = new Mock<IPermissionService>();
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(Guid.NewGuid());

        var result = await controller.Get(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_Owner_DeletesFolder()
    {
        var callerId = Guid.NewGuid();
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Mine", OwnerId = callerId };
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
        var permissionService = new Mock<IPermissionService>();
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Delete(folder.Id);

        Assert.IsType<NoContentResult>(result);
        folderRepo.Verify(r => r.DeleteAsync(folder.Id), Times.Once);
    }

    [Fact]
    public async Task Delete_NotOwner_ReturnsForbid()
    {
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Mine", OwnerId = Guid.NewGuid() };
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
        var permissionService = new Mock<IPermissionService>();
        var controller = new FoldersController(folderRepo.Object, permissionService.Object);
        controller.SetCallerIdForTesting(Guid.NewGuid());

        var result = await controller.Delete(folder.Id);

        Assert.IsType<ForbidResult>(result);
        folderRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
