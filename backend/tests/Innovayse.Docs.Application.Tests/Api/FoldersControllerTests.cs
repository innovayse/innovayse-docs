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
}
