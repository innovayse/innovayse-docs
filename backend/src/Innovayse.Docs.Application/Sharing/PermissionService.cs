using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Folders;
using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public class PermissionService : IPermissionService
{
    private const int MaxFolderChainDepth = 50;

    private readonly IDocumentRepository _documentRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IFolderPermissionRepository _folderPermissionRepository;
    private readonly IFolderRepository _folderRepository;

    public PermissionService(
        IDocumentRepository documentRepository,
        IPermissionRepository permissionRepository,
        IFolderPermissionRepository folderPermissionRepository,
        IFolderRepository folderRepository)
    {
        _documentRepository = documentRepository;
        _permissionRepository = permissionRepository;
        _folderPermissionRepository = folderPermissionRepository;
        _folderRepository = folderRepository;
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

        DocumentRole? highest = await _permissionRepository.GetRoleAsync(documentId, userId);
        var folderRole = await GetHighestFolderChainRoleAsync(document.FolderId, userId);
        if (folderRole.HasValue && (!highest.HasValue || folderRole.Value > highest.Value))
            highest = folderRole.Value;

        return highest;
    }

    public async Task<bool> AuthorizeFolderAsync(Guid folderId, Guid userId, DocumentRole required)
    {
        var role = await GetEffectiveFolderRoleAsync(folderId, userId);
        return role.HasValue && role.Value.Satisfies(required);
    }

    public async Task<DocumentRole?> GetEffectiveFolderRoleAsync(Guid folderId, Guid userId)
    {
        var folder = await _folderRepository.GetByIdAsync(folderId);
        if (folder is null) return null;

        if (folder.OwnerId == userId) return DocumentRole.Owner;

        return await GetHighestFolderChainRoleAsync(folderId, userId);
    }

    /// <summary>Walks from <paramref name="startFolderId"/> up through ParentFolderId,
    /// collecting the highest FolderPermission role found for userId at any level. A null
    /// startFolderId (document not filed in any folder) returns null immediately.</summary>
    private async Task<DocumentRole?> GetHighestFolderChainRoleAsync(Guid? startFolderId, Guid userId)
    {
        DocumentRole? highest = null;
        var currentFolderId = startFolderId;
        var depth = 0;

        while (currentFolderId.HasValue && depth < MaxFolderChainDepth)
        {
            var role = await _folderPermissionRepository.GetRoleAsync(currentFolderId.Value, userId);
            if (role.HasValue && (!highest.HasValue || role.Value > highest.Value))
                highest = role.Value;

            var folder = await _folderRepository.GetByIdAsync(currentFolderId.Value);
            currentFolderId = folder?.ParentFolderId;
            depth++;
        }

        return highest;
    }

    public async Task<List<Guid>> GetDocumentParticipantUserIdsAsync(Guid documentId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document is null) return new List<Guid>();

        var participantIds = new HashSet<Guid> { document.OwnerId };

        var directGrants = await _permissionRepository.ListForDocumentAsync(documentId);
        foreach (var grant in directGrants) participantIds.Add(grant.UserId);

        foreach (var userId in await GetFolderChainParticipantIdsAsync(document.FolderId))
            participantIds.Add(userId);

        return participantIds.ToList();
    }

    /// <summary>Walks from <paramref name="startFolderId"/> up through ParentFolderId,
    /// collecting every distinct UserId with a FolderPermission at any level. Mirrors
    /// GetHighestFolderChainRoleAsync's walk but collects participants instead of a role.</summary>
    private async Task<HashSet<Guid>> GetFolderChainParticipantIdsAsync(Guid? startFolderId)
    {
        var participantIds = new HashSet<Guid>();
        var currentFolderId = startFolderId;
        var depth = 0;

        while (currentFolderId.HasValue && depth < MaxFolderChainDepth)
        {
            var grants = await _folderPermissionRepository.ListForFolderAsync(currentFolderId.Value);
            foreach (var grant in grants) participantIds.Add(grant.UserId);

            var folder = await _folderRepository.GetByIdAsync(currentFolderId.Value);
            currentFolderId = folder?.ParentFolderId;
            depth++;
        }

        return participantIds;
    }
}
