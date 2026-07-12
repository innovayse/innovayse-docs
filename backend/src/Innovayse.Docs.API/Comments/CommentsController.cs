using System.Security.Claims;
using Innovayse.Docs.Application.Comments;
using Innovayse.Docs.Application.Notifications;
using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Comments;
using Innovayse.Docs.Domain.Notifications;
using Innovayse.Docs.Domain.Sharing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Innovayse.Docs.API.Comments;

[ApiController]
[Route("documents/{documentId}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPermissionService _permissionService;
    private readonly INotificationRepository _notificationRepository;
    private Guid? _callerIdOverride;

    public CommentsController(
        ICommentRepository commentRepository,
        IPermissionService permissionService,
        INotificationRepository notificationRepository)
    {
        _commentRepository = commentRepository;
        _permissionService = permissionService;
        _notificationRepository = notificationRepository;
    }

    internal void SetCallerIdForTesting(Guid callerId) => _callerIdOverride = callerId;

    private Guid CallerId => _callerIdOverride ??
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing sub claim"));

    public class CreateCommentRequest
    {
        public string Text { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int AnchorPosition { get; set; }
        public Guid? ParentCommentId { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<Comment>> Create(Guid documentId, CreateCommentRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Commenter))
            return Forbid();

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            AnchorPosition = request.AnchorPosition,
            AuthorId = CallerId,
            AuthorName = request.AuthorName,
            Text = request.Text,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _commentRepository.CreateAsync(comment);

        var participantIds = await _permissionService.GetDocumentParticipantUserIdsAsync(documentId) ?? [];
        var notificationType = request.ParentCommentId.HasValue ? NotificationType.NewReply : NotificationType.NewComment;
        var previewText = request.Text.Length > 80 ? request.Text[..80] : request.Text;
        foreach (var recipientId in participantIds.Where(id => id != CallerId))
        {
            await _notificationRepository.CreateAsync(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipientId,
                Type = notificationType,
                ActorUserId = CallerId,
                ActorName = request.AuthorName,
                DocumentId = documentId,
                PreviewText = previewText,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return Created($"/documents/{documentId}/comments/{comment.Id}", comment);
    }

    [HttpGet]
    public async Task<ActionResult<List<Comment>>> List(Guid documentId)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Viewer))
            return Forbid();

        return Ok(await _commentRepository.ListForDocumentAsync(documentId));
    }

    public class UpdateCommentRequest
    {
        public bool Resolved { get; set; }
    }

    [HttpPatch("{commentId}")]
    public async Task<IActionResult> Update(Guid documentId, Guid commentId, UpdateCommentRequest request)
    {
        if (!await _permissionService.AuthorizeAsync(documentId, CallerId, DocumentRole.Commenter))
            return Forbid();

        var comment = await _commentRepository.GetByIdAsync(commentId);
        if (comment is null || comment.DocumentId != documentId)
            return NotFound();

        comment.Resolved = request.Resolved;
        await _commentRepository.UpdateAsync(comment);
        return NoContent();
    }
}
