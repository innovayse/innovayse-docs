using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly DocsDbContext _context;
    public PermissionRepository(DocsDbContext context) => _context = context;

    public async Task<DocumentRole?> GetRoleAsync(Guid documentId, Guid userId)
    {
        var permission = await _context.DocumentPermissions
            .FirstOrDefaultAsync(p => p.DocumentId == documentId && p.UserId == userId);
        return permission?.Role;
    }

    public async Task GrantAsync(DocumentPermission permission)
    {
        _context.DocumentPermissions.Add(permission);
        await _context.SaveChangesAsync();
    }

    public Task<List<DocumentPermission>> ListForDocumentAsync(Guid documentId) =>
        _context.DocumentPermissions.Where(p => p.DocumentId == documentId).ToListAsync();
}
