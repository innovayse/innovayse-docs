using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public interface IPermissionService
{
    Task<bool> AuthorizeAsync(Guid documentId, Guid userId, DocumentRole required);

    /// <summary>Resolves the caller's effective role on a document: the max of ownership,
    /// any direct grant, and any grant found walking the document's containing folder's
    /// ancestor chain — or null if they have no access at all.</summary>
    Task<DocumentRole?> GetEffectiveRoleAsync(Guid documentId, Guid userId);

    Task<bool> AuthorizeFolderAsync(Guid folderId, Guid userId, DocumentRole required);

    /// <summary>Resolves the caller's effective role on a folder itself: ownership, or the
    /// max grant found at this folder or any ancestor in its <c>ParentFolderId</c> chain —
    /// or null if they have no access at all.</summary>
    Task<DocumentRole?> GetEffectiveFolderRoleAsync(Guid folderId, Guid userId);
}
