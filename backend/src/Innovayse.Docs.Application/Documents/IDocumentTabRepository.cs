using Innovayse.Docs.Domain.Documents;

namespace Innovayse.Docs.Application.Documents;

public interface IDocumentTabRepository
{
    Task CreateAsync(DocumentTab tab);
    Task<DocumentTab?> GetByIdAsync(Guid tabId);
    Task UpdateAsync(DocumentTab tab);
    Task DeleteAsync(Guid tabId);

    /// <summary>Tabs belonging to a document, ordered by OrderIndex — the order the
    /// tab sidebar displays them in.</summary>
    Task<List<DocumentTab>> ListForDocumentAsync(Guid documentId);
}
