using Innovayse.Docs.Application.Comments;
using Innovayse.Docs.Domain.Comments;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly DocsDbContext _context;
    public CommentRepository(DocsDbContext context) => _context = context;

    public async Task CreateAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
    }

    public Task<List<Comment>> ListForDocumentAsync(Guid documentId) =>
        _context.Comments.Where(c => c.DocumentId == documentId).ToListAsync();
}
