using Innovayse.Docs.Domain.Versions;

namespace Innovayse.Docs.Application.Versions;

public interface IVersionRepository
{
    Task AppendUpdateAsync(DocumentUpdate update);
    Task<List<DocumentUpdate>> ListUpdatesAsync(Guid tabId);
    Task CreateVersionAsync(DocumentVersion version);
    Task<List<DocumentVersion>> ListForDocumentAsync(Guid documentId);
    Task RestoreAsync(Guid documentId, Guid versionId);
}
