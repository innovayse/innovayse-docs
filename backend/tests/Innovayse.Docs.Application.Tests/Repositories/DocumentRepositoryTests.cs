using Innovayse.Docs.Domain.Documents;
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
}
