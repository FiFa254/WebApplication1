using Microsoft.EntityFrameworkCore;
using DevFolio.Models;

namespace DevFolio.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileProject> ProfileProjects => Set<ProfileProject>();
}
