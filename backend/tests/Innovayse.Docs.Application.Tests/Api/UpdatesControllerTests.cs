using Innovayse.Docs.API.Versions;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Application.Versions;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Domain.Versions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class UpdatesControllerTests
{
    [Fact]
    public async Task Append_EditorRole_StoresDecodedUpdate()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var versionRepo = new Mock<IVersionRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        var controller = new UpdatesController(versionRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var payload = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var result = await controller.Append(documentId, new UpdatesController.AppendUpdateRequest
        {
            UpdateBase64 = payload
        });

        Assert.IsType<NoContentResult>(result);
        versionRepo.Verify(r => r.AppendUpdateAsync(It.Is<DocumentUpdate>(u =>
            u.DocumentId == documentId &&
            u.AuthorId == callerId &&
            u.UpdateBinary.SequenceEqual(new byte[] { 1, 2, 3 }))), Times.Once);
    }

    [Fact]
    public async Task List_ViewerRole_ReturnsEncodedUpdatesInOrder()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var versionRepo = new Mock<IVersionRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Viewer)).ReturnsAsync(true);
        versionRepo.Setup(r => r.ListUpdatesAsync(documentId)).ReturnsAsync(new List<DocumentUpdate>
        {
            new() { Id = Guid.NewGuid(), DocumentId = documentId, UpdateBinary = new byte[] { 1, 2, 3 }, AuthorId = callerId, CreatedAt = DateTimeOffset.UtcNow },
        });
        var controller = new UpdatesController(versionRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.List(documentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IEnumerable<UpdatesController.UpdateResponse>>(ok.Value);
        Assert.Equal(Convert.ToBase64String(new byte[] { 1, 2, 3 }), Assert.Single(body).UpdateBase64);
    }

    [Fact]
    public async Task List_NoAccess_ReturnsForbid()
    {
        var documentId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var versionRepo = new Mock<IVersionRepository>();
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Viewer)).ReturnsAsync(false);
        var controller = new UpdatesController(versionRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.List(documentId);

        Assert.IsType<ForbidResult>(result.Result);
    }
}
