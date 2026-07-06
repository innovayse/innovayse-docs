namespace Innovayse.Docs.Domain.Sharing;

public enum DocumentRole
{
    Viewer = 0,
    Commenter = 1,
    Editor = 2,
    Owner = 3
}

public static class DocumentRoleExtensions
{
    public static bool Satisfies(this DocumentRole held, DocumentRole required) => held >= required;
}
