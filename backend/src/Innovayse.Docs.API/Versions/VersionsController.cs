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

    public class CreateVersionRequest
    {
        public string SnapshotBase64 { get; set; } = string.Empty;
        public string? Label { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<DocumentVersion>> Create(Guid documentId, CreateVersionRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Editor))
            return Forbid();

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Snapshot = Convert.FromBase64String(request.SnapshotBase64),
            CreatedBy = CallerId,
            CreatedAt = DateTimeOffset.UtcNow,
            Label = request.Label,
        };
        await _versionRepository.CreateVersionAsync(version);
        return Created($"/documents/{documentId}/versions/{version.Id}", version);
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
