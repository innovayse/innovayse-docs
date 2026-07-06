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

        var sut = new PermissionService(docRepo.Object, permRepo.Object);

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

        var sut = new PermissionService(docRepo.Object, permRepo.Object);

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

        var sut = new PermissionService(docRepo.Object, permRepo.Object);

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

        var sut = new PermissionService(docRepo.Object, permRepo.Object);

        Assert.False(await sut.AuthorizeAsync(document.Id, userId, DocumentRole.Editor));
    }

    [Fact]
    public async Task AuthorizeAsync_UnknownDocument_Denied()
    {
        var docRepo = new Mock<IDocumentRepository>();
        docRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Document?)null);
        var permRepo = new Mock<IPermissionRepository>();

        var sut = new PermissionService(docRepo.Object, permRepo.Object);

        Assert.False(await sut.AuthorizeAsync(Guid.NewGuid(), Guid.NewGuid(), DocumentRole.Viewer));
    }
}
