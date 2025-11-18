using BandService.Domain;

namespace BandService.Endpoints.Bands.Invitations.CreateInvitation;

public sealed record CreateInvitationRequest(
    string InviteeEmail,
    MemberRole Role = MemberRole.Member
);
