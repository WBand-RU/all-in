using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.PlaylistModule.Contracts;
using WBand.Modules.PlaylistModule.Domain;
using WBand.Modules.SongModule.Contracts;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.PlaylistModule.Endpoints;

public sealed record PlaylistItemRequest(
    Guid? Id,
    Guid SongId,
    string? KeyOverride,
    int? BpmOverride,
    IReadOnlyList<PlaylistSectionOverride>? StructureOverride,
    IReadOnlyList<StemMixOverride>? StemMixOverrides,
    PlaylistTransitionType Transition = PlaylistTransitionType.Pause,
    int PauseSeconds = 0,
    int CrossfadeSeconds = 0,
    string? Notes = null);

public sealed record CreatePlaylistRequest(
    Guid BandId,
    string Title,
    string? Description,
    DateTimeOffset? EventDateTime,
    string? Venue,
    IReadOnlyList<PlaylistItemRequest>? Items);

public sealed record UpdatePlaylistRequest(
    string Title,
    string? Description,
    DateTimeOffset? EventDateTime,
    string? Venue,
    IReadOnlyList<PlaylistItemRequest>? Items,
    long ExpectedContentVersion);

public sealed record PlaylistItemResponse(
    Guid Id,
    Guid SongId,
    string SongTitle,
    long SongContentVersion,
    int Order,
    string? KeyOverride,
    int? BpmOverride,
    IReadOnlyList<PlaylistSectionOverride> StructureOverride,
    IReadOnlyList<StemMixOverride> StemMixOverrides,
    PlaylistTransitionType Transition,
    int PauseSeconds,
    int CrossfadeSeconds,
    string? Notes);

public sealed record PlaylistResponse(
    Guid Id,
    Guid BandId,
    string Title,
    string? Description,
    DateTimeOffset? EventDateTime,
    string? Venue,
    IReadOnlyList<PlaylistItemResponse> Items,
    long ContentVersion,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy);

public sealed class CreatePlaylistRequestValidator : AbstractValidator<CreatePlaylistRequest>
{
    public CreatePlaylistRequestValidator() => PlaylistValidation.Configure(
        this, request => request.Title, request => request.Venue, request => request.Items);
}

public sealed class UpdatePlaylistRequestValidator : AbstractValidator<UpdatePlaylistRequest>
{
    public UpdatePlaylistRequestValidator()
    {
        PlaylistValidation.Configure(this, request => request.Title, request => request.Venue, request => request.Items);
        RuleFor(request => request.ExpectedContentVersion).GreaterThan(0);
    }
}

internal static class PlaylistValidation
{
    public static void Configure<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, string?>> venue,
        System.Linq.Expressions.Expression<Func<T, IReadOnlyList<PlaylistItemRequest>?>> items)
    {
        validator.RuleFor(title).NotEmpty().MaximumLength(200);
        validator.RuleFor(venue).MaximumLength(300);
        validator.RuleFor(items).Must(value => value is null || value.All(item =>
            item.SongId != Guid.Empty &&
            item.BpmOverride is null or >= 20 and <= 400 &&
            item.PauseSeconds is >= 0 and <= 3600 &&
            item.CrossfadeSeconds is >= 0 and <= 60 &&
            (item.StructureOverride is null || item.StructureOverride.All(section =>
                !string.IsNullOrWhiteSpace(section.Name) && section.StartBar > 0 && section.EndBar >= section.StartBar)) &&
            (item.StemMixOverrides is null || item.StemMixOverrides.All(mix =>
                mix.Volume is >= 0 and <= 1 && mix.Pan is >= -1 and <= 1))));
    }
}

internal static class PlaylistEndpointSupport
{
    public static Task<bool> Can(IMessageBus bus, Guid bandId, Guid userId, BandPermission permission) =>
        bus.InvokeAsync<bool>(new CheckBandPermission(bandId, userId, permission));

    public static async Task<Dictionary<Guid, SongReference>?> GetSongs(
        IMessageBus bus, Guid bandId, IEnumerable<Guid> songIds)
    {
        var ids = songIds.Distinct().ToArray();
        var songs = await bus.InvokeAsync<SongReference[]>(new GetSongReferences(ids));
        if (songs.Length != ids.Length || songs.Any(song => song.BandId != bandId)) return null;
        return songs.ToDictionary(song => song.SongId);
    }

    public static List<PlaylistItem> MapItems(IEnumerable<PlaylistItemRequest>? items) =>
        (items ?? []).Select((item, index) => new PlaylistItem
        {
            Id = item.Id is { } id && id != Guid.Empty ? id : Guid.CreateVersion7(),
            SongId = item.SongId,
            Order = index + 1,
            KeyOverride = string.IsNullOrWhiteSpace(item.KeyOverride) ? null : item.KeyOverride.Trim(),
            BpmOverride = item.BpmOverride,
            StructureOverride = item.StructureOverride?.ToList() ?? [],
            StemMixOverrides = item.StemMixOverrides?.ToList() ?? [],
            Transition = item.Transition,
            PauseSeconds = item.Transition == PlaylistTransitionType.Pause ? item.PauseSeconds : 0,
            CrossfadeSeconds = item.Transition == PlaylistTransitionType.Crossfade ? item.CrossfadeSeconds : 0,
            Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim(),
        }).ToList();

    public static PlaylistResponse Response(Playlist playlist, IReadOnlyDictionary<Guid, SongReference> songs) => new(
        playlist.Id, playlist.BandId, playlist.Title, playlist.Description, playlist.EventDateTime,
        playlist.Venue, playlist.Items.OrderBy(item => item.Order).Select(item =>
        {
            songs.TryGetValue(item.SongId, out var song);
            return new PlaylistItemResponse(item.Id, item.SongId, song?.Title ?? "", song?.ContentVersion ?? 0,
                item.Order, item.KeyOverride, item.BpmOverride, item.StructureOverride, item.StemMixOverrides,
                item.Transition, item.PauseSeconds, item.CrossfadeSeconds, item.Notes);
        }).ToArray(), playlist.ContentVersion, playlist.CreatedAt, playlist.CreatedBy,
        playlist.UpdatedAt, playlist.UpdatedBy);
}

public static class CreatePlaylistEndpoint
{
    [Authorize]
    [WolverinePost("/playlist-api/playlists")]
    public static async Task<IResult> Post(CreatePlaylistRequest request, ICurrentUser user,
        IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        if (!await PlaylistEndpointSupport.Can(bus, request.BandId, user.GetUserId, BandPermission.EditContent))
            return Results.Forbid();
        var songs = await PlaylistEndpointSupport.GetSongs(bus, request.BandId,
            request.Items?.Select(item => item.SongId) ?? []);
        if (songs is null) return Results.BadRequest(new { code = "playlist_song_outside_band" });

        var playlist = new Playlist
        {
            Id = Guid.CreateVersion7(), BandId = request.BandId, Title = request.Title.Trim(),
            Description = request.Description?.Trim(), EventDateTime = request.EventDateTime,
            Venue = request.Venue?.Trim(), Items = PlaylistEndpointSupport.MapItems(request.Items),
            CreatedAt = DateTimeOffset.UtcNow, CreatedBy = user.GetUserId,
        };
        session.Store(playlist);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new PlaylistCreated(playlist.Id, playlist.BandId, playlist.ContentVersion));
        return Results.Ok(ApiResponse<PlaylistResponse>.Success(PlaylistEndpointSupport.Response(playlist, songs)));
    }
}

public static class ListPlaylistsEndpoint
{
    [Authorize]
    [WolverineGet("/playlist-api/playlists")]
    public static async Task<IResult> Get(Guid? bandId, string? searchTerm, ICurrentUser user,
        IQuerySession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        Guid[] bandIds;
        if (bandId is { } selectedBandId)
        {
            if (!await PlaylistEndpointSupport.Can(bus, selectedBandId, user.GetUserId, BandPermission.View))
                return Results.Forbid();
            bandIds = [selectedBandId];
        }
        else bandIds = await bus.InvokeAsync<Guid[]>(new GetUserBandIds(user.GetUserId));

        var query = session.Query<Playlist>().Where(playlist =>
            bandIds.Contains(playlist.BandId) && playlist.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(playlist => playlist.Title.Contains(searchTerm));
        var playlists = await query.OrderBy(playlist => playlist.EventDateTime).ToListAsync(cancellationToken);
        var songMap = (await bus.InvokeAsync<SongReference[]>(new GetSongReferences(
            playlists.SelectMany(playlist => playlist.Items).Select(item => item.SongId).Distinct().ToArray())))
            .ToDictionary(song => song.SongId);
        return Results.Ok(ApiResponse<IReadOnlyList<PlaylistResponse>>.Success(
            playlists.Select(playlist => PlaylistEndpointSupport.Response(playlist, songMap)).ToArray()));
    }
}

public static class GetPlaylistEndpoint
{
    [Authorize]
    [WolverineGet("/playlist-api/playlists/{playlistId}")]
    public static async Task<IResult> Get(Guid playlistId, ICurrentUser user, IQuerySession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var playlist = await session.LoadAsync<Playlist>(playlistId, cancellationToken);
        if (playlist is null || playlist.IsDeleted) return Results.NotFound();
        if (!await PlaylistEndpointSupport.Can(bus, playlist.BandId, user.GetUserId, BandPermission.View))
            return Results.Forbid();
        var songs = (await bus.InvokeAsync<SongReference[]>(new GetSongReferences(
            playlist.Items.Select(item => item.SongId).ToArray()))).ToDictionary(song => song.SongId);
        return Results.Ok(ApiResponse<PlaylistResponse>.Success(PlaylistEndpointSupport.Response(playlist, songs)));
    }
}

public static class UpdatePlaylistEndpoint
{
    [Authorize]
    [WolverinePut("/playlist-api/playlists/{playlistId}")]
    public static async Task<IResult> Put(Guid playlistId, UpdatePlaylistRequest request, ICurrentUser user,
        IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var playlist = await session.LoadAsync<Playlist>(playlistId, cancellationToken);
        if (playlist is null || playlist.IsDeleted) return Results.NotFound();
        if (!await PlaylistEndpointSupport.Can(bus, playlist.BandId, user.GetUserId, BandPermission.EditContent))
            return Results.Forbid();
        if (playlist.ContentVersion != request.ExpectedContentVersion)
            return Results.Conflict(new { code = "playlist_version_conflict", currentContentVersion = playlist.ContentVersion });
        var songs = await PlaylistEndpointSupport.GetSongs(bus, playlist.BandId,
            request.Items?.Select(item => item.SongId) ?? []);
        if (songs is null) return Results.BadRequest(new { code = "playlist_song_outside_band" });

        playlist.Title = request.Title.Trim(); playlist.Description = request.Description?.Trim();
        playlist.EventDateTime = request.EventDateTime; playlist.Venue = request.Venue?.Trim();
        playlist.Items = PlaylistEndpointSupport.MapItems(request.Items); playlist.ContentVersion++;
        playlist.UpdatedAt = DateTimeOffset.UtcNow; playlist.UpdatedBy = user.GetUserId;
        session.Store(playlist);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new PlaylistChanged(playlist.Id, playlist.BandId, playlist.ContentVersion));
        return Results.Ok(ApiResponse<PlaylistResponse>.Success(PlaylistEndpointSupport.Response(playlist, songs)));
    }
}

public static class DeletePlaylistEndpoint
{
    [Authorize]
    [WolverineDelete("/playlist-api/playlists/{playlistId}")]
    public static async Task<IResult> Delete(Guid playlistId, ICurrentUser user, IDocumentSession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var playlist = await session.LoadAsync<Playlist>(playlistId, cancellationToken);
        if (playlist is null || playlist.IsDeleted) return Results.NotFound();
        if (!await PlaylistEndpointSupport.Can(bus, playlist.BandId, user.GetUserId, BandPermission.EditContent))
            return Results.Forbid();
        playlist.DeletedAt = DateTimeOffset.UtcNow; playlist.DeletedBy = user.GetUserId;
        playlist.UpdatedAt = playlist.DeletedAt; playlist.UpdatedBy = user.GetUserId; playlist.ContentVersion++;
        session.Store(playlist);
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new PlaylistDeleted(playlist.Id, playlist.BandId, playlist.ContentVersion));
        return Results.Ok(ApiResponse.Success());
    }
}
