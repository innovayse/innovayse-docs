using Innovayse.Docs.Domain.Documents;

namespace Innovayse.Docs.Application.Folders;

public interface IFolderRepository
{
    Task CreateAsync(Folder folder);
    Task<Folder?> GetByIdAsync(Guid id);
    Task<List<Folder>> ListForUserAsync(Guid ownerId);

    /// <summary>Deletes the folder and un-files any documents that were in it
    /// (their FolderId is cleared, not deleted).</summary>
    Task DeleteAsync(Guid id);
}
