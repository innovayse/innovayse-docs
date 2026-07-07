using System.Security.Claims;
using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Identity;
using Innovayse.Docs.Application.Sharing;
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
    private Guid? _callerIdOverride;

    public SharingController(
        IPermissionRepository permissionRepository,
        IShareLinkRepository shareLinkRepository,
        IPermissionService permissionService,
        ISsoUserLookupService ssoUserLookupService,
        IDocumentRepository documentRepository)
    {
        _permissionRepository = permissionRepository;
        _shareLinkRepository = shareLinkRepository;
        _permissionService = permissionService;
        _ssoUserLookupService = ssoUserLookupService;
        _documentRepository = documentRepository;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    public class InviteUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public DocumentRole Role { get; set; }
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser(Guid documentId, InviteUserRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Owner))
            return Forbid();

        var user = await _ssoUserLookupService.FindByEmailAsync(request.Email);
        if (user is null)
            return NotFound(new { message = "No account found for that email" });

        await _permissionRepository.GrantAsync(new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = user.Id,
            Role = request.Role,
            GrantedBy = CallerId,
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
