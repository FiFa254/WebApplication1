using System.Net;
using System.Net.Http.Headers;
using DevFolio.Infrastructure;
using DevFolio.Models;
using Microsoft.EntityFrameworkCore;

namespace DevFolio.Tests;

public class PublicPageTests : IClassFixture<DevFolioFactory>
{
    private readonly DevFolioFactory _factory;

    public PublicPageTests(DevFolioFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Profiles")]
    [InlineData("/Home/Projects")]
    [InlineData("/Account/Login")]
    public async Task PublicPages_Load(string url)
    {
        var response = await _factory.CreateBrowser().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await _factory.CreateBrowser().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownProfile_Returns404Page()
    {
        var response = await _factory.CreateBrowser().GetAsync("/Home/Profile/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/Home/AddProfile")]
    [InlineData("/Home/EditProfile/1")]
    public async Task AdminPages_RedirectAnonymousToLogin(string url)
    {
        var response = await _factory.CreateBrowser().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task AnonymousNav_HidesAddProfile()
    {
        var html = await _factory.CreateBrowser().GetStringAsync("/");

        Assert.DoesNotContain("href=\"/Home/AddProfile\"", html);
        Assert.Contains("href=\"/Account/Login\"", html);
    }

    [Fact]
    public async Task Profiles_ArePaged_WithProjectCounts()
    {
        await _factory.SeedAsync(db =>
        {
            for (var i = 0; i < 15; i++)
            {
                db.Profiles.Add(new Profile
                {
                    FirstName = $"Paged{i}",
                    LastName = "Dev",
                    Email = $"paged{i}@example.com",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    Projects = [new ProfileProject { ProjectName = "A" }, new ProfileProject { ProjectName = "B" }]
                });
            }
        });

        var client = _factory.CreateBrowser();
        var page1 = await client.GetStringAsync("/Home/Profiles");
        var page2 = await client.GetStringAsync("/Home/Profiles?page=2");

        Assert.Contains("Paged0", page1);
        Assert.Contains("2 project(s)", page1);
        Assert.Contains("Page 1 of", page1);
        Assert.DoesNotContain("Paged0 ", page2);
        Assert.Contains("Page 2 of", page2);
    }
}

public class LoginTests
{
    [Fact]
    public async Task WrongPassword_IsRejected()
    {
        using var factory = new DevFolioFactory();
        var client = factory.CreateBrowser();
        var token = await DevFolioFactory.GetAntiforgeryTokenAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", Form(token, DevFolioFactory.AdminUser, "wrong-password"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid username or password", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_IsRateLimited()
    {
        using var factory = new DevFolioFactory { LoginPerMinute = 3 };
        var client = factory.CreateBrowser();
        var token = await DevFolioFactory.GetAntiforgeryTokenAsync(client, "/Account/Login");

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
        {
            var response = await client.PostAsync("/Account/Login", Form(token, "admin", "wrong"));
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, statuses[2]);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[3]);
    }

    [Fact]
    public async Task NoAdminConfigured_LoginFails()
    {
        using var factory = new DevFolioFactory().WithWebHostBuilder(b => b.UseSetting("Admin:PasswordHash", ""));
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var token = await DevFolioFactory.GetAntiforgeryTokenAsync(client, "/Account/Login");

        var response = await client.PostAsync("/Account/Login", Form(token, DevFolioFactory.AdminUser, DevFolioFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Admin sign-in is disabled", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public void PasswordHash_VerifiesOnlyCorrectCredentials()
    {
        var admin = new AdminCredentials { Username = "Admin", PasswordHash = AdminCredentials.HashPassword("s3cret!") };

        Assert.True(admin.Verify("admin", "s3cret!"));
        Assert.False(admin.Verify("admin", "S3cret!"));
        Assert.False(admin.Verify("other", "s3cret!"));
        Assert.False(new AdminCredentials().Verify("admin", "s3cret!"));
    }

    private static FormUrlEncodedContent Form(string token, string user, string password) => new(new Dictionary<string, string>
    {
        ["Username"] = user,
        ["Password"] = password,
        ["__RequestVerificationToken"] = token
    });
}

public class AdminProfileTests : IClassFixture<DevFolioFactory>
{
    private static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly DevFolioFactory _factory;

    public AdminProfileTests(DevFolioFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_CanAddProfileWithoutProjects()
    {
        var client = await _factory.CreateAdminAsync();

        var response = await PostProfileAsync(client, "/Home/AddProfile", "No", "Projects", "noprojects@example.com",
            projects: [("", "", "")]);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var profile = _factory.WithDb(db => db.Profiles.Include(p => p.Projects).Single(p => p.Email == "noprojects@example.com"));
        Assert.Empty(profile.Projects);
        Assert.Equal($"/Home/Profile/{profile.Id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Admin_AddEditDelete_WithImage()
    {
        var client = await _factory.CreateAdminAsync();

        var add = await PostProfileAsync(client, "/Home/AddProfile", "Ada", "Lovelace", "ada@example.com",
            projects: [("Engine", "Notes", "https://github.com/ada/engine"), ("", "", "")],
            image: ("ada.png", PngBytes));
        Assert.Equal(HttpStatusCode.Redirect, add.StatusCode);

        var created = _factory.WithDb(db => db.Profiles.Include(p => p.Projects).Single(p => p.Email == "ada@example.com"));
        Assert.Single(created.Projects);
        Assert.NotNull(created.ProfileImagePath);
        var imageFile = Path.Combine(_factory.UploadsPath, "profiles", Path.GetFileName(created.ProfileImagePath));
        Assert.True(File.Exists(imageFile));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(created.ProfileImagePath)).StatusCode);

        var edit = await PostProfileAsync(client, $"/Home/EditProfile/{created.Id}", "Ada", "King", "ada@example.com",
            projects: [("Engine v2", null, null), ("Loom", "Pattern cards", null)],
            removeImage: true);
        Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);

        var edited = _factory.WithDb(db => db.Profiles.Include(p => p.Projects).Single(p => p.Id == created.Id));
        Assert.Equal("King", edited.LastName);
        Assert.Equal(["Engine v2", "Loom"], edited.Projects.Select(p => p.ProjectName).OrderBy(n => n));
        Assert.Null(edited.ProfileImagePath);
        Assert.False(File.Exists(imageFile));

        var token = await DevFolioFactory.GetAntiforgeryTokenAsync(client, $"/Home/Profile/{created.Id}");
        var delete = await client.PostAsync($"/Home/DeleteProfile/{created.Id}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);

        Assert.False(_factory.WithDb(db => db.Profiles.Any(p => p.Id == created.Id)));
        Assert.False(_factory.WithDb(db => db.ProfileProjects.Any(p => p.ProfileId == created.Id)));
    }

    [Fact]
    public async Task FakeImage_IsRejected()
    {
        var client = await _factory.CreateAdminAsync();

        var response = await PostProfileAsync(client, "/Home/AddProfile", "Fake", "Image", "fake@example.com",
            projects: [], image: ("evil.png", "<script>alert(1)</script>"u8.ToArray()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("must be a JPG, PNG, or WebP", await response.Content.ReadAsStringAsync());
        Assert.False(_factory.WithDb(db => db.Profiles.Any(p => p.Email == "fake@example.com")));
    }

    [Fact]
    public async Task ProjectWithoutName_IsRejected()
    {
        var client = await _factory.CreateAdminAsync();

        var response = await PostProfileAsync(client, "/Home/AddProfile", "Half", "Filled", "half@example.com",
            projects: [("", "has a description", "")]);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The Project Name field is required.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DuplicateEmail_IsRejected()
    {
        await _factory.SeedAsync(db => db.Profiles.Add(new Profile { FirstName = "A", LastName = "B", Email = "taken@example.com" }));
        var client = await _factory.CreateAdminAsync();

        var response = await PostProfileAsync(client, "/Home/AddProfile", "C", "D", "TAKEN@example.com", projects: []);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("already uses this e-mail", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TooManyProjects_IsRejected()
    {
        var client = await _factory.CreateAdminAsync();
        var projects = Enumerable.Range(0, ProfileViewModel.MaxProjects + 1)
            .Select(i => ((string?)$"P{i}", (string?)null, (string?)null))
            .ToArray();

        var response = await PostProfileAsync(client, "/Home/AddProfile", "Many", "Projects", "many@example.com", projects);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"at most {ProfileViewModel.MaxProjects} projects", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        var client = await _factory.CreateAdminAsync();

        var response = await client.PostAsync("/Home/AddProfile", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FirstName"] = "No",
            ["LastName"] = "Token",
            ["Email"] = "notoken@example.com"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void DetectExtension_RecognisesSignatures()
    {
        Assert.Equal(".png", ImageStorage.DetectExtension(PngBytes.AsSpan(0, 12)));
        Assert.Equal(".jpg", ImageStorage.DetectExtension([0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0]));
        Assert.Equal(".webp", ImageStorage.DetectExtension("RIFF\0\0\0\0WEBP"u8));
        Assert.Null(ImageStorage.DetectExtension("GIF89a......"u8));
    }

    private static async Task<HttpResponseMessage> PostProfileAsync(
        HttpClient client,
        string url,
        string first,
        string last,
        string email,
        (string? Name, string? Description, string? Link)[] projects,
        (string FileName, byte[] Bytes)? image = null,
        bool removeImage = false)
    {
        var formPage = url.StartsWith("/Home/EditProfile", StringComparison.Ordinal) ? url : "/Home/AddProfile";
        var token = await DevFolioFactory.GetAntiforgeryTokenAsync(client, formPage);

        var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(first), "FirstName" },
            { new StringContent(last), "LastName" },
            { new StringContent(email), "Email" },
            { new StringContent(removeImage ? "true" : "false"), "RemoveImage" }
        };

        for (var i = 0; i < projects.Length; i++)
        {
            content.Add(new StringContent(projects[i].Name ?? ""), $"Projects[{i}].ProjectName");
            content.Add(new StringContent(projects[i].Description ?? ""), $"Projects[{i}].Description");
            content.Add(new StringContent(projects[i].Link ?? ""), $"Projects[{i}].GithubLink");
        }

        if (image is { } file)
        {
            var bytes = new ByteArrayContent(file.Bytes);
            bytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(bytes, "ProfileImage", file.FileName);
        }

        return await client.PostAsync(url, content);
    }
}
