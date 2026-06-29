using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartLink.Api.Config;
using SmartLink.Api.Data;
using SmartLink.Api.Endpoints;
using SmartLink.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Options ----
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<DeezerOptions>(builder.Configuration.GetSection("Deezer"));
builder.Services.Configure<SpotifyOptions>(builder.Configuration.GetSection("Spotify"));
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection("AdminSeed"));
builder.Services.Configure<AppUrlsOptions>(builder.Configuration.GetSection("AppUrls"));

// ---- Database ----
// Managed platforms (Render, Heroku, …) expose a single DATABASE_URL; convert it
// to the Npgsql key=value form. Falls back to the configured connection string.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = !string.IsNullOrWhiteSpace(databaseUrl)
    ? NpgsqlFromUrl(databaseUrl)
    : builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=smartlink;Username=smartlink;Password=smartlink";
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));

// ---- HTTP clients for outbound integrations ----
builder.Services.AddHttpClient("odesli", c =>
{
    c.BaseAddress = new Uri("https://api.song.link/v1-alpha.1/");
    c.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient("deezer", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient("spotify", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient("artwork", c => c.Timeout = TimeSpan.FromSeconds(10));

// ---- App services ----
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<OdesliClient>();
builder.Services.AddScoped<DeezerClient>();
builder.Services.AddScoped<SpotifyClient>();
builder.Services.AddScoped<ColorExtractor>();

// ---- Auth ----
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key))
{
    // Fall back to a dev key when none is configured, and make sure the bound
    // options (used by TokenService) get the same value.
    jwt.Key = "dev-only-insecure-key-change-me-0123456789abcdef";
    builder.Services.PostConfigure<JwtOptions>(o =>
    {
        if (string.IsNullOrWhiteSpace(o.Key)) o.Key = jwt.Key;
    });
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        };
    });
builder.Services.AddAuthorization();

// ---- CORS (frontend dev server + configured web origin) ----
var webBaseUrl = builder.Configuration.GetSection("AppUrls").Get<AppUrlsOptions>()?.WebBaseUrl
    ?? "http://localhost:5173";
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(webBaseUrl, "http://localhost:5173", "http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPublicEndpoints();
app.MapDeezerEndpoints();
app.MapSpotifyEndpoints();
app.MapAdminEndpoints();

await app.MigrateAndSeedAsync();

app.Run();

// Converts a postgres://user:pass@host:port/db URL into an Npgsql connection string.
static string NpgsqlFromUrl(string url)
{
    var uri = new Uri(url);
    var creds = uri.UserInfo.Split(':', 2);
    var database = uri.AbsolutePath.Trim('/');
    var port = uri.Port > 0 ? uri.Port : 5432;
    var user = Uri.UnescapeDataString(creds[0]);
    var pass = creds.Length > 1 ? Uri.UnescapeDataString(creds[1]) : "";
    return $"Host={uri.Host};Port={port};Database={database};Username={user};Password={pass};" +
           "SSL Mode=Prefer;Trust Server Certificate=true";
}
