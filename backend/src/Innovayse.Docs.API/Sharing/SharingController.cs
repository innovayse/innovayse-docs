using System.Security.Claims;
using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Identity;
using Innovayse.Docs.Application.Notifications;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Notifications;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Sharing;

[ApiController]
[Route("documents/{documentId}/share")]
[Authorize]
public class SharingController : ControllerBase
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IShareLinkRepository _shareLinkRepository;
    private readonly IPermissionService _permissionService;
    private readonly ISsoUserLookupService _ssoUserLookupService;
    private readonly IDocumentRepository _documentRepository;
    private readonly INotificationRepository _notificationRepository;
    private Guid? _callerIdOverride;

    public SharingController(
        IPermissionRepository permissionRepository,
        IShareLinkRepository shareLinkRepository,
        IPermissionService permissionService,
        ISsoUserLookupService ssoUserLookupService,
        IDocumentRepository documentRepository,
        INotificationRepository notificationRepository)
    {
        _permissionRepository = permissionRepository;
        _shareLinkRepository = shareLinkRepository;
        _permissionService = permissionService;
        _ssoUserLookupService = ssoUserLookupService;
        _documentRepository = documentRepository;
        _notificationRepository = notificationRepository;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    /// <summary>Roles grantable through invite/share-link — <see cref="DocumentRole.Owner"/> is
    /// excluded because ownership is fixed at document creation, not something one owner can hand
    /// out to another user via a share action.</summary>
    private static bool IsInvitableRole(DocumentRole role) =>
        role is DocumentRole.Viewer or DocumentRole.Commenter or DocumentRole.Editor;

    public class InviteUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public DocumentRole Role { get; set; }
        public string InviterName { get; set; } = string.Empty;
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser(Guid documentId, InviteUserRequest request, CancellationToken ct)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Owner))
            return Forbid();

        if (!IsInvitableRole(request.Role))
            return BadRequest(new { message = "Role must be Viewer, Commenter, or Editor" });

        var user = await _ssoUserLookupService.FindByEmailAsync(request.Email, ct);
        if (user is null)
            return NotFound(new { message = "No account found for that email" });

        // Upsert, not a raw grant — re-inviting the same person (e.g. to change their role)
        // updates their existing permission in place instead of piling up duplicate rows.
        await _permissionRepository.UpsertAsync(new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = user.Id,
            Role = request.Role,
            GrantedBy = CallerId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var document = await _documentRepository.GetByIdAsync(documentId);
        await _notificationRepository.CreateAsync(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = user.Id,
            Type = NotificationType.DocumentShared,
            ActorUserId = CallerId,
            ActorName = request.InviterName,
            DocumentId = documentId,
            PreviewText = document?.Title ?? "Untitled document",
            CreatedAt = DateTimeOffset.UtcNow
        });

        return NoContent();
    }

    public class CreateLinkRequest
    {
        public DocumentRole Role { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }

    [HttpPost("link")]
    public async Task<ActionResult<ShareLink>> CreateLink(Guid documentId, CreateLinkRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Owner))
            return Forbid();

        if (!IsInvitableRole(request.Role))
            return BadRequest(new { message = "Role must be Viewer, Commenter, or Editor" });

        var link = new ShareLink
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Token = Guid.NewGuid().ToString("N"),
            Role = request.Role,
            ExpiresAt = request.ExpiresAt
        };
        await _shareLinkRepository.CreateAsync(link);
        return Created($"/documents/{documentId}/share/link/{link.Id}", link);
    }

    public class RedeemLinkRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    [HttpPost("redeem")]
    public async Task<IActionResult> RedeemLink(Guid documentId, RedeemLinkRequest request)
    {
        var link = await _shareLinkRepository.GetByTokenAsync(request.Token);
        if (link is null || link.DocumentId != documentId)
            return NotFound();
        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTimeOffset.UtcNow)
            return StatusCode(StatusCodes.Status410Gone);

        var existingRole = await _permissionService.GetEffectiveRoleAsync(documentId, CallerId);
        if (existingRole.HasValue)
            return NoContent();

        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document is null)
            return NotFound();

        await _permissionRepository.GrantAsync(new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = CallerId,
            Role = link.Role,
            GrantedBy = document.OwnerId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        return NoContent();
    }
}
