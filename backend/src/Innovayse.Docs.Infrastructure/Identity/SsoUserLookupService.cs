using System.Net;
using System.Net.Http.Json;
using Innovayse.Docs.Application.Identity;
using Microsoft.AspNetCore.Http;

namespace Innovayse.Docs.Infrastructure.Identity;

public class SsoUserLookupService : ISsoUserLookupService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SsoUserLookupService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    private record LookupResponseDto(string Id, string Email, string Name);

    public async Task<SsoUserLookupResult?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/lookup?email={Uri.EscapeDataString(email)}");

        // Forward the caller's own token — the SSO lookup endpoint accepts any
        // authenticated Innovayse user, and this keeps the call attributable to
        // whoever is sending the invite rather than a separate service credential.
        var incomingAuth = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(incomingAuth))
            request.Headers.TryAddWithoutValidation("Authorization", incomingAuth);

        var response = await _httpClient.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LookupResponseDto>(cancellationToken: ct);
        if (body is null) return null;
        return new SsoUserLookupResult(Guid.Parse(body.Id), body.Email, body.Name);
    }
}
