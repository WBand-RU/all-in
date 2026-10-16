namespace WBand.Modules.BandModule.Contracts;

/// <summary>Announces a new band after its owner membership is persisted.</summary>
public sealed record BandCreated(Guid BandId, Guid OwnerId, string Name);
/// <summary>Announces a newly created invitation.</summary>
public sealed record MemberInvited(Guid InvitationId, Guid BandId, string InviteeEmail);
/// <summary>Announces membership changes using the user's Keycloak subject.</summary>
public sealed record MembershipChanged(Guid BandId, Guid UserId, bool IsMember);
