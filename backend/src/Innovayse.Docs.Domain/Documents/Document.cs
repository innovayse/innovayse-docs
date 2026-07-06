namespace Innovayse.Docs.Domain.Documents;

public class Document
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "Untitled document";
    public Guid? FolderId { get; set; }
    public Guid OwnerId { get; set; }
    public byte[]? ContentSnapshot { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
