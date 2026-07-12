using System.Security.Claims;
using Innovayse.Docs.Application.Folders;
using Innovayse.Docs.Application.Identity;
using Innovayse.Docs.Application.Notifications;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Notifications;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Folders;

[ApiController]
[Route("folders/{folderId}/share")]
[Authorize]
public class FolderSharingController : ControllerBase
{
    private readonly IFolderPermissionRepository _folderPermissionRepository;
    private readonly IPermissionService _permissionService;
    private readonly ISsoUserLookupService _ssoUserLookupService;
    private readonly IFolderRepository _folderRepository;
    private readonly INotificationRepository _notificationRepository;
    private Guid? _callerIdOverride;

    public FolderSharingController(
        IFolderPermissionRepository folderPermissionRepository,
        IPermissionService permissionService,
        ISsoUserLookupService ssoUserLookupService,
        IFolderRepository folderRepository,
        INotificationRepository notificationRepository)
    {
        _folderPermissionRepository = folderPermissionRepository;
        _permissionService = permissionService;
        _ssoUserLookupService = ssoUserLookupService;
        _folderRepository = folderRepository;
        _notificationRepository = notificationRepository;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    /// <summary>Roles grantable through invite — Owner is excluded because folder ownership
    /// is fixed at creation, matching the equivalent document-sharing restriction.</summary>
    private static bool IsInvitableRole(DocumentRole role) =>
        role is DocumentRole.Viewer or DocumentRole.Commenter or DocumentRole.Editor;

    public class InviteUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public DocumentRole Role { get; set; }
        public string InviterName { get; set; } = string.Empty;
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser(Guid folderId, InviteUserRequest request, CancellationToken ct)
    {
        if (!await _permissionService.AuthorizeFolderAsync(folderId, CallerId, DocumentRole.Owner))
            return Forbid();

        if (!IsInvitableRole(request.Role))
            return BadRequest(new { message = "Role must be Viewer, Commenter, or Editor" });

        var user = await _ssoUserLookupService.FindByEmailAsync(request.Email, ct);
        if (user is null)
            return NotFound(new { message = "No account found for that email" });

        await _folderPermissionRepository.UpsertAsync(new FolderPermission
        {
            Id = Guid.NewGuid(),
            FolderId = folderId,
            UserId = user.Id,
            Role = request.Role,
            GrantedBy = CallerId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var folder = await _folderRepository.GetByIdAsync(folderId);
        await _notificationRepository.CreateAsync(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            Type = NotificationType.FolderShared,
            ActorUserId = CallerId,
            ActorName = request.InviterName,
            FolderId = folderId,
            PreviewText = folder?.Name ?? "Untitled folder",
            CreatedAt = DateTimeOffset.UtcNow
        });

        return NoContent();
    }
}
