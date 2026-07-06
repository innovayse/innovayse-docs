using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Sharing;

public class PermissionServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_Owner_AlwaysAllowed()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = ownerId };
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object);

        Assert.True(await sut.AuthorizeAsync(document.Id, ownerId, DocumentRole.Owner));
    }

    [Fact]
    public async Task AuthorizeAsync_DirectGrant_SatisfiesLowerRequirement()
    {
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync(DocumentRole.Editor);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object);

        Assert.True(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Commenter));
    }

    [Fact]
    public async Task AuthorizeAsync_NoGrant_Denied()
    {
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync((DocumentRole?)null);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object);

        Assert.False(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Viewer));
    }

    [Fact]
    public async Task AuthorizeAsync_GrantBelowRequirement_Denied()
    {
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync(DocumentRole.Viewer);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object);

        Assert.False(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Editor));
    }

    [Fact]
    public async Task AuthorizeAsync_UnknownDocument_Denied()
    {
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Document?)null);
        var permRepo = new Mock<IPermissionRepository>();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object);

        Assert.False(await sut.AuthorizeAsync(Guid.NewGuid(), Guid.NewGuid(), DocumentRole.Viewer));
    }

    [Fact]
    public async Task AuthorizeAsync_NoDocumentGrant_FallsBackToFolderGrant()
    {
        var folderId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), FolderId = folderId };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync((DocumentRole?)null);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folderId, userId)).ReturnsAsync(DocumentRole.Editor);

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object);

        Assert.True(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Commenter));
    }

    [Fact]
    public async Task AuthorizeAsync_DocumentLevelGrant_OverridesFolderGrant()
    {
        var folderId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), FolderId = folderId };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync(DocumentRole.Viewer);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folderId, userId)).ReturnsAsync(DocumentRole.Editor);

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object);

        // Document-level Viewer grant exists, so it is used as-is (not overridden by the
        // higher folder-level Editor grant) — an explicit document override always wins.
        Assert.False(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Editor));
    }

    [Fact]
    public async Task AuthorizeAsync_NoDocumentAndNoFolderGrant_Denied()
    {
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), FolderId = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync((DocumentRole?)null);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(document.FolderId!.Value, userId)).ReturnsAsync((DocumentRole?)null);

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object);

        Assert.False(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Viewer));
    }
}
