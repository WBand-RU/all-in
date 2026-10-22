namespace WBand.Modules.BandModule.Contracts;

/// <summary>Retrieves profile and current band role for historical contributor subjects.</summary>
public sealed record GetBandContributorInfo(Guid BandId, Guid[] UserIds);

/// <summary>Identifies a contributor even when their active membership has ended.</summary>
public sealed record BandContributorInfo(Guid UserId, string Email, string? Role);
