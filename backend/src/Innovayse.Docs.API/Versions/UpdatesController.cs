using System.Linq;
using System.Security.Claims;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Application.Versions;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Domain.Versions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Versions;

[ApiController]
[Route("documents/{documentId}/tabs/{tabId}/updates")]
[Authorize]
public class UpdatesController : ControllerBase
{
    private readonly IVersionRepository _versionRepository;
    private readonly IPermissionService _permissionService;
    private Guid? _callerIdOverride;

    public UpdatesController(IVersionRepository versionRepository, IPermissionService permissionService)
    {
        _versionRepository = versionRepository;
        _permissionService = permissionService;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    public class AppendUpdateRequest
    {
        public string UpdateBase64 { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> Append(Guid documentId, Guid tabId, AppendUpdateRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Editor))
            return Forbid();

        await _versionRepository.AppendUpdateAsync(new DocumentUpdate
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            TabId = tabId,
            UpdateBinary = Convert.FromBase64String(request.UpdateBase64),
            AuthorId = CallerId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return NoContent();
    }

    public record UpdateResponse(string UpdateBase64);

    [HttpGet]
    public async Task<ActionResult<List<UpdateResponse>>> List(Guid documentId, Guid tabId)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Viewer))
            return Forbid();

        var updates = await _versionRepository.ListUpdatesAsync(tabId);
        return Ok(updates.Select(u => new UpdateResponse(Convert.ToBase64String(u.UpdateBinary))).ToList());
    }
}
