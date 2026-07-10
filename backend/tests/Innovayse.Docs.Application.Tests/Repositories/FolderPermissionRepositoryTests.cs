using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Infrastructure.Persistence;
using Innovayse.Docs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Repositories;

public class FolderPermissionRepositoryTests
{
    private static DocsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DocsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocsDbContext(options);
    }

    [Fact]
    public async Task UpsertAsync_NoExistingGrant_CreatesOne()
    {
        await using var context = CreateContext();
        var repo = new FolderPermissionRepository(context);
        var folderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await repo.UpsertAsync(new FolderPermission
        {
            Id = Guid.NewGuid(),
            FolderId = folderId,
            UserId = userId,
            Role = DocumentRole.Viewer,
            GrantedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        Assert.Equal(DocumentRole.Viewer, await repo.GetRoleAsync(folderId, userId));
    }

    [Fact]
    public async Task UpsertAsync_ExistingGrant_UpdatesRoleInPlaceRatherThanDuplicating()
    {
        await using var context = CreateContext();
        var repo = new FolderPermissionRepository(context);
        var folderId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await repo.UpsertAsync(new FolderPermission
        {
            Id = Guid.NewGuid(),
            FolderId = folderId,
            UserId = userId,
            Role = DocumentRole.Viewer,
            GrantedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await repo.UpsertAsync(new FolderPermission
        {
            Id = Guid.NewGuid(),
            FolderId = folderId,
            UserId = userId,
            Role = DocumentRole.Editor,
            GrantedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        Assert.Equal(DocumentRole.Editor, await repo.GetRoleAsync(folderId, userId));
        var count = context.FolderPermissions.Count(p => p.FolderId == folderId && p.UserId == userId);
        Assert.Equal(1, count);
    }
}
