using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Infrastructure.Persistence;
using Innovayse.Docs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Repositories;

public class FolderRepositoryTests
{
    private static DocsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DocsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocsDbContext(options);
    }

    [Fact]
    public async Task ListForUserAsync_OwnedFolder_IsIncluded()
    {
        await using var context = CreateContext();
        var repo = new FolderRepository(context);
        var ownerId = Guid.NewGuid();
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Mine", OwnerId = ownerId };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        var result = await repo.ListForUserAsync(ownerId);

        Assert.Contains(result, f => f.Id == folder.Id);
    }

    [Fact]
    public async Task ListForUserAsync_DirectlySharedFolder_AppearsAsRoot()
    {
        await using var context = CreateContext();
        var repo = new FolderRepository(context);
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Shared", OwnerId = ownerId };
        context.Folders.Add(folder);
        context.FolderPermissions.Add(new FolderPermission
        {
            Id = Guid.NewGuid(), FolderId = folder.Id, UserId = sharedWithId,
            Role = DocumentRole.Editor, GrantedBy = ownerId, CreatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var result = await repo.ListForUserAsync(sharedWithId);

        Assert.Contains(result, f => f.Id == folder.Id);
    }

    [Fact]
    public async Task ListForUserAsync_NestedSubfolderOfSharedFolder_IsIncludedButAncestorAboveShareIsNot()
    {
        await using var context = CreateContext();
        var repo = new FolderRepository(context);
        var ownerId = Guid.NewGuid();
        var sharedWithId = Guid.NewGuid();
        var grandparent = new Folder { Id = Guid.NewGuid(), Name = "Grandparent (not shared)", OwnerId = ownerId };
        var sharedFolder = new Folder { Id = Guid.NewGuid(), Name = "Shared", OwnerId = ownerId, ParentFolderId = grandparent.Id };
        var child = new Folder { Id = Guid.NewGuid(), Name = "Child", OwnerId = ownerId, ParentFolderId = sharedFolder.Id };
        context.Folders.AddRange(grandparent, sharedFolder, child);
        context.FolderPermissions.Add(new FolderPermission
        {
            Id = Guid.NewGuid(), FolderId = sharedFolder.Id, UserId = sharedWithId,
            Role = DocumentRole.Viewer, GrantedBy = ownerId, CreatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        var result = await repo.ListForUserAsync(sharedWithId);

        Assert.Contains(result, f => f.Id == sharedFolder.Id);
        Assert.Contains(result, f => f.Id == child.Id);
        Assert.DoesNotContain(result, f => f.Id == grandparent.Id);
    }

    [Fact]
    public async Task ListForUserAsync_UnrelatedFolder_IsExcluded()
    {
        await using var context = CreateContext();
        var repo = new FolderRepository(context);
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var folder = new Folder { Id = Guid.NewGuid(), Name = "Private", OwnerId = ownerId };
        context.Folders.Add(folder);
        await context.SaveChangesAsync();

        var result = await repo.ListForUserAsync(strangerId);

        Assert.DoesNotContain(result, f => f.Id == folder.Id);
    }
}
