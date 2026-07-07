namespace Innovayse.Docs.Application.Identity;

public record SsoUserLookupResult(Guid Id, string Email, string Name);

public interface ISsoUserLookupService
{
    Task<SsoUserLookupResult?> FindByEmailAsync(string email, CancellationToken ct = default);
}
