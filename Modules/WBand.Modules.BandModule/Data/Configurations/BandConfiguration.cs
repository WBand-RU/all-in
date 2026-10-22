using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.Modules.BandModule.Data.Configurations;

/// <summary>Maps Band to PostgreSQL with explicit keys, columns and relationships.</summary>
public sealed class BandConfiguration : IEntityTypeConfiguration<Band>
{
    public void Configure(EntityTypeBuilder<Band> builder)
    {
        builder.ToTable("bands", BandSchema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Name).HasMaxLength(120);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy)
            .HasPrincipalKey(x => x.KeycloakId).OnDelete(DeleteBehavior.Restrict);
    }
}
