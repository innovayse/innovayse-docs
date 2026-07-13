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

    /// <summary>Atomically deletes a tab unless it is the document's last remaining tab.
    /// Returns false (without deleting) if the tab doesn't exist, if it is the last tab,
    /// or if a concurrent delete won a race for the last-tab guard.</summary>
    Task<bool> DeleteIfNotLastAsync(Guid tabId);
}
