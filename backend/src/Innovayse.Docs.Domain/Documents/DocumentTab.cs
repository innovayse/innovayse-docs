namespace Innovayse.Docs.Domain.Documents;

public class DocumentTab
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = "Tab 1";
    public int OrderIndex { get; set; }
    public byte[]? ContentSnapshot { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
