using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.Modules.SongModule.Data.Configurations;

/// <summary>Maps SongSection to PostgreSQL with explicit keys, columns and relationships.</summary>
public sealed class SongSectionConfiguration : IEntityTypeConfiguration<SongSection>
{
    public void Configure(EntityTypeBuilder<SongSection> builder)
    {
        builder.ToTable("song_sections", SongSchema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SongId).HasColumnName("song_id");
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.Order).HasColumnName("order");
        builder.Property(x => x.Revision).HasColumnName("revision");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(x => new { x.SongId, x.Order });
        builder.HasAlternateKey(x => new { x.SongId, x.Id });
        builder.HasOne(x => x.Song).WithMany(x => x.Sections).HasForeignKey(x => x.SongId).OnDelete(DeleteBehavior.Cascade);
    }
}
