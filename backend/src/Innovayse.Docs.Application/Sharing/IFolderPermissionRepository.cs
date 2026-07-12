using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public interface IFolderPermissionRepository
{
    Task<DocumentRole?> GetRoleAsync(Guid folderId, Guid userId);
    Task GrantAsync(FolderPermission permission);

    /// <summary>Grants a permission, or updates the role in place if the user already has a
    /// direct grant on this folder — avoids piling up duplicate rows when the same person is
    /// invited more than once.</summary>
    Task UpsertAsync(FolderPermission permission);

    /// <summary>Every direct grant on this folder — used to resolve the full set of
    /// participants for a document filed in (or under) this folder.</summary>
    Task<List<FolderPermission>> ListForFolderAsync(Guid folderId);
}
