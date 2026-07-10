using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Infrastructure.Persistence;
using Innovayse.Docs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Repositories;

public class DocumentRepositoryTests
{
    private static DocsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DocsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocsDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ThenGetByIdAsync_ReturnsSameDocument()
    {
        await using var context = CreateContext();
        var repo = new DocumentRepository(context);
        var owner = Guid.NewGuid();
        var doc = new Document { Id = Guid.NewGuid(), Title = "Test Doc", OwnerId = owner };

        await repo.CreateAsync(doc);
        var fetched = await repo.GetByIdAsync(doc.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Test Doc", fetched!.Title);
    }

    [Fact]
    public async Task ListForUserAsync_DocumentInDirectlySharedFolder_IsIncluded()
    {
        await using var context = CreateContext();
        var repo = new DocumentRepository(context);
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        context.Folders.Add(new Folder { Id = folderId, Name = "Shared", OwnerId = ownerId });
        context.FolderPermissions.Add(new FolderPermission
        {
            Id = Guid.NewGuid(), FolderId = folderId, UserId = sharedWithId,
            Role = DocumentRole.Viewer, GrantedBy = ownerId, CreatedAt = DateTimeOffset.UtcNow,
        });
        var document = new Document { Id = Guid.NewGuid(), Title = "In shared folder", OwnerId = ownerId, FolderId = folderId, UpdatedAt = DateTimeOffset.UtcNow };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var result = await repo.ListForUserAsync(sharedWithId);

        Assert.Contains(result, d => d.Id == document.Id);
    }

    [Fact]
    public async Task ListForUserAsync_DocumentInNestedSubfolderOfSharedFolder_IsIncluded()
    {
        await using var context = CreateContext();
        var repo = new DocumentRepository(context);
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var childFolderId = Guid.NewGuid();
        context.Folders.Add(new Folder { Id = parentFolderId, Name = "Parent", OwnerId = ownerId });
        context.Folders.Add(new Folder { Id = childFolderId, Name = "Child", OwnerId = ownerId, ParentFolderId = parentFolderId });
        context.FolderPermissions.Add(new FolderPermission
        {
            Id = Guid.NewGuid(), FolderId = parentFolderId, UserId = sharedWithId,
            Role = DocumentRole.Viewer, GrantedBy = ownerId, CreatedAt = DateTimeOffset.UtcNow,
        });
        var document = new Document { Id = Guid.NewGuid(), Title = "Nested", OwnerId = ownerId, FolderId = childFolderId, UpdatedAt = DateTimeOffset.UtcNow };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var result = await repo.ListForUserAsync(sharedWithId);

        Assert.Contains(result, d => d.Id == document.Id);
    }

    [Fact]
    public async Task ListForUserAsync_UnrelatedFolder_IsExcluded()
    {
        await using var context = CreateContext();
        var repo = new DocumentRepository(context);
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        context.Folders.Add(new Folder { Id = folderId, Name = "Not shared", OwnerId = ownerId });
        var document = new Document { Id = Guid.NewGuid(), Title = "Private", OwnerId = ownerId, FolderId = folderId, UpdatedAt = DateTimeOffset.UtcNow };
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var result = await repo.ListForUserAsync(strangerId);

        Assert.DoesNotContain(result, d => d.Id == document.Id);
    }
}
