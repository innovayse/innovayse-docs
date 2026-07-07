using Innovayse.Docs.Application.Versions;
using Innovayse.Docs.Domain.Versions;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class VersionRepository : IVersionRepository
{
    private readonly DocsDbContext _context;
    public VersionRepository(DocsDbContext context) => _context = context;

    public async Task AppendUpdateAsync(DocumentUpdate update)
    {
        _context.DocumentUpdates.Add(update);
        await _context.SaveChangesAsync();
    }

    public Task<List<DocumentUpdate>> ListUpdatesAsync(Guid documentId) =>
        _context.DocumentUpdates
            .Where(u => u.DocumentId == documentId)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

    public Task<List<DocumentVersion>> ListForDocumentAsync(Guid documentId) =>
        _context.DocumentVersions.Where(v => v.DocumentId == documentId).ToListAsync();

    public async Task RestoreAsync(Guid documentId, Guid versionId)
    {
        var version = await _context.DocumentVersions
            .FirstOrDefaultAsync(v => v.Id == versionId && v.DocumentId == documentId);
        if (version is null) return;

        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId);
        if (document is null) return;

        document.ContentSnapshot = version.Snapshot;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
    }
}
