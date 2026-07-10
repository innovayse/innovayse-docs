using System.Security.Claims;
using Innovayse.Docs.Application.Folders;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Folders;

[ApiController]
[Route("folders")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly IFolderRepository _folderRepository;
    private readonly IPermissionService _permissionService;
    private Guid? _callerIdOverride;

    public FoldersController(IFolderRepository folderRepository, IPermissionService permissionService)
    {
        _folderRepository = folderRepository;
        _permissionService = permissionService;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    public class CreateFolderRequest
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentFolderId { get; set; }
    }

    /// <summary>Adds the caller's effective role — the frontend uses this to decide whether
    /// to show the Share button (Owner-only) and whether to badge the folder as shared.</summary>
    public record FolderWithRoleResponse(Guid Id, string Name, Guid? ParentFolderId, Guid OwnerId, DocumentRole Role);

    [HttpPost]
    public async Task<ActionResult<Folder>> Create(CreateFolderRequest request)
    {
        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ParentFolderId = request.ParentFolderId,
            OwnerId = CallerId
        };
        await _folderRepository.CreateAsync(folder);
        return Created($"/folders/{folder.Id}", folder);
    }

    [HttpGet]
    public async Task<ActionResult<List<FolderWithRoleResponse>>> List()
    {
        var folders = await _folderRepository.ListForUserAsync(CallerId);
        var result = new List<FolderWithRoleResponse>();
        foreach (var folder in folders)
        {
            // ListForUserAsync only ever returns folders the caller owns or can reach through
            // a folder share, so this should never actually be null — same defensive fallback
            // pattern as DocumentsController.List.
            var role = await _permissionService.GetEffectiveFolderRoleAsync(folder.Id, CallerId) ?? DocumentRole.Viewer;
            result.Add(new FolderWithRoleResponse(folder.Id, folder.Name, folder.ParentFolderId, folder.OwnerId, role));
        }
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FolderWithRoleResponse>> Get(Guid id)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder is null) return NotFound();

        var role = await _permissionService.GetEffectiveFolderRoleAsync(id, CallerId);
        if (role is null) return Forbid();

        return Ok(new FolderWithRoleResponse(folder.Id, folder.Name, folder.ParentFolderId, folder.OwnerId, role.Value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder is null) return NotFound();

        if (folder.OwnerId != CallerId) return Forbid();

        await _folderRepository.DeleteAsync(id);
        return NoContent();
    }
}
