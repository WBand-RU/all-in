using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Services;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.SongModule.Application;
using WBand.Modules.SongModule.Contracts;
using WBand.Modules.SongModule.Domain;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.SongModule.Endpoints;

public sealed record CreateSongRequest(
    Guid BandId, string Title, IReadOnlyList<string>? Authors, string? Author,
    string? Key, int? Bpm, IReadOnlyList<TempoChange>? TempoTrack,
    IReadOnlyList<TimeSignatureChange>? TimeSignatureTrack, int CountInBars = 2,
    IReadOnlyList<SongSection>? Sections = null, string? Lyrics = null, string? Chords = null);

public sealed record UpdateSongRequest(
    string Title, IReadOnlyList<string>? Authors, string? Author, string? Key, int? Bpm,
    IReadOnlyList<TempoChange>? TempoTrack, IReadOnlyList<TimeSignatureChange>? TimeSignatureTrack,
    int CountInBars, IReadOnlyList<SongSection>? Sections, string? Lyrics, string? Chords,
    SongStatus Status, long ExpectedContentVersion);

public sealed record SongResponse(
    Guid Id, Guid BandId, string Title, IReadOnlyList<string> Authors, string? Author,
    string? Key, int? Bpm, IReadOnlyList<TempoChange> TempoTrack,
    IReadOnlyList<TimeSignatureChange> TimeSignatureTrack, int CountInBars,
    IReadOnlyList<SongSection> Sections, string? Lyrics, string? Chords, SongStatus Status,
    long ContentVersion, DateTimeOffset CreatedAt, Guid CreatedBy, DateTimeOffset? UpdatedAt,
    Guid? UpdatedBy, DateTimeOffset? DeletedAt, DateTimeOffset? PurgeAfter)
{
    public static SongResponse From(Song song) => new(
        song.Id, song.BandId, song.Title, song.Authors, song.Authors.FirstOrDefault(), song.Key,
        song.Bpm, song.TempoTrack, song.TimeSignatureTrack, song.CountInBars, song.Sections,
        song.Lyrics, song.Chords, song.Status, song.ContentVersion, song.CreatedAt, song.CreatedBy,
        song.UpdatedAt, song.UpdatedBy, song.DeletedAt, song.PurgeAfter);
}

public sealed class CreateSongRequestValidator : AbstractValidator<CreateSongRequest>
{
    public CreateSongRequestValidator() => SongValidation.Configure(this, x => x.Title, x => x.Key,
        x => x.Bpm, x => x.CountInBars, x => x.Sections, x => x.TimeSignatureTrack, x => x.TempoTrack);
}

public sealed class UpdateSongRequestValidator : AbstractValidator<UpdateSongRequest>
{
    public UpdateSongRequestValidator()
    {
        SongValidation.Configure(this, x => x.Title, x => x.Key, x => x.Bpm, x => x.CountInBars,
            x => x.Sections, x => x.TimeSignatureTrack, x => x.TempoTrack);
        RuleFor(x => x.ExpectedContentVersion).GreaterThan(0);
    }
}

internal static class SongValidation
{
    public static void Configure<T>(AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, string?>> key,
        System.Linq.Expressions.Expression<Func<T, int?>> bpm,
        System.Linq.Expressions.Expression<Func<T, int>> countIn,
        System.Linq.Expressions.Expression<Func<T, IReadOnlyList<SongSection>?>> sections,
        System.Linq.Expressions.Expression<Func<T, IReadOnlyList<TimeSignatureChange>?>> signatures,
        System.Linq.Expressions.Expression<Func<T, IReadOnlyList<TempoChange>?>> tempos)
    {
        validator.RuleFor(title).NotEmpty().MaximumLength(200);
        validator.RuleFor(key).Matches("^[CDEFGAB](#|b)?(m|maj|min|dim|aug)?$").When(x => key.Compile()(x) is not null);
        validator.RuleFor(bpm).InclusiveBetween(20, 400).When(x => bpm.Compile()(x).HasValue);
        validator.RuleFor(countIn).InclusiveBetween(0, 8);
        validator.RuleFor(sections).Must(x => x is null || x.All(s =>
            !string.IsNullOrWhiteSpace(s.Name) && s.StartBar > 0 && s.EndBar >= s.StartBar));
        validator.RuleFor(signatures).Must(x => x is null || x.All(s =>
            s.Bar > 0 && s.Beats > 0 && s.BeatUnit is 1 or 2 or 4 or 8 or 16));
        validator.RuleFor(tempos).Must(x => x is null || x.All(t =>
            t.Bar > 0 && t.Bpm is >= 20 and <= 400));
    }
}

internal static class SongAccess
{
    public static Task<bool> Check(IMessageBus bus, Guid bandId, Guid userId, BandPermission permission) =>
        bus.InvokeAsync<bool>(new CheckBandPermission(bandId, userId, permission));

    public static List<string> Authors(IReadOnlyList<string>? authors, string? author) =>
        (authors?.Count > 0 ? authors : string.IsNullOrWhiteSpace(author) ? [] : [author])
        .Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}

public static class CreateSongEndpoint
{
    [Authorize]
    [WolverinePost("/song-api/songs")]
    public static async Task<IResult> Post(CreateSongRequest request, ICurrentUser user,
        IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        if (!await SongAccess.Check(bus, request.BandId, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        var now = DateTimeOffset.UtcNow;
        var song = new Song
        {
            Id = Guid.CreateVersion7(), BandId = request.BandId, Title = request.Title.Trim(),
            Authors = SongAccess.Authors(request.Authors, request.Author), Key = request.Key?.Trim(),
            Bpm = request.Bpm, TempoTrack = request.TempoTrack?.ToList() ?? [],
            TimeSignatureTrack = request.TimeSignatureTrack?.ToList() ?? [new(1, 4, 4)],
            CountInBars = request.CountInBars, Sections = request.Sections?.ToList() ?? [],
            Lyrics = request.Lyrics, Chords = request.Chords, CreatedAt = now, CreatedBy = user.GetUserId,
        };
        session.Store(song);
        session.Store(SongVersioning.Revision(song, user.GetUserId, now));
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new SongCreated(song.Id, song.BandId, song.ContentVersion));
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class ListSongsEndpoint
{
    [Authorize]
    [WolverineGet("/song-api/songs")]
    public static async Task<IResult> Get(Guid? bandId, int page, int pageSize, string? searchTerm,
        ICurrentUser user, IQuerySession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        Guid[] availableBandIds;
        if (bandId.HasValue)
        {
            if (!await SongAccess.Check(bus, bandId.Value, user.GetUserId, BandPermission.View)) return Results.Forbid();
            availableBandIds = [bandId.Value];
        }
        else
        {
            availableBandIds = await bus.InvokeAsync<Guid[]>(new GetUserBandIds(user.GetUserId));
        }

        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = session.Query<Song>().Where(x => availableBandIds.Contains(x.BandId) && x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(searchTerm)) query = query.Where(x => x.Title.Contains(searchTerm));
        var total = await query.LongCountAsync(cancellationToken);
        var songs = await query.OrderBy(x => x.Title).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Results.Ok(ApiResponse<TableResponse<SongResponse>>.Success(new(total, songs.Select(SongResponse.From))));
    }
}

public static class GetSongEndpoint
{
    [Authorize]
    [WolverineGet("/song-api/songs/{songId}")]
    public static async Task<IResult> Get(Guid songId, ICurrentUser user, IQuerySession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await session.LoadAsync<Song>(songId, cancellationToken);
        if (song is null || song.IsDeleted) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.View)) return Results.Forbid();
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class UpdateSongEndpoint
{
    [Authorize]
    [WolverinePut("/song-api/songs/{songId}")]
    public static async Task<IResult> Put(Guid songId, UpdateSongRequest request, ICurrentUser user,
        IDocumentSession session, IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await session.LoadAsync<Song>(songId, cancellationToken);
        if (song is null || song.IsDeleted) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        if (song.ContentVersion != request.ExpectedContentVersion)
            return Results.Conflict(new { code = "song_version_conflict", currentContentVersion = song.ContentVersion });
        song.Title = request.Title.Trim(); song.Authors = SongAccess.Authors(request.Authors, request.Author);
        song.Key = request.Key?.Trim(); song.Bpm = request.Bpm; song.TempoTrack = request.TempoTrack?.ToList() ?? [];
        song.TimeSignatureTrack = request.TimeSignatureTrack?.ToList() ?? []; song.CountInBars = request.CountInBars;
        song.Sections = request.Sections?.ToList() ?? []; song.Lyrics = request.Lyrics; song.Chords = request.Chords;
        song.Status = request.Status; song.ContentVersion++; song.UpdatedAt = DateTimeOffset.UtcNow; song.UpdatedBy = user.GetUserId;
        session.Store(song);
        session.Store(SongVersioning.Revision(song, user.GetUserId, song.UpdatedAt.Value));
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new SongChanged(song.Id, song.BandId, song.ContentVersion));
        return Results.Ok(ApiResponse<SongResponse>.Success(SongResponse.From(song)));
    }
}

public static class DeleteSongEndpoint
{
    [Authorize]
    [WolverineDelete("/song-api/songs/{songId}")]
    public static async Task<IResult> Delete(Guid songId, ICurrentUser user, IDocumentSession session,
        IMessageBus bus, CancellationToken cancellationToken)
    {
        var song = await session.LoadAsync<Song>(songId, cancellationToken);
        if (song is null || song.IsDeleted) return Results.NotFound();
        if (!await SongAccess.Check(bus, song.BandId, user.GetUserId, BandPermission.EditContent)) return Results.Forbid();
        song.DeletedAt = DateTimeOffset.UtcNow; song.DeletedBy = user.GetUserId; song.ContentVersion++;
        song.UpdatedAt = song.DeletedAt; song.UpdatedBy = user.GetUserId;
        session.Store(song);
        session.Store(SongVersioning.Revision(song, user.GetUserId, song.DeletedAt.Value));
        await session.SaveChangesAsync(cancellationToken);
        await bus.PublishAsync(new SongDeleted(song.Id, song.BandId, song.ContentVersion));
        return Results.Ok(ApiResponse.Success());
    }
}
