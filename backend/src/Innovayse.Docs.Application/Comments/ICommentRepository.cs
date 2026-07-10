using Innovayse.Docs.Domain.Comments;

namespace Innovayse.Docs.Application.Comments;

public interface ICommentRepository
{
    Task CreateAsync(Comment comment);
    Task<List<Comment>> ListForDocumentAsync(Guid documentId);
    Task<Comment?> GetByIdAsync(Guid id);
    Task UpdateAsync(Comment comment);
}
