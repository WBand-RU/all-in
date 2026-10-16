using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.WebAPI.Data.Configurations;

/// <summary>Maps BandMember to PostgreSQL with explicit keys, columns and relationships.</summary>
public sealed class BandMemberConfiguration : IEntityTypeConfiguration<BandMember>
{
    public void Configure(EntityTypeBuilder<BandMember> builder)
    {
        builder.ToTable("band_members", "public");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.BandId).HasColumnName("band_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<int>();
        builder.Property(x => x.JoinedAt).HasColumnName("joined_at");
        builder.HasIndex(x => new { x.BandId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.BandId, x.Email }).IsUnique();
        builder.HasIndex(x => x.BandId).IsUnique().HasFilter("role = 0");
        builder.HasOne(x => x.User).WithMany(x => x.BandMemberships).HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.KeycloakId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Band).WithMany(x => x.Members).HasForeignKey(x => x.BandId).OnDelete(DeleteBehavior.Cascade);
    }
}
