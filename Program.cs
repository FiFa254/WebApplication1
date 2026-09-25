using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using DevFolio.Data;
using DevFolio.Infrastructure;

// Small CLI helpers so production needs nothing but the published app:
//   dotnet DevFolio.dll hash-password <password>   → prints a value for Admin__PasswordHash
//   dotnet DevFolio.dll healthcheck                → exit 0 when /health answers 200 (Docker HEALTHCHECK)
if (args.Length > 0 && args[0] == "hash-password")
{
    if (args.Length < 2 || string.IsNullOrEmpty(args[1]))
    {
        Console.Error.WriteLine("Usage: dotnet DevFolio.dll hash-password <password>");
        return 1;
    }

    Console.WriteLine(AdminCredentials.HashPassword(args[1]));
    return 0;
}

if (args.Length > 0 && args[0] == "healthcheck")
{
    var healthPort = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await http.GetAsync($"http://localhost:{healthPort}/health");
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch
    {
        return 1;
    }
}

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

builder.Services.AddControllersWithViews();

var dbProvider = DatabaseConfiguration.ResolveProvider(builder.Configuration);
var connectionString = DatabaseConfiguration.GetConnectionString(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider == DatabaseConfiguration.DatabaseProvider.PostgreSql)
    {
        options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly("DevFolio.PostgresMigrations"));
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");

// Admin sign-in (single account from configuration, see AdminCredentials).
builder.Services.Configure<AdminCredentials>(builder.Configuration.GetSection(AdminCredentials.SectionName));
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "DevFolio.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => RateLimitPolicies.Configure(options, builder.Configuration));

builder.Services.AddSingleton<ImageStorage>();

// Keep data protection keys (auth cookie + antiforgery) across container restarts.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("DevFolio");
if (!string.IsNullOrWhiteSpace(keysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// Behind a reverse proxy (Render, nginx, Traefik) trust X-Forwarded-For / -Proto.
// Only enable when the app is not reachable directly, otherwise clients could spoof their IP.
var trustProxy = builder.Configuration.GetValue<bool>("ForwardedHeaders:TrustAll");
if (trustProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync();
        }
        else
        {
            await db.Database.EnsureCreatedAsync();
        }

        logger.LogInformation("Database ready ({Provider}).", dbProvider);

        if (app.Configuration.GetValue<bool>("Seed:DemoData") && await DemoData.SeedIfEmptyAsync(db))
        {
            logger.LogInformation("Demo profiles added (Seed:DemoData).");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed.");
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }

    if (!app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminCredentials>>().Value.IsConfigured)
    {
        logger.LogWarning("Admin__Username / Admin__PasswordHash are not set: admin sign-in is disabled.");
    }
}

if (trustProxy)
{
    app.UseForwardedHeaders();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Plain-HTTP deployments (PORT set by the host, or a proxy terminating TLS) skip the redirect.
if (string.IsNullOrEmpty(port) && app.Configuration.GetValue("Hosting:UseHttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

app.UseSecurityHeaders();
app.UseStatusCodePagesWithReExecute("/Home/Error");

app.UseStaticFiles();

var images = app.Services.GetRequiredService<ImageStorage>();
var webUploads = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "uploads");
if (!string.Equals(Path.GetFullPath(images.RootPath), Path.GetFullPath(webUploads), StringComparison.OrdinalIgnoreCase))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(images.RootPath),
        RequestPath = ImageStorage.RequestPath
    });
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
return 0;

public partial class Program;
