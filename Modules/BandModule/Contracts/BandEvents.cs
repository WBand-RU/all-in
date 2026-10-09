namespace WBand.Modules.BandModule.Contracts;

public sealed record BandCreated(Guid BandId, Guid OwnerId, string Name);
public sealed record MemberInvited(Guid InvitationId, Guid BandId, string InviteeEmail);
public sealed record MembershipChanged(Guid BandId, Guid UserId, bool IsMember);
