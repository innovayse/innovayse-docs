using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public class PermissionService : IPermissionService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IFolderPermissionRepository _folderPermissionRepository;

    public PermissionService(
        IDocumentRepository documentRepository,
        IPermissionRepository permissionRepository,
        IFolderPermissionRepository folderPermissionRepository)
    {
        _documentRepository = documentRepository;
        _permissionRepository = permissionRepository;
        _folderPermissionRepository = folderPermissionRepository;
    }

    public async Task<bool> AuthorizeAsync(Guid documentId, Guid userId, DocumentRole required)
    {
        var role = await GetEffectiveRoleAsync(documentId, userId);
        return role.HasValue && role.Value.Satisfies(required);
    }

    public async Task<DocumentRole?> GetEffectiveRoleAsync(Guid documentId, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document is null) return null;

        if (document.OwnerId == userId) return DocumentRole.Owner;

        var documentRole = await _permissionRepository.GetRoleAsync(documentId, userId);
        if (documentRole.HasValue) return documentRole.Value;

        if (document.FolderId.HasValue)
        {
            var folderRole = await _folderPermissionRepository.GetRoleAsync(document.FolderId.Value, userId);
            if (folderRole.HasValue) return folderRole.Value;
        }

        return null;
    }
}
