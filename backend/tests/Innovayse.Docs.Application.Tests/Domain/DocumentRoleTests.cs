using Innovayse.Docs.Domain.Sharing;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Domain;

public class DocumentRoleTests
{
    [Theory]
    [InlineData(DocumentRole.Owner, DocumentRole.Editor, true)]
    [InlineData(DocumentRole.Editor, DocumentRole.Commenter, true)]
    [InlineData(DocumentRole.Commenter, DocumentRole.Viewer, true)]
    [InlineData(DocumentRole.Viewer, DocumentRole.Editor, false)]
    [InlineData(DocumentRole.Commenter, DocumentRole.Editor, false)]
    public void Satisfies_ReturnsExpected(DocumentRole held, DocumentRole required, bool expected)
    {
        Assert.Equal(expected, held.Satisfies(required));
    }
}
