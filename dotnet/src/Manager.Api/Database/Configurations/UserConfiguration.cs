using Manager.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Manager.Api.Database.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .UseIdentityByDefaultColumn()
            .HasColumnType("bigint");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(80)
            .HasColumnName("name")
            .HasColumnType("VARCHAR(80)");

        builder.Property(x => x.Password)
            .IsRequired()
            .HasMaxLength(1000)
            .HasColumnName("password")
            .HasColumnType("VARCHAR(1000)");

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(180)
            .HasColumnName("email")
            .HasColumnType("VARCHAR(180)");

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("IX_User_Email");

        builder.Property(x => x.RefreshTokenHash)
            .HasMaxLength(128)
            .HasColumnName("refresh_token_hash")
            .HasColumnType("VARCHAR(128)");

        builder.Property(x => x.RefreshTokenExpiresAt)
            .HasColumnName("refresh_token_expires_at")
            .HasColumnType("timestamp with time zone");
    }
}
