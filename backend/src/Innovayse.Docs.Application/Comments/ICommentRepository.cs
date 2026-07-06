using Innovayse.Docs.Domain.Comments;

namespace Innovayse.Docs.Application.Comments;

public interface ICommentRepository
{
    Task CreateAsync(Comment comment);
    Task<List<Comment>> ListForDocumentAsync(Guid documentId);
}
