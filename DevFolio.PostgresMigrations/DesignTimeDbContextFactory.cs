using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DevFolio.Data;

namespace DevFolio.PostgresMigrations;

/// <summary>
/// Used only by `dotnet ef migrations add ... --project DevFolio.PostgresMigrations`.
/// Set DEVFOLIO_PG to a Npgsql connection string; migrations generation does not need a live database.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DEVFOLIO_PG")
            ?? "Host=localhost;Database=DevFolioDb;Username=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly("DevFolio.PostgresMigrations"));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
