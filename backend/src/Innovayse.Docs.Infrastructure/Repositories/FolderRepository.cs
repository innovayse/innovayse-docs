using Innovayse.Docs.Application.Folders;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly DocsDbContext _context;
    public FolderRepository(DocsDbContext context) => _context = context;

    public async Task CreateAsync(Folder folder)
    {
        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();
    }

    public Task<Folder?> GetByIdAsync(Guid id) =>
        _context.Folders.FirstOrDefaultAsync(f => f.Id == id);

    public Task<List<Folder>> ListForUserAsync(Guid ownerId) =>
        _context.Folders.Where(f => f.OwnerId == ownerId).OrderBy(f => f.Name).ToListAsync();

    public async Task DeleteAsync(Guid id)
    {
        var folder = await GetByIdAsync(id);
        if (folder is null) return;

        var documentsInFolder = await _context.Documents.Where(d => d.FolderId == id).ToListAsync();
        foreach (var document in documentsInFolder) document.FolderId = null;

        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync();
    }
}
