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
}
