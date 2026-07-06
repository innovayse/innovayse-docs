using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public interface IFolderPermissionRepository
{
    Task<DocumentRole?> GetRoleAsync(Guid folderId, Guid userId);
    Task GrantAsync(FolderPermission permission);
}
