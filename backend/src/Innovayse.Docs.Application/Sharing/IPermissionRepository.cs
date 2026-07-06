using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public interface IPermissionRepository
{
    Task<DocumentRole?> GetRoleAsync(Guid documentId, Guid userId);
    Task GrantAsync(DocumentPermission permission);
    Task<List<DocumentPermission>> ListForDocumentAsync(Guid documentId);
}
