using Innovayse.Docs.Domain.Documents;

namespace Innovayse.Docs.Application.Folders;

public interface IFolderRepository
{
    Task CreateAsync(Folder folder);
    Task<Folder?> GetByIdAsync(Guid id);
    Task<List<Folder>> ListForUserAsync(Guid ownerId);
}
