using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.Modules.SongModule.Data.Configurations;

/// <summary>Maps Song to PostgreSQL with explicit keys, columns and relationships.</summary>
public sealed class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.ToTable("songs", SongSchema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.BandId).HasColumnName("band_id");
        builder.Property(x => x.Title).HasColumnName("title");
        builder.Property(x => x.Authors).HasColumnName("authors");
        builder.Property(x => x.Key).HasColumnName("key");
        builder.Property(x => x.Bpm).HasColumnName("bpm");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(x => x.ContentVersion).HasColumnName("content_version");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.Ignore(x => x.IsDeleted);
        builder.Ignore(x => x.PurgeAfter);
        builder.HasIndex(x => new { x.BandId, x.Title });
        builder.Property(x => x.Authors).HasColumnType("text[]");
    }
}
