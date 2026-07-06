using Innovayse.Docs.Domain.Documents;

namespace Innovayse.Docs.Application.Documents;

public interface IDocumentRepository
{
    Task CreateAsync(Document document);
    Task<Document?> GetByIdAsync(Guid id);
    Task UpdateAsync(Document document);
    Task DeleteAsync(Guid id);
    Task<List<Document>> ListByFolderAsync(Guid? folderId, Guid ownerId);
}
