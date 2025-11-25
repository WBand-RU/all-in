namespace Shared.Messages;

public record GetSongRequest(string SongId);

public record GetSongResponse(string? BandId, bool Success);

public record CheckBandMembershipRequest(string UserId, string BandId);

public record CheckBandMembershipResponse(bool IsMember);
