using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.WebAPI.Data.Configurations;

/// <summary>Maps User to PostgreSQL with explicit keys, columns and relationships.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "public");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.KeycloakId).HasColumnName("keycloak_id");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(x => x.FirstName).HasColumnName("first_name");
        builder.Property(x => x.LastName).HasColumnName("last_name");
        builder.HasAlternateKey(x => x.KeycloakId);
        builder.HasIndex(x => x.Email);
    }
}
