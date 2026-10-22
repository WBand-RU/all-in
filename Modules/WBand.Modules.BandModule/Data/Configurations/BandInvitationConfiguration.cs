using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WBand.Modules.BandModule.Data.Configurations;

/// <summary>Maps BandInvitation to PostgreSQL with explicit keys, columns and relationships.</summary>
public sealed class BandInvitationConfiguration : IEntityTypeConfiguration<BandInvitation>
{
    public void Configure(EntityTypeBuilder<BandInvitation> builder)
    {
        builder.ToTable("band_invitations", BandSchema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.BandId).HasColumnName("band_id");
        builder.Property(x => x.InviterId).HasColumnName("inviter_id");
        builder.Property(x => x.InviterEmail).HasColumnName("inviter_email");
        builder.Property(x => x.InviteeEmail).HasColumnName("invitee_email");
        builder.Property(x => x.Role).HasColumnName("role").HasConversion<int>();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.RespondedAt).HasColumnName("responded_at");
        builder.HasIndex(x => new { x.BandId, x.InviteeEmail }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.InviterId)
            .HasPrincipalKey(x => x.KeycloakId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Band>().WithMany().HasForeignKey(x => x.BandId).OnDelete(DeleteBehavior.Cascade);
    }
}
