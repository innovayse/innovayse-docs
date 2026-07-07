using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public interface IPermissionRepository
{
    Task<DocumentRole?> GetRoleAsync(Guid documentId, Guid userId);
    Task GrantAsync(DocumentPermission permission);

    /// <summary>Grants a permission, or updates the role in place if the user already has a
    /// document-level grant — avoids piling up duplicate rows when the same person is invited
    /// more than once.</summary>
    Task UpsertAsync(DocumentPermission permission);

    Task<List<DocumentPermission>> ListForDocumentAsync(Guid documentId);
}
