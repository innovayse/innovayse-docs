using Innovayse.Docs.Domain.Documents;

namespace Innovayse.Docs.Application.Documents;

public interface IDocumentRepository
{
    Task CreateAsync(Document document);
    Task<Document?> GetByIdAsync(Guid id);
    Task UpdateAsync(Document document);
    Task DeleteAsync(Guid id);
    Task<List<Document>> ListByFolderAsync(Guid? folderId, Guid ownerId);

    /// <summary>Documents the user owns, has been granted direct access to, or that live inside
    /// a folder (or nested subfolder) reachable from a folder shared with them — most recently
    /// updated first. Powers the documents home screen.</summary>
    Task<List<Document>> ListForUserAsync(Guid userId);
}
