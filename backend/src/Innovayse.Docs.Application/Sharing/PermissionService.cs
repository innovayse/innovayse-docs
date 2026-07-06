using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public class PermissionService : IPermissionService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IPermissionRepository _permissionRepository;

    public PermissionService(IDocumentRepository documentRepository, IPermissionRepository permissionRepository)
    {
        _documentRepository = documentRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<bool> AuthorizeAsync(Guid documentId, Guid userId, DocumentRole required)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document is null) return false;

        if (document.OwnerId == userId) return true;

        var role = await _permissionRepository.GetRoleAsync(documentId, userId);
        return role.HasValue && role.Value.Satisfies(required);
    }
}
