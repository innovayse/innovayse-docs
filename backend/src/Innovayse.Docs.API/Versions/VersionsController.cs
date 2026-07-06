using System.Security.Claims;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Application.Versions;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Domain.Versions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Versions;

[ApiController]
[Route("documents/{documentId}/versions")]
[Authorize]
public class VersionsController : ControllerBase
{
    private readonly IVersionRepository _versionRepository;
    private readonly IPermissionService _permissionService;
    private Guid? _callerIdOverride;

    public VersionsController(IVersionRepository versionRepository, IPermissionService permissionService)
    {
        _versionRepository = versionRepository;
        _permissionService = permissionService;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    [HttpGet]
    public async Task<ActionResult<List<DocumentVersion>>> List(Guid documentId)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Viewer))
            return Forbid();

        return Ok(await _versionRepository.ListForDocumentAsync(documentId));
    }

    [HttpPost("{versionId}/restore")]
    public async Task<IActionResult> Restore(Guid documentId, Guid versionId)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Editor))
            return Forbid();

        await _versionRepository.RestoreAsync(documentId, versionId);
        return NoContent();
    }
}
