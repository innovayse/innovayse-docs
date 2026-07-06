namespace Innovayse.Docs.Domain.Sharing;

public class DocumentPermission
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public DocumentRole Role { get; set; }
    public Guid GrantedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
