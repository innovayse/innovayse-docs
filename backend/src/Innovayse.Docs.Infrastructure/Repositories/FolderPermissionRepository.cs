using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class FolderPermissionRepository : IFolderPermissionRepository
{
    private readonly DocsDbContext _context;
    public FolderPermissionRepository(DocsDbContext context) => _context = context;

    public async Task<DocumentRole?> GetRoleAsync(Guid folderId, Guid userId)
    {
        var permission = await _context.FolderPermissions
            .FirstOrDefaultAsync(p => p.FolderId == folderId && p.UserId == userId);
        return permission?.Role;
    }

    public async Task GrantAsync(FolderPermission permission)
    {
        _context.FolderPermissions.Add(permission);
        await _context.SaveChangesAsync();
    }

    public async Task UpsertAsync(FolderPermission permission)
    {
        var existing = await _context.FolderPermissions
            .FirstOrDefaultAsync(p => p.FolderId == permission.FolderId && p.UserId == permission.UserId);
        if (existing is null)
        {
            _context.FolderPermissions.Add(permission);
        }
        else
        {
            existing.Role = permission.Role;
            existing.GrantedBy = permission.GrantedBy;
        }
        await _context.SaveChangesAsync();
    }

    public Task<List<FolderPermission>> ListForFolderAsync(Guid folderId) =>
        _context.FolderPermissions.Where(p => p.FolderId == folderId).ToListAsync();
}
