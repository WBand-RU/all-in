using BandService.Domain;

namespace BandService.Endpoints.User.Invitations.GetMyInvitations;

public sealed record GetMyInvitationsResponse(
    string Id,
    string InviteeEmail,
    string BandId,
    string BandName,
    string InviterEmail,
    MemberRole Role,
    InvitationStatus Status
);
