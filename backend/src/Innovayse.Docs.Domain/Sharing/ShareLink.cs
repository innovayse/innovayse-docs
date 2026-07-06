namespace Innovayse.Docs.Domain.Sharing;

public class ShareLink
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DocumentRole Role { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
