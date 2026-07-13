namespace Innovayse.Docs.Domain.Versions;

public class DocumentUpdate
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TabId { get; set; }
    public byte[] UpdateBinary { get; set; } = Array.Empty<byte>();
    public Guid AuthorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
