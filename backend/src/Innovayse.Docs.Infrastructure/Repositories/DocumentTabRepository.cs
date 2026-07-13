using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class DocumentTabRepository : IDocumentTabRepository
{
    private readonly DocsDbContext _context;
    public DocumentTabRepository(DocsDbContext context) => _context = context;

    public async Task CreateAsync(DocumentTab tab)
    {
        _context.DocumentTabs.Add(tab);
        await _context.SaveChangesAsync();
    }

    public Task<DocumentTab?> GetByIdAsync(Guid tabId) =>
        _context.DocumentTabs.FirstOrDefaultAsync(t => t.Id == tabId);

    public async Task UpdateAsync(DocumentTab tab)
    {
        _context.DocumentTabs.Update(tab);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid tabId)
    {
        var tab = await GetByIdAsync(tabId);
        if (tab is null) return;
        _context.DocumentTabs.Remove(tab);
        await _context.SaveChangesAsync();
    }

    public Task<List<DocumentTab>> ListForDocumentAsync(Guid documentId) =>
        _context.DocumentTabs
            .Where(t => t.DocumentId == documentId)
            .OrderBy(t => t.OrderIndex)
            .ToListAsync();
}
