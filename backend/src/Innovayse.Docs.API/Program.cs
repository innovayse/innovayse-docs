using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<Innovayse.Docs.Infrastructure.Persistence.DocsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DocsDb")));
builder.Services.AddScoped<Innovayse.Docs.Application.Documents.IDocumentRepository,
    Innovayse.Docs.Infrastructure.Repositories.DocumentRepository>();
builder.Services.AddScoped<Innovayse.Docs.Application.Folders.IFolderRepository,
    Innovayse.Docs.Infrastructure.Repositories.FolderRepository>();
builder.Services.AddScoped<Innovayse.Docs.Application.Sharing.IPermissionRepository,
    Innovayse.Docs.Infrastructure.Repositories.PermissionRepository>();
builder.Services.AddScoped<Innovayse.Docs.Application.Sharing.IFolderPermissionRepository,
    Innovayse.Docs.Infrastructure.Repositories.FolderPermissionRepository>();
builder.Services.AddScoped<Innovayse.Docs.Application.Sharing.IPermissionService,
    Innovayse.Docs.Application.Sharing.PermissionService>();
builder.Services.AddScoped<Innovayse.Docs.Application.Sharing.IShareLinkRepository,
    Innovayse.Docs.Infrastructure.Repositories.ShareLinkRepository>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
