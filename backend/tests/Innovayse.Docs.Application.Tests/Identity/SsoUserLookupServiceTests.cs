using System.Net;
using Innovayse.Docs.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using Xunit;

namespace Innovayse.Docs.Application.Tests.Identity;

public class SsoUserLookupServiceTests
{
    private static (SsoUserLookupService sut, Mock<HttpMessageHandler> handler) BuildSut(
        HttpStatusCode statusCode, string? jsonBody, string? incomingAuthHeader)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = jsonBody is null
                    ? new StringContent(string.Empty)
                    : new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://sso-api.local") };

        var httpContext = new DefaultHttpContext();
        if (incomingAuthHeader is not null)
            httpContext.Request.Headers.Authorization = incomingAuthHeader;
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        return (new SsoUserLookupService(httpClient, contextAccessor.Object), handler);
    }

    [Fact]
    public async Task FindByEmailAsync_UserFound_ReturnsResult()
    {
        var (sut, _) = BuildSut(HttpStatusCode.OK,
            """{"id":"11111111-1111-1111-1111-111111111111","email":"a@b.com","name":"A B"}""",
            "Bearer incoming-token");

        var result = await sut.FindByEmailAsync("a@b.com");

        Assert.NotNull(result);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result!.Id);
        Assert.Equal("a@b.com", result.Email);
        Assert.Equal("A B", result.Name);
    }

    [Fact]
    public async Task FindByEmailAsync_NotFound_ReturnsNull()
    {
        var (sut, _) = BuildSut(HttpStatusCode.NotFound, null, "Bearer incoming-token");

        var result = await sut.FindByEmailAsync("nobody@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindByEmailAsync_ForwardsIncomingAuthorizationHeader()
    {
        var (sut, handler) = BuildSut(HttpStatusCode.NotFound, null, "Bearer the-callers-token");

        await sut.FindByEmailAsync("nobody@example.com");

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.Authorization != null &&
                req.Headers.Authorization.ToString() == "Bearer the-callers-token"),
            ItExpr.IsAny<CancellationToken>());
    }
}
