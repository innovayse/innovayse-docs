using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DocsDbContext _context;
    public DocumentRepository(DocsDbContext context) => _context = context;

    public async Task CreateAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
    }

    public Task<Document?> GetByIdAsync(Guid id) =>
        _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

    public async Task UpdateAsync(Document document)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var doc = await GetByIdAsync(id);
        if (doc is null) return;
        _context.Documents.Remove(doc);
        await _context.SaveChangesAsync();
    }

    public Task<List<Document>> ListByFolderAsync(Guid? folderId, Guid ownerId) =>
        _context.Documents
            .Where(d => d.FolderId == folderId && d.OwnerId == ownerId)
            .ToListAsync();

    public async Task<List<Document>> ListForUserAsync(Guid userId)
    {
        var accessibleFolderIds = await FolderAccessHelper.GetAccessibleFolderIdsAsync(_context, userId);
        return await _context.Documents
            .Where(d => d.OwnerId == userId ||
                _context.DocumentPermissions.Any(p => p.DocumentId == d.Id && p.UserId == userId) ||
                (d.FolderId != null && accessibleFolderIds.Contains(d.FolderId.Value)))
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();
    }
}
