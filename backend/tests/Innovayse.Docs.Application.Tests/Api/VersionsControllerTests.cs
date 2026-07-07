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
    public async Task Create_EditorRole_StoresDecodedSnapshot()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var versionRepo = new Mock<IVersionRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        var controller = new VersionsController(versionRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var payload = Convert.ToBase64String(new byte[] { 9, 8, 7 });
        var result = await controller.Create(documentId, new VersionsController.CreateVersionRequest
        {
            SnapshotBase64 = payload,
            Label = "Before rewrite",
        });

        var created = Assert.IsType<CreatedResult>(result.Result);
        var version = Assert.IsType<DocumentVersion>(created.Value);
        Assert.Equal("Before rewrite", version.Label);
        Assert.Equal(callerId, version.CreatedBy);
        versionRepo.Verify(r => r.CreateVersionAsync(It.Is<DocumentVersion>(v =>
            v.DocumentId == documentId &&
            v.CreatedBy == callerId &&
            v.Snapshot.SequenceEqual(new byte[] { 9, 8, 7 }))), Times.Once);
    }

    [Fact]
    public async Task Create_WithoutEditorRole_ReturnsForbid()
    {
        var documentId = Guid.NewGuid();
        var versionRepo = new Mock<IVersionRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, It.IsAny<Guid>(), DocumentRole.Editor)).ReturnsAsync(false);
        var controller = new VersionsController(versionRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(Guid.NewGuid());

        var result = await controller.Create(documentId, new VersionsController.CreateVersionRequest
        {
            SnapshotBase64 = Convert.ToBase64String(new byte[] { 1 }),
        });

        Assert.IsType<ForbidResult>(result.Result);
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
