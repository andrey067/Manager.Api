using Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Database.Mapping;

public class UserMapping : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Email).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Password).IsRequired();
        builder.ComplexProperty(e => e.Name);
    }
}