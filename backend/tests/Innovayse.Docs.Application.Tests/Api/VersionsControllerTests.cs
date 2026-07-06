// tests/Innovayse.Docs.Application.Tests/Api/VersionsControllerTests.cs
using Innovayse.Docs.API.Versions;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Application.Versions;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Domain.Versions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class VersionsControllerTests
{
    [Fact]
    public async Task List_ViewerRole_ReturnsVersions()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var versionRepo = new Mock<IVersionRepository>();
        versionRepo.Setup(r => r.ListForDocumentAsync(documentId))
            .ReturnsAsync(new List<DocumentVersion> { new() { Id = Guid.NewGuid(), DocumentId = documentId } });
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Viewer)).ReturnsAsync(true);
        var controller = new VersionsController(versionRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.List(documentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var versions = Assert.IsType<List<DocumentVersion>>(ok.Value);
        Assert.Single(versions);
    }

    [Fact]
    public async Task Restore_EditorRole_CallsRepository()
    {
        var documentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var versionRepo = new Mock<IVersionRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        var controller = new VersionsController(versionRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Restore(documentId, versionId);

        Assert.IsType<NoContentResult>(result);
        versionRepo.Verify(r => r.RestoreAsync(documentId, versionId), Times.Once);
    }
}
