using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.Modules.SongModule.Data.Configurations;

/// <summary>Maps SongRevision to PostgreSQL with explicit keys, columns and relationships.</summary>
public sealed class SongRevisionConfiguration : IEntityTypeConfiguration<SongRevision>
{
    public void Configure(EntityTypeBuilder<SongRevision> builder)
    {
        builder.ToTable("song_revisions", SongSchema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SongId).HasColumnName("song_id");
        builder.Property(x => x.ContentVersion).HasColumnName("content_version");
        builder.Property(x => x.Title).HasColumnName("title");
        builder.Property(x => x.Authors).HasColumnName("authors");
        builder.Property(x => x.Key).HasColumnName("key");
        builder.Property(x => x.Bpm).HasColumnName("bpm");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.HasIndex(x => new { x.SongId, x.ContentVersion }).IsUnique();
        builder.Property(x => x.Authors).HasColumnType("text[]");
        builder.HasOne(x => x.Song).WithMany(x => x.Revisions).HasForeignKey(x => x.SongId).OnDelete(DeleteBehavior.Cascade);
    }
}
