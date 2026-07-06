using System.Security.Claims;
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
    private Guid? _callerIdOverride;

    public SharingController(
        IPermissionRepository permissionRepository,
        IShareLinkRepository shareLinkRepository,
        IPermissionService permissionService)
    {
        _permissionRepository = permissionRepository;
        _shareLinkRepository = shareLinkRepository;
        _permissionService = permissionService;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    public class InviteUserRequest
    {
        public Guid UserId { get; set; }
        public DocumentRole Role { get; set; }
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser(Guid documentId, InviteUserRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Owner))
            return Forbid();

        await _permissionRepository.GrantAsync(new DocumentPermission
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = request.UserId,
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
}
