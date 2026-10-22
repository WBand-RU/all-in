using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.Modules.BandModule.Data.Configurations;

/// <summary>Maps BandDeparture to PostgreSQL with explicit keys, columns and relationships.</summary>
public sealed class BandDepartureConfiguration : IEntityTypeConfiguration<BandDeparture>
{
    public void Configure(EntityTypeBuilder<BandDeparture> builder)
    {
        builder.ToTable("band_departures", BandSchema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.BandId).HasColumnName("band_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.Reason).HasColumnName("reason").HasConversion<int>();
        builder.Property(x => x.LeftAt).HasColumnName("left_at");
        builder.Property(x => x.RejoinedAt).HasColumnName("rejoined_at");
        builder.HasIndex(x => new { x.BandId, x.UserId });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.KeycloakId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Band>().WithMany().HasForeignKey(x => x.BandId).OnDelete(DeleteBehavior.Cascade);
    }
}
