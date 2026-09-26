using System.Net;
using System.Text.RegularExpressions;
using DevFolio.Data;
using DevFolio.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevFolio.Tests;

/// <summary>
/// Runs the real app with an in-memory database, a temp uploads folder and a known admin account.
/// </summary>
public sealed class DevFolioFactory : WebApplicationFactory<Program>
{
    public const string AdminUser = "admin";
    public const string AdminPassword = "Correct-Horse-9!";

    private readonly string _databaseName = $"devfolio-tests-{Guid.NewGuid():N}";

    /// <summary>High by default so tests that sign in many times are not throttled.</summary>
    public int LoginPerMinute { get; init; } = 1000;

    public string UploadsPath { get; } = Path.Combine(Path.GetTempPath(), $"devfolio-uploads-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Admin:Username", AdminUser);
        builder.UseSetting("Admin:PasswordHash", AdminCredentials.HashPassword(AdminPassword));
        builder.UseSetting("Storage:UploadsPath", UploadsPath);
        builder.UseSetting("Hosting:UseHttpsRedirection", "false");
        builder.UseSetting("RateLimiting:LoginPerMinute", LoginPerMinute.ToString());
        builder.UseSetting("RateLimiting:ContactPerMinute", "1000");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    public async Task<HttpClient> CreateAdminAsync()
    {
        var client = CreateBrowser();
        var token = await GetAntiforgeryTokenAsync(client, "/Account/Login");
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = AdminUser,
            ["Password"] = AdminPassword,
            ["__RequestVerificationToken"] = token
        }));

        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            throw new InvalidOperationException($"Admin login failed: {response.StatusCode}");
        }

        return client;
    }

    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        if (!match.Success)
        {
            throw new InvalidOperationException($"No antiforgery token on {url}.");
        }

        return match.Groups[1].Value;
    }

    public async Task SeedAsync(Action<ApplicationDbContext> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        seed(db);
        await db.SaveChangesAsync();
    }

    public T WithDb<T>(Func<ApplicationDbContext, T> read)
    {
        using var scope = Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(UploadsPath))
        {
            Directory.Delete(UploadsPath, recursive: true);
        }
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(T)).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
