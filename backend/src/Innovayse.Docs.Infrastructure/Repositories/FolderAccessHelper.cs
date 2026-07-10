using Innovayse.Docs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Repositories;

/// <summary>Expands a user's direct folder shares into the full set of folder IDs they can
/// reach — the shared folder itself plus every descendant, recursively. Loads the whole
/// Folders table into memory to build the parent→children map; acceptable at this app's
/// current scale (a single tenant's folder tree), revisit with a recursive CTE if it grows
/// large enough to matter.</summary>
internal static class FolderAccessHelper
{
    public static async Task<HashSet<Guid>> GetAccessibleFolderIdsAsync(DocsDbContext context, Guid userId)
    {
        var allFolders = await context.Folders.ToListAsync();
        var childrenByParent = allFolders
            .Where(f => f.ParentFolderId.HasValue)
            .GroupBy(f => f.ParentFolderId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(f => f.Id).ToList());

        var directGrantFolderIds = await context.FolderPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.FolderId)
            .ToListAsync();

        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>(directGrantFolderIds);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!result.Add(id)) continue;
            if (childrenByParent.TryGetValue(id, out var children))
                foreach (var child in children) queue.Enqueue(child);
        }
        return result;
    }
}
