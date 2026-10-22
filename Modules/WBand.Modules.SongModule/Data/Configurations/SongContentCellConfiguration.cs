using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.Modules.SongModule.Data.Configurations;

/// <summary>Enforces one cell per section/partition pair and prevents cross-song references.</summary>
public sealed class SongContentCellConfiguration : IEntityTypeConfiguration<SongContentCell>
{
    public void Configure(EntityTypeBuilder<SongContentCell> builder)
    {
        builder.ToTable("song_content_cells", SongSchema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SongId).HasColumnName("song_id");
        builder.Property(x => x.SectionId).HasColumnName("section_id");
        builder.Property(x => x.PartitionId).HasColumnName("partition_id");
        builder.Property(x => x.Content).HasColumnName("content");
        builder.Property(x => x.Revision).HasColumnName("revision");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.HasIndex(x => new { x.SectionId, x.PartitionId }).IsUnique();
        builder.HasOne(x => x.Section).WithMany(x => x.Cells)
            .HasForeignKey(x => new { x.SongId, x.SectionId }).HasPrincipalKey(x => new { x.SongId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Partition).WithMany(x => x.Cells)
            .HasForeignKey(x => new { x.SongId, x.PartitionId }).HasPrincipalKey(x => new { x.SongId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
