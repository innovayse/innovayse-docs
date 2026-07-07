using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Infrastructure.Persistence;
using Innovayse.Docs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Repositories;

public class PermissionRepositoryTests
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
        var repo = new PermissionRepository(context);
        var documentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await repo.UpsertAsync(new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = userId,
            Role = DocumentRole.Viewer,
            GrantedBy = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        Assert.Equal(DocumentRole.Viewer, await repo.GetRoleAsync(documentId, userId));
    }

    [Fact]
    public async Task UpsertAsync_ExistingGrant_UpdatesRoleInPlaceRatherThanDuplicating()
    {
        await using var context = CreateContext();
        var repo = new PermissionRepository(context);
        var documentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var firstGranter = Guid.NewGuid();
        var secondGranter = Guid.NewGuid();

        await repo.UpsertAsync(new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = userId,
            Role = DocumentRole.Viewer,
            GrantedBy = firstGranter,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await repo.UpsertAsync(new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = userId,
            Role = DocumentRole.Editor,
            GrantedBy = secondGranter,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        Assert.Equal(DocumentRole.Editor, await repo.GetRoleAsync(documentId, userId));
        var all = await repo.ListForDocumentAsync(documentId);
        var forUser = Assert.Single(all, p => p.UserId == userId);
        Assert.Equal(secondGranter, forUser.GrantedBy);
    }
}
