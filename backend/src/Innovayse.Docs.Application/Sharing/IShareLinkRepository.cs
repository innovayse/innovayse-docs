using Innovayse.Docs.Domain.Sharing;

namespace Innovayse.Docs.Application.Sharing;

public interface IShareLinkRepository
{
    Task CreateAsync(ShareLink link);
    Task<ShareLink?> GetByTokenAsync(string token);
}
