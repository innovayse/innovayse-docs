using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public interface IPermissionService
{
    Task<bool> AuthorizeAsync(Guid documentId, Guid userId, DocumentRole required);

    /// <summary>Resolves the caller's effective role (owner, direct grant, or inherited
    /// from the containing folder), or null if they have no access at all.</summary>
    Task<DocumentRole?> GetEffectiveRoleAsync(Guid documentId, Guid userId);
}
