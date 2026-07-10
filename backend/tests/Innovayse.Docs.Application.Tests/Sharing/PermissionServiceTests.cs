using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Folders;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Moq;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Sharing;

public class PermissionServiceTests
{
    private static Mock<IFolderRepository> EmptyFolderRepo()
    {
        var repo = new Mock<IFolderRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Folder?)null);
        return repo;
    }

    [Fact]
    public async Task AuthorizeAsync_Owner_AlwaysAllowed()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = ownerId };
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, EmptyFolderRepo().Object);

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

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, EmptyFolderRepo().Object);

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

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, EmptyFolderRepo().Object);

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

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, EmptyFolderRepo().Object);

        Assert.False(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Editor));
    }

    [Fact]
    public async Task AuthorizeAsync_UnknownDocument_Denied()
    {
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Document?)null);
        var permRepo = new Mock<IPermissionRepository>();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, EmptyFolderRepo().Object);

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
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, OwnerId = Guid.NewGuid() });

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.True(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Commenter));
    }

    [Fact]
    public async Task AuthorizeAsync_HigherFolderGrant_OverridesLowerDocumentGrant()
    {
        // Highest-role-wins: a direct Viewer grant on the document does not cap access when
        // an ancestor folder grants Editor — the max of the two applies. (This supersedes the
        // prior "document grant always wins" behavior, changed by explicit product decision
        // when folder sharing was added — see 2026-07-10-folder-sharing-design.md.)
        var folderId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), FolderId = folderId };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync(DocumentRole.Viewer);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folderId, userId)).ReturnsAsync(DocumentRole.Editor);
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, OwnerId = Guid.NewGuid() });

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.True(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Editor));
    }

    [Fact]
    public async Task AuthorizeAsync_HigherDocumentGrant_OverridesLowerFolderGrant()
    {
        var folderId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), FolderId = folderId };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync(DocumentRole.Editor);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folderId, userId)).ReturnsAsync(DocumentRole.Viewer);
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, OwnerId = Guid.NewGuid() });

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.True(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Editor));
    }

    [Fact]
    public async Task AuthorizeAsync_NoDocumentAndNoFolderGrant_Denied()
    {
        var folderId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), FolderId = folderId };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync((DocumentRole?)null);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folderId, userId)).ReturnsAsync((DocumentRole?)null);
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(new Folder { Id = folderId, OwnerId = Guid.NewGuid() });

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.False(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Viewer));
    }

    [Fact]
    public async Task AuthorizeAsync_GrantOnGrandparentFolder_IsInherited()
    {
        // document -> folderId (no direct grant) -> parentFolderId (Editor grant for userId)
        var parentFolderId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), FolderId = folderId };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync((DocumentRole?)null);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folderId, userId)).ReturnsAsync((DocumentRole?)null);
        folderPermRepo.Setup(r => r.GetRoleAsync(parentFolderId, userId)).ReturnsAsync(DocumentRole.Editor);
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folderId))
            .ReturnsAsync(new Folder { Id = folderId, OwnerId = Guid.NewGuid(), ParentFolderId = parentFolderId });
        folderRepo.Setup(r => r.GetByIdAsync(parentFolderId))
            .ReturnsAsync(new Folder { Id = parentFolderId, OwnerId = Guid.NewGuid() });

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.True(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Editor));
    }

    [Fact]
    public async Task AuthorizeAsync_OrphanedAncestorFolder_StopsWalkWithoutThrowing()
    {
        // folderId's ParentFolderId points at a folder that no longer exists (defensive case:
        // the walk must stop cleanly, not throw a NullReferenceException).
        var missingParentId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var document = new Document { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), FolderId = folderId };
        var userId = Guid.NewGuid();
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var permRepo = new Mock<IPermissionRepository>();
        permRepo.Setup(r => r.GetRoleAsync(document.Id, userId)).ReturnsAsync((DocumentRole?)null);
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folderId, userId)).ReturnsAsync((DocumentRole?)null);
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folderId))
            .ReturnsAsync(new Folder { Id = folderId, OwnerId = Guid.NewGuid(), ParentFolderId = missingParentId });
        folderRepo.Setup(r => r.GetByIdAsync(missingParentId)).ReturnsAsync((Folder?)null);

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.False(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Viewer));
    }

    [Fact]
    public async Task GetEffectiveFolderRoleAsync_Owner_ReturnsOwner()
    {
        var ownerId = Guid.NewGuid();
        var folder = new Folder { Id = Guid.NewGuid(), OwnerId = ownerId };
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
        var docRepo = new Mock<IDocumentRepository>();
        var permRepo = new Mock<IPermissionRepository>();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.Equal(DocumentRole.Owner, await sut.GetEffectiveFolderRoleAsync(folder.Id, ownerId));
    }

    [Fact]
    public async Task GetEffectiveFolderRoleAsync_DirectGrant_ReturnsGrantedRole()
    {
        var folder = new Folder { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
        var docRepo = new Mock<IDocumentRepository>();
        var permRepo = new Mock<IPermissionRepository>();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folder.Id, userId)).ReturnsAsync(DocumentRole.Commenter);

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.Equal(DocumentRole.Commenter, await sut.GetEffectiveFolderRoleAsync(folder.Id, userId));
    }

    [Fact]
    public async Task GetEffectiveFolderRoleAsync_NoGrant_ReturnsNull()
    {
        var folder = new Folder { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        var userId = Guid.NewGuid();
        var folderRepo = new Mock<IFolderRepository>();
        folderRepo.Setup(r => r.GetByIdAsync(folder.Id)).ReturnsAsync(folder);
        var docRepo = new Mock<IDocumentRepository>();
        var permRepo = new Mock<IPermissionRepository>();
        var folderPermRepo = new Mock<IFolderPermissionRepository>();
        folderPermRepo.Setup(r => r.GetRoleAsync(folder.Id, userId)).ReturnsAsync((DocumentRole?)null);

        var sut = new PermissionService(docRepo.Object, permRepo.Object, folderPermRepo.Object, folderRepo.Object);

        Assert.Null(await sut.GetEffectiveFolderRoleAsync(folder.Id, userId));
    }
}
