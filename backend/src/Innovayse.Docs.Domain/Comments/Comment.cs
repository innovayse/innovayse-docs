namespace Innovayse.Docs.Domain.Comments;

public class Comment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int AnchorPosition { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Resolved { get; set; }
    public Guid? ParentCommentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
