using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WebApplication1.Data;

namespace WebApplication1.PostgresMigrations;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=WebApplication1Db;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly("WebApplication1.PostgresMigrations"));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
