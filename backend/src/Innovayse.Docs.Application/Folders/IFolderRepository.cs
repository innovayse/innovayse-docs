using Innovayse.Docs.Domain.Documents;

namespace Innovayse.Docs.Application.Folders;

public interface IFolderRepository
{
    Task CreateAsync(Folder folder);
    Task<Folder?> GetByIdAsync(Guid id);

    /// <summary>Folders the user owns, plus every folder reachable from a folder directly
    /// shared with them (the shared folder itself and all its nested subfolders,
    /// recursively) — powers both the top-level "your folders" list and, via client-side
    /// ParentFolderId filtering, navigation into a shared folder's subtree.</summary>
    Task<List<Folder>> ListForUserAsync(Guid userId);

    /// <summary>Deletes the folder and un-files any documents that were in it
    /// (their FolderId is cleared, not deleted).</summary>
    Task DeleteAsync(Guid id);
}
