var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<Innovayse.Docs.Application.Sharing.IFolderPermissionRepository,
    Innovayse.Docs.Infrastructure.Repositories.FolderPermissionRepository>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
