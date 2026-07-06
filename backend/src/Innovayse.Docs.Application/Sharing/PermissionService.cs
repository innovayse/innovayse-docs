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
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document is null) return false;

        if (document.OwnerId == userId) return true;

        var documentRole = await _permissionRepository.GetRoleAsync(documentId, userId);
        if (documentRole.HasValue) return documentRole.Value.Satisfies(required);

        if (document.FolderId.HasValue)
        {
            var folderRole = await _folderPermissionRepository.GetRoleAsync(document.FolderId.Value, userId);
            if (folderRole.HasValue) return folderRole.Value.Satisfies(required);
        }

        return false;
    }
}
