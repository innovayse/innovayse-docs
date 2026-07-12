using Innovayse.Docs.Domain.Comments;
using Innovayse.Docs.Domain.Documents;
using Innovayse.Docs.Domain.Notifications;
using Innovayse.Docs.Domain.Sharing;
using Innovayse.Docs.Domain.Versions;
using Microsoft.EntityFrameworkCore;

namespace Innovayse.Docs.Infrastructure.Persistence;

public class DocsDbContext : DbContext
{
    public DocsDbContext(DbContextOptions<DocsDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<DocumentPermission> DocumentPermissions => Set<DocumentPermission>();
    public DbSet<FolderPermission> FolderPermissions => Set<FolderPermission>();
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<DocumentUpdate> DocumentUpdates => Set<DocumentUpdate>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>().HasKey(d => d.Id);
        modelBuilder.Entity<Folder>().HasKey(f => f.Id);
        modelBuilder.Entity<DocumentPermission>().HasKey(p => p.Id);
        modelBuilder.Entity<FolderPermission>().HasKey(p => p.Id);
        modelBuilder.Entity<ShareLink>().HasKey(s => s.Id);
        modelBuilder.Entity<ShareLink>().HasIndex(s => s.Token).IsUnique();
        modelBuilder.Entity<Comment>().HasKey(c => c.Id);
        modelBuilder.Entity<DocumentUpdate>().HasKey(u => u.Id);
        modelBuilder.Entity<DocumentVersion>().HasKey(v => v.Id);
        modelBuilder.Entity<Notification>().HasKey(n => n.Id);
    }
}
