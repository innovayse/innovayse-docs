using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<Innovayse.Docs.Application.Identity.ISsoUserLookupService,
    Innovayse.Docs.Infrastructure.Identity.SsoUserLookupService>(client =>
{
    // sso-api.local (not sso.local) — the bare host is proxied to the SSO *frontend*
    // container, not its API, and would 404 on this call. See project memory for the
    // NPM routing gotcha this mirrors.
    client.BaseAddress = new Uri(builder.Configuration["Sso:ApiBaseUrl"] ?? "http://sso-api.local");
});
builder.Services.AddScoped<Innovayse.Docs.Application.Comments.ICommentRepository,
    Innovayse.Docs.Infrastructure.Repositories.CommentRepository>();
builder.Services.AddScoped<Innovayse.Docs.Application.Versions.IVersionRepository,
    Innovayse.Docs.Infrastructure.Repositories.VersionRepository>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        // Frontend and the collab service compare roles as strings (e.g. "Viewer");
        // without this, DocumentRole serializes as its raw int and those checks
        // silently never match (readOnly is always false, invite/share requests
        // fail to bind).
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3100"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Sso:Authority"] ?? "http://sso.local";
        options.RequireHttpsMetadata = builder.Environment.IsProduction();
        // OpenIddict access tokens carry no `aud` claim by default — matches the
        // ValidateAudience=false pattern used by the other Innovayse SSO clients
        // (see hostpanel/innovayse-main Program.cs).
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

app.Run();

public partial class Program { }
