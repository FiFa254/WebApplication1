using DevFolio.Models;
using Microsoft.EntityFrameworkCore;

namespace DevFolio.Data;

/// <summary>
/// Sample profiles for a public demo. Runs only when Seed:DemoData is true and the database has no profiles,
/// so it never touches real data. All people and e-mail addresses are fictional (example.com).
/// </summary>
public static class DemoData
{
    public static async Task<bool> SeedIfEmptyAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Profiles.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = DateTime.UtcNow;
        db.Profiles.AddRange(
            new Profile
            {
                FirstName = "Mali",
                LastName = "Srisuk",
                Email = "mali.srisuk@example.com",
                Bio = "Backend developer focused on .NET APIs, SQL performance, and clean architecture.",
                CreatedAt = now.AddDays(-3),
                Projects =
                [
                    new ProfileProject
                    {
                        ProjectName = "Inventory API",
                        Description = "REST API for stock tracking with ASP.NET Core, EF Core, and PostgreSQL. Includes paging, validation, and integration tests.",
                        GithubLink = "https://github.com/example/inventory-api"
                    },
                    new ProfileProject
                    {
                        ProjectName = "Report Scheduler",
                        Description = "Background service that builds daily sales reports and e-mails them as CSV.",
                        GithubLink = "https://github.com/example/report-scheduler"
                    }
                ]
            },
            new Profile
            {
                FirstName = "Krit",
                LastName = "Wongsa",
                Email = "krit.wongsa@example.com",
                Bio = "Frontend developer who enjoys accessible UI, React, and design systems.",
                CreatedAt = now.AddDays(-2),
                Projects =
                [
                    new ProfileProject
                    {
                        ProjectName = "Design Tokens Kit",
                        Description = "Shared colors, spacing, and typography tokens exported to CSS variables and TypeScript.",
                        GithubLink = "https://github.com/example/design-tokens-kit"
                    }
                ]
            },
            new Profile
            {
                FirstName = "Nicha",
                LastName = "Chaiyo",
                Email = "nicha.chaiyo@example.com",
                Bio = "Full-stack developer building small business tools with C#, WinForms, and SQL Server.",
                CreatedAt = now.AddDays(-1),
                Projects =
                [
                    new ProfileProject
                    {
                        ProjectName = "Customer Hub",
                        Description = "Desktop app for managing customers with Excel import, CSV reports, and role-based access.",
                        GithubLink = "https://github.com/example/customer-hub"
                    },
                    new ProfileProject
                    {
                        ProjectName = "Stock Watch",
                        Description = "Watches product pages and records when items come back in stock."
                    }
                ]
            });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
