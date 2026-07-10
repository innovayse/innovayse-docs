using System.Security.Claims;
using Innovayse.Docs.Application.Identity;
using Innovayse.Docs.Application.Sharing;
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
    private Guid? _callerIdOverride;

    public FolderSharingController(
        IFolderPermissionRepository folderPermissionRepository,
        IPermissionService permissionService,
        ISsoUserLookupService ssoUserLookupService)
    {
        _folderPermissionRepository = folderPermissionRepository;
        _permissionService = permissionService;
        _ssoUserLookupService = ssoUserLookupService;
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
        return NoContent();
    }
}
