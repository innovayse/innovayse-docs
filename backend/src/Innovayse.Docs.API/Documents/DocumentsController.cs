using System.Security.Claims;
using Innovayse.Docs.API.Documents.Requests;
using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Documents;

[ApiController]
[Route("documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IPermissionService _permissionService;
    private Guid? _callerIdOverride;

    public DocumentsController(IDocumentRepository documentRepository, IPermissionService permissionService)
    {
        _documentRepository = documentRepository;
        _permissionService = permissionService;
    }

    // Test seam: production code reads the caller from the JWT `sub` claim.
    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    [HttpPost]
    public async Task<ActionResult<Document>> Create(CreateDocumentRequest request)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            FolderId = request.FolderId,
            OwnerId = CallerId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _documentRepository.CreateAsync(document);
        return Created($"/documents/{document.Id}", document);
    }

    [HttpGet]
    public async Task<ActionResult<List<Document>>> List() =>
        Ok(await _documentRepository.ListForUserAsync(CallerId));

    /// <summary>Adds the caller's effective role to the document — the frontend uses this
    /// to decide whether the editor should be editable and whether to show formatting
    /// controls (Viewer/Commenter must not be able to write to the document body).</summary>
    public record DocumentWithRoleResponse(
        Guid Id,
        string Title,
        Guid? FolderId,
        Guid OwnerId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        DocumentRole Role);

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentWithRoleResponse>> Get(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document is null) return NotFound();

        var role = await _permissionService.GetEffectiveRoleAsync(id, CallerId);
        if (role is null) return Forbid();

        return Ok(new DocumentWithRoleResponse(
            document.Id,
            document.Title,
            document.FolderId,
            document.OwnerId,
            document.CreatedAt,
            document.UpdatedAt,
            role.Value));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<Document>> Update(Guid id, CreateDocumentRequest request)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document is null) return NotFound();

        if (!await _permissionService.AuthorizeAsync(id, CallerId, DocumentRole.Editor))
            return Forbid();

        document.Title = request.Title;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _documentRepository.UpdateAsync(document);
        return Ok(document);
    }

    public class MoveDocumentRequest
    {
        public Guid? FolderId { get; set; }
    }

    [HttpPatch("{id}/folder")]
    public async Task<ActionResult<Document>> Move(Guid id, MoveDocumentRequest request)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document is null) return NotFound();

        if (!await _permissionService.AuthorizeAsync(id, CallerId, DocumentRole.Editor))
            return Forbid();

        document.FolderId = request.FolderId;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await _documentRepository.UpdateAsync(document);
        return Ok(document);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var document = await _documentRepository.GetByIdAsync(id);
        if (document is null) return NotFound();

        if (!await _permissionService.AuthorizeAsync(id, CallerId, DocumentRole.Owner))
            return Forbid();

        await _documentRepository.DeleteAsync(id);
        return NoContent();
    }
}
