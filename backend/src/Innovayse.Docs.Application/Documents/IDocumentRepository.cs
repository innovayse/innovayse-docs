using Innovayse.Docs.Domain.Documents;

namespace Innovayse.Docs.Application.Documents;

public interface IDocumentRepository
{
    Task CreateAsync(Document document);
    Task<Document?> GetByIdAsync(Guid id);
    Task UpdateAsync(Document document);
    Task DeleteAsync(Guid id);
    Task<List<Document>> ListByFolderAsync(Guid? folderId, Guid ownerId);

    /// <summary>Documents the user owns or has been granted direct access to,
    /// most recently updated first — powers the documents home screen.</summary>
    Task<List<Document>> ListForUserAsync(Guid userId);
}
