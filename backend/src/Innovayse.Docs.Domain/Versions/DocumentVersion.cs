namespace Innovayse.Docs.Domain.Versions;

public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public byte[] Snapshot { get; set; } = Array.Empty<byte>();
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Label { get; set; }
}
