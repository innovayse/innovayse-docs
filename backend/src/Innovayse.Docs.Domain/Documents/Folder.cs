namespace Innovayse.Docs.Domain.Documents;

public class Folder
{
    public Guid Id { get; set; }
    public Guid? ParentFolderId { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
}
