using Api.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Database;

public class ManagerContext(DbContextOptions<ManagerContext> options) : DbContext
{
    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Password).IsRequired();
            entity.OwnsOne(e => e.Name, name =>
            {
                name.Property(n => n.Value.FirstName).IsRequired().HasMaxLength(100);
                name.Property(n => n.Value.FirstName).IsRequired().HasMaxLength(100);
            });
        });
    }
}