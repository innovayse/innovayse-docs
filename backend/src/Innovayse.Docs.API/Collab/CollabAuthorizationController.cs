using System.Security.Claims;
using Innovayse.Docs.Application.Sharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Collab;

[ApiController]
[Route("internal/collab")]
[Authorize]
public class CollabAuthorizationController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private Guid? _callerIdOverride;

    public CollabAuthorizationController(IPermissionService permissionService) =>
        _permissionService = permissionService;

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    public record AuthorizeResponse(Innovayse.Docs.Domain.Sharing.DocumentRole Role);

    [HttpGet("authorize")]
    public async Task<ActionResult<AuthorizeResponse>> Authorize(Guid documentId)
    {
        var role = await _permissionService.GetEffectiveRoleAsync(documentId, CallerId);
        if (role is null) return Forbid();
        return Ok(new AuthorizeResponse(role.Value));
    }
}
