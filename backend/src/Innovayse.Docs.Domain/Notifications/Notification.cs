namespace Innovayse.Docs.Domain.Notifications;

public class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public NotificationType Type { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public Guid? DocumentId { get; set; }
    public Guid? FolderId { get; set; }
    public string PreviewText { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}
