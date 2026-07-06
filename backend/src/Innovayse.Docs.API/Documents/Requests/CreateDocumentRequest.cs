namespace Innovayse.Docs.API.Documents.Requests;

public class CreateDocumentRequest
{
    public string Title { get; set; } = "Untitled document";
    public Guid? FolderId { get; set; }
}
