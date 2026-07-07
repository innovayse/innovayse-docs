using Innovayse.Docs.API.Folders;
using Innovayse.Docs.Application.Folders;
using Innovayse.Docs.Domain.Documents;
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
        var callerId = Guid.NewGuid();
        var controller = new FoldersController(folderRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Create(new FoldersController.CreateFolderRequest { Name = "My Folder" });

        var created = Assert.IsType<CreatedResult>(result.Result);
        var folder = Assert.IsType<Folder>(created.Value);
        Assert.Equal("My Folder", folder.Name);
        Assert.Equal(callerId, folder.OwnerId);
    }

    [Fact]
    public async Task List_ReturnsFoldersForCaller()
    {
        var callerId = Guid.NewGuid();
        var folders = new List<Folder> { new() { Id = Guid.NewGuid(), Name = "Mine", OwnerId = callerId } };
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.ListForUserAsync(callerId)).ReturnsAsync(folders);
        var controller = new FoldersController(folderRepo.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(folders, ok.Value);
    }

    [Fact]
    public async Task Delete_Owner_DeletesFolder()
    {
        var callerId = Guid.NewGuid();
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Mine", OwnerId = callerId };
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
        var controller = new FoldersController(folderRepo.Object);
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
        var controller = new FoldersController(folderRepo.Object);
        controller.SetCallerIdForTesting(Guid.NewGuid());

        var result = await controller.Delete(folder.Id);

        Assert.IsType<ForbidResult>(result);
        folderRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
