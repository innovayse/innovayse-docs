using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public interface IPermissionService
{
    Task<bool> AuthorizeAsync(Guid documentId, Guid userId, DocumentRole required);
}
