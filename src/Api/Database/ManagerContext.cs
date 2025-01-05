using Api.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Database;

public class ManagerContext(DbContextOptions<ManagerContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("manager-vertical");

        builder.ApplyConfigurationsFromAssembly(typeof(ManagerContext).Assembly);
    }
}