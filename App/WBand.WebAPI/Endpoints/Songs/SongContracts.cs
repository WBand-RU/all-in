using FluentValidation;
using WBand.WebAPI.Data;

namespace WBand.WebAPI.Endpoints.Songs;

/// <summary>Song metadata without musical structure or playback settings.</summary>
public sealed record CreateSongRequest(Guid BandId, string Title, IReadOnlyList<string>? Authors = null,
    string? Author = null, string? Key = null, int? Bpm = null);

/// <summary>Replacement metadata with an optimistic concurrency version.</summary>
public sealed record UpdateSongRequest(string Title, IReadOnlyList<string>? Authors, string? Author,
    string? Key, int? Bpm, SongStatus Status, long ExpectedContentVersion);

/// <summary>Current metadata and lifecycle of a song. Sections are retrieved separately.</summary>
public sealed record SongResponse(Guid Id, Guid BandId, string Title, IReadOnlyList<string> Authors,
    string? Author, string? Key, int? Bpm, SongStatus Status, long ContentVersion,
    DateTimeOffset CreatedAt, Guid CreatedBy, DateTimeOffset? UpdatedAt, Guid? UpdatedBy,
    DateTimeOffset? DeletedAt, DateTimeOffset? PurgeAfter)
{
    public static SongResponse From(Song song) => new(song.Id, song.BandId, song.Title, song.Authors,
        song.Authors.FirstOrDefault(), song.Key, song.Bpm, song.Status, song.ContentVersion,
        song.CreatedAt, song.CreatedBy, song.UpdatedAt, song.UpdatedBy, song.DeletedAt, song.PurgeAfter);
}

public sealed class CreateSongRequestValidator : AbstractValidator<CreateSongRequest>
{
    public CreateSongRequestValidator()
    {
        RuleFor(x => x.BandId).NotEmpty();
        SongValidation.Configure(this, x => x.Title, x => x.Authors, x => x.Author, x => x.Key, x => x.Bpm);
    }
}

public sealed class UpdateSongRequestValidator : AbstractValidator<UpdateSongRequest>
{
    public UpdateSongRequestValidator()
    {
        SongValidation.Configure(this, x => x.Title, x => x.Authors, x => x.Author, x => x.Key, x => x.Bpm);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.ExpectedContentVersion).GreaterThan(0);
    }
}

internal static class SongValidation
{
    public static void Configure<T>(AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> title,
        System.Linq.Expressions.Expression<Func<T, IReadOnlyList<string>?>> authors,
        System.Linq.Expressions.Expression<Func<T, string?>> author,
        System.Linq.Expressions.Expression<Func<T, string?>> key,
        System.Linq.Expressions.Expression<Func<T, int?>> bpm)
    {
        validator.RuleFor(title).NotEmpty().MaximumLength(200);
        validator.RuleFor(authors).Must(x => x is null || (x.Count <= 20 && x.All(a => !string.IsNullOrWhiteSpace(a) && a.Length <= 200)));
        validator.RuleFor(author).MaximumLength(200);
        validator.RuleFor(key).Matches("^[CDEFGAB](#|b)?(m|maj|min|dim|aug)?$");
        validator.RuleFor(bpm).InclusiveBetween(20, 400);
    }
}
