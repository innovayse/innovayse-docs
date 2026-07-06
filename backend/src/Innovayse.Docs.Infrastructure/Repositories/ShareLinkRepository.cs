using Innovayse.Docs.Application.Sharing;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

public class ShareLinkRepository : IShareLinkRepository
{
    private readonly DocsDbContext _context;
    public ShareLinkRepository(DocsDbContext context) => _context = context;

    public async Task CreateAsync(ShareLink link)
    {
        _context.ShareLinks.Add(link);
        await _context.SaveChangesAsync();
    }

    public Task<ShareLink?> GetByTokenAsync(string token) =>
        _context.ShareLinks.FirstOrDefaultAsync(l => l.Token == token);
}
