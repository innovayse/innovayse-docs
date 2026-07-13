using System.Security.Claims;
using Innovayse.Docs.Application.Documents;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Documents;

[ApiController]
[Route("documents/{documentId}/tabs")]
[Authorize]
public class DocumentTabsController : ControllerBase
{
    private readonly IDocumentTabRepository _tabRepository;
    private readonly IPermissionService _permissionService;
    private Guid? _callerIdOverride;

    public DocumentTabsController(IDocumentTabRepository tabRepository, IPermissionService permissionService)
    {
        _tabRepository = tabRepository;
        _permissionService = permissionService;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    public record TabResponse(Guid Id, string Title, int OrderIndex);

    [HttpGet]
    public async Task<ActionResult<List<TabResponse>>> List(Guid documentId)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Viewer))
            return Forbid();

        var tabs = await _tabRepository.ListForDocumentAsync(documentId);
        return Ok(tabs.Select(t => new TabResponse(t.Id, t.Title, t.OrderIndex)).ToList());
    }

    public class CreateTabRequest
    {
        public string Title { get; set; } = "Untitled tab";
    }

    [HttpPost]
    public async Task<ActionResult<TabResponse>> Create(Guid documentId, CreateTabRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Editor))
            return Forbid();

        var existingTabs = await _tabRepository.ListForDocumentAsync(documentId);
        var nextOrderIndex = existingTabs.Count == 0 ? 0 : existingTabs.Max(t => t.OrderIndex) + 1;

        var tab = new DocumentTab
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Title = request.Title,
            OrderIndex = nextOrderIndex,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _tabRepository.CreateAsync(tab);

        return Created($"/documents/{documentId}/tabs/{tab.Id}", new TabResponse(tab.Id, tab.Title, tab.OrderIndex));
    }

    public class UpdateTabRequest
    {
        public string? Title { get; set; }
        public int? OrderIndex { get; set; }
    }

    [HttpPatch("{tabId}")]
    public async Task<ActionResult<TabResponse>> Update(Guid documentId, Guid tabId, UpdateTabRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Editor))
            return Forbid();

        var tab = await _tabRepository.GetByIdAsync(tabId);
        if (tab is null || tab.DocumentId != documentId) return NotFound();

        if (request.Title is not null) tab.Title = request.Title;
        if (request.OrderIndex is not null) tab.OrderIndex = request.OrderIndex.Value;
        tab.UpdatedAt = DateTimeOffset.UtcNow;

        await _tabRepository.UpdateAsync(tab);
        return Ok(new TabResponse(tab.Id, tab.Title, tab.OrderIndex));
    }

    [HttpDelete("{tabId}")]
    public async Task<IActionResult> Delete(Guid documentId, Guid tabId)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Editor))
            return Forbid();

        var tab = await _tabRepository.GetByIdAsync(tabId);
        if (tab is null || tab.DocumentId != documentId) return NotFound();

        var deleted = await _tabRepository.DeleteIfNotLastAsync(tabId);
        if (!deleted)
            return Conflict(new { message = "A document must have at least one tab." });

        return NoContent();
    }
}
