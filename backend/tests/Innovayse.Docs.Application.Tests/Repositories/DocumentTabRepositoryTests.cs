using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Infrastructure.Persistence;
using Innovayse.Docs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Repositories;

public class DocumentTabRepositoryTests
{
    private static DocsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DocsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DocsDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ThenGetByIdAsync_ReturnsSameTab()
    {
        await using var context = CreateContext();
        var repo = new DocumentTabRepository(context);
        var tab = new DocumentTab { Id = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Title = "Tab 1", OrderIndex = 0 };

        await repo.CreateAsync(tab);
        var fetched = await repo.GetByIdAsync(tab.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Tab 1", fetched!.Title);
    }

    [Fact]
    public async Task ListForDocumentAsync_ReturnsTabsOrderedByOrderIndex()
    {
        await using var context = CreateContext();
        var repo = new DocumentTabRepository(context);
        var documentId = Guid.NewGuid();
        await repo.CreateAsync(new DocumentTab { Id = Guid.NewGuid(), DocumentId = documentId, Title = "Second", OrderIndex = 1 });
        await repo.CreateAsync(new DocumentTab { Id = Guid.NewGuid(), DocumentId = documentId, Title = "First", OrderIndex = 0 });
        await repo.CreateAsync(new DocumentTab { Id = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Title = "OtherDoc", OrderIndex = 0 });

        var result = await repo.ListForDocumentAsync(documentId);

        Assert.Equal(2, result.Count);
        Assert.Equal("First", result[0].Title);
        Assert.Equal("Second", result[1].Title);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTab()
    {
        await using var context = CreateContext();
        var repo = new DocumentTabRepository(context);
        var tab = new DocumentTab { Id = Guid.NewGuid(), DocumentId = Guid.NewGuid(), Title = "Tab 1", OrderIndex = 0 };
        await repo.CreateAsync(tab);

        await repo.DeleteAsync(tab.Id);
        var fetched = await repo.GetByIdAsync(tab.Id);

        Assert.Null(fetched);
    }
}
