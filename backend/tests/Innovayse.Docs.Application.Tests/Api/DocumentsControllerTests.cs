using Innovayse.Docs.API.Documents;
using Innovayse.Docs.API.Documents.Requests;
using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Api;

public class DocumentsControllerTests
{
    [Fact]
    public async Task Create_ReturnsCreatedDocumentWithCallerAsOwner()
    {
        var docRepo = new Mock<IDocumentRepository>();
        var permService = new Mock<IPermissionService>();
        var callerId = Guid.NewGuid();
        var controller = new DocumentsController(docRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Create(new CreateDocumentRequest { Title = "New Doc" });

        var created = Assert.IsType<CreatedResult>(result.Result);
        var doc = Assert.IsType<Document>(created.Value);
        Assert.Equal("New Doc", doc.Title);
        Assert.Equal(callerId, doc.OwnerId);
        docRepo.Verify(r => r.CreateAsync(It.Is<Document>(d => d.OwnerId == callerId)), Times.Once);
    }

    [Fact]
    public async Task List_ReturnsDocumentsForCaller()
    {
        var callerId = Guid.NewGuid();
        var documents = new List<Document> { new() { Id = Guid.NewGuid(), Title = "Mine", OwnerId = callerId } };
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.ListForUserAsync(callerId)).ReturnsAsync(documents);
        var permService = new Mock<IPermissionService>();
        var controller = new DocumentsController(docRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(documents, ok.Value);
    }

    [Fact]
    public async Task Get_UserWithNoRole_ReturnsForbid()
    {
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var callerId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.GetEffectiveRoleAsync(document.Id, callerId)).ReturnsAsync((DocumentRole?)null);
        var controller = new DocumentsController(docRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Get(document.Id);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Get_UserWithRole_ReturnsDocumentWithRole()
    {
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), Title = "Shared Doc" };
        var callerId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.GetEffectiveRoleAsync(document.Id, callerId)).ReturnsAsync(DocumentRole.Commenter);
        var controller = new DocumentsController(docRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Get(document.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<DocumentsController.DocumentWithRoleResponse>(ok.Value);
        Assert.Equal("Shared Doc", body.Title);
        Assert.Equal(DocumentRole.Commenter, body.Role);
    }

    [Fact]
    public async Task Move_EditorRole_UpdatesFolderId()
    {
        var callerId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = callerId };
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(document.Id, callerId, DocumentRole.Editor)).ReturnsAsync(true);
        var controller = new DocumentsController(docRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(callerId);

        var result = await controller.Move(document.Id, new DocumentsController.MoveDocumentRequest { FolderId = folderId });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<Document>(ok.Value);
        Assert.Equal(folderId, updated.FolderId);
        docRepo.Verify(r => r.UpdateAsync(It.Is<Document>(d => d.FolderId == folderId)), Times.Once);
    }

    [Fact]
    public async Task Move_WithoutEditorRole_ReturnsForbid()
    {
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permService = new Mock<IPermissionService>();
        permService.Setup(p => p.AuthorizeAsync(document.Id, It.IsAny<Guid>(), DocumentRole.Editor))
            .ReturnsAsync(false);
        var controller = new DocumentsController(docRepo.Object, permService.Object);
        controller.SetCallerIdForTesting(Guid.NewGuid());

        var result = await controller.Move(document.Id, new DocumentsController.MoveDocumentRequest());

        Assert.IsType<ForbidResult>(result.Result);
    }
}
