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
}
