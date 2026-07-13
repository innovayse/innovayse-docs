using System.Data;
using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    /// <summary>Atomically deletes a tab unless it is the document's last remaining tab.
    /// Runs the read-count-then-delete inside a Serializable transaction so two concurrent
    /// deletes on a 2-tab document can't both pass the count check and leave the document
    /// with zero tabs — a losing concurrent transaction resolves to "not deleted" (returns
    /// false) rather than crashing, which the caller then reports as 409 Conflict.</summary>
    public async Task<bool> DeleteIfNotLastAsync(Guid tabId)
    {
        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        }
        catch (InvalidOperationException)
        {
            // Providers that don't support transactions (e.g. the EF InMemory provider used
            // in unit tests) throw here — fall back to a non-transactional guarded delete.
        }

        try
        {
            var tab = await _context.DocumentTabs.FirstOrDefaultAsync(t => t.Id == tabId);
            if (tab is null) return false;

            var count = await _context.DocumentTabs.CountAsync(t => t.DocumentId == tab.DocumentId);
            if (count <= 1) return false;

            _context.DocumentTabs.Remove(tab);
            await _context.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            // A concurrent transaction won the race (or a serialization failure occurred) —
            // treat this as "not deleted" so the caller reports 409, not a 500.
            if (transaction is not null) await transaction.RollbackAsync();
            return false;
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }
}
