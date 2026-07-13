using Innovayse.Docs.API.Documents;
using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class DocumentTabsControllerTests
{
    private static (DocumentTabsController controller, Mock<IDocumentTabRepository> tabRepo, Mock<IPermissionService> permService) MakeController(Guid callerId)
    {
        var tabRepo = new Mock<IDocumentTabRepository>();
        var permService = new Mock<IPermissionService>();
        var controller = new DocumentTabsController(tabRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);
        return (controller, tabRepo, permService);
    }

    [Fact]
    public async Task List_ReturnsTabsOrderedByOrderIndex()
    {
        var callerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var (controller, tabRepo, permService) = MakeController(callerId);
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Viewer)).ReturnsAsync(true);
        tabRepo.Setup(r => r.ListForDocumentAsync(documentId)).ReturnsAsync(
        [
            new DocumentTab { Id = Guid.NewGuid(), DocumentId = documentId, Title = "Tab 1", OrderIndex = 0 }
        ]);

        var result = await controller.List(documentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<List<DocumentTabsController.TabResponse>>(ok.Value);
        Assert.Single(body);
        Assert.Equal("Tab 1", body[0].Title);
    }

    [Fact]
    public async Task List_UserWithoutAccess_ReturnsForbid()
    {
        var callerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var (controller, _, permService) = MakeController(callerId);
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Viewer)).ReturnsAsync(false);

        var result = await controller.List(documentId);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Create_AppendsTabWithNextOrderIndex()
    {
        var callerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var (controller, tabRepo, permService) = MakeController(callerId);
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        tabRepo.Setup(r => r.ListForDocumentAsync(documentId)).ReturnsAsync(
        [
            new DocumentTab { Id = Guid.NewGuid(), DocumentId = documentId, Title = "Tab 1", OrderIndex = 0 },
            new DocumentTab { Id = Guid.NewGuid(), DocumentId = documentId, Title = "Tab 2", OrderIndex = 1 }
        ]);

        var result = await controller.Create(documentId, new DocumentTabsController.CreateTabRequest { Title = "Tab 3" });

        var created = Assert.IsType<CreatedResult>(result.Result);
        var body = Assert.IsType<DocumentTabsController.TabResponse>(created.Value);
        Assert.Equal("Tab 3", body.Title);
        Assert.Equal(2, body.OrderIndex);
        tabRepo.Verify(r => r.CreateAsync(It.Is<DocumentTab>(t => t.OrderIndex == 2 && t.DocumentId == documentId)), Times.Once);
    }

    [Fact]
    public async Task Update_RenamesTab()
    {
        var callerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var tabId = Guid.NewGuid();
        var (controller, tabRepo, permService) = MakeController(callerId);
        var tab = new DocumentTab { Id = tabId, DocumentId = documentId, Title = "Old title", OrderIndex = 0 };
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        tabRepo.Setup(r => r.GetByIdAsync(tabId)).ReturnsAsync(tab);

        var result = await controller.Update(documentId, tabId, new DocumentTabsController.UpdateTabRequest { Title = "New title" });

        Assert.IsType<OkObjectResult>(result.Result);
        tabRepo.Verify(r => r.UpdateAsync(It.Is<DocumentTab>(t => t.Title == "New title")), Times.Once);
    }

    [Fact]
    public async Task Update_TabBelongingToDifferentDocument_ReturnsNotFound()
    {
        var callerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var tabId = Guid.NewGuid();
        var (controller, tabRepo, permService) = MakeController(callerId);
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        tabRepo.Setup(r => r.GetByIdAsync(tabId)).ReturnsAsync(
            new DocumentTab { Id = tabId, DocumentId = Guid.NewGuid(), Title = "Other doc's tab", OrderIndex = 0 });

        var result = await controller.Update(documentId, tabId, new DocumentTabsController.UpdateTabRequest { Title = "New title" });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Delete_LastRemainingTab_ReturnsConflict()
    {
        var callerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var tabId = Guid.NewGuid();
        var (controller, tabRepo, permService) = MakeController(callerId);
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        tabRepo.Setup(r => r.GetByIdAsync(tabId)).ReturnsAsync(
            new DocumentTab { Id = tabId, DocumentId = documentId, Title = "Only tab", OrderIndex = 0 });
        tabRepo.Setup(r => r.ListForDocumentAsync(documentId)).ReturnsAsync(
        [
            new DocumentTab { Id = tabId, DocumentId = documentId, Title = "Only tab", OrderIndex = 0 }
        ]);

        var result = await controller.Delete(documentId, tabId);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        tabRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Delete_NotLastTab_Succeeds()
    {
        var callerId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var tabId = Guid.NewGuid();
        var (controller, tabRepo, permService) = MakeController(callerId);
        permService.Setup(p => p.AuthorizeAsync(documentId, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        tabRepo.Setup(r => r.GetByIdAsync(tabId)).ReturnsAsync(
            new DocumentTab { Id = tabId, DocumentId = documentId, Title = "Tab 1", OrderIndex = 0 });
        tabRepo.Setup(r => r.ListForDocumentAsync(documentId)).ReturnsAsync(
        [
            new DocumentTab { Id = tabId, DocumentId = documentId, Title = "Tab 1", OrderIndex = 0 },
            new DocumentTab { Id = Guid.NewGuid(), DocumentId = documentId, Title = "Tab 2", OrderIndex = 1 }
        ]);

        var result = await controller.Delete(documentId, tabId);

        Assert.IsType<NoContentResult>(result);
        tabRepo.Verify(r => r.DeleteAsync(tabId), Times.Once);
    }
}
