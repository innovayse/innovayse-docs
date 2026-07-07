using System.Security.Claims;
using Innovayse.Docs.Application.Folders;
using Innovayse.Docs.Domain.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Folders;

[ApiController]
[Route("folders")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly IFolderRepository _folderRepository;
    private Guid? _callerIdOverride;

    public FoldersController(IFolderRepository folderRepository) => _folderRepository = folderRepository;

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    public class CreateFolderRequest
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentFolderId { get; set; }
    }

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
    public async Task<ActionResult<List<Folder>>> List() =>
        Ok(await _folderRepository.ListForUserAsync(CallerId));

    [HttpGet("{id}")]
    public async Task<ActionResult<Folder>> Get(Guid id)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        return folder is null ? NotFound() : Ok(folder);
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
