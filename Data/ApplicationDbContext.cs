using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfileProject> ProfileProjects => Set<ProfileProject>();
}
