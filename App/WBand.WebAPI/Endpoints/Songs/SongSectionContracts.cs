using FluentValidation;
using WBand.WebAPI.Data;

namespace WBand.WebAPI.Endpoints.Songs;

/// <summary>A named section with plain text lyrics and chords, without bar-based structure.</summary>
public sealed record CreateSongSectionRequest(string Name, int Order, string? Lyrics = null, string? Chords = null);

/// <summary>Replacement section text with an optimistic concurrency revision.</summary>
public sealed record UpdateSongSectionRequest(string Name, int Order, string? Lyrics, string? Chords, long ExpectedRevision);

/// <summary>The text and display order of one song section.</summary>
public sealed record SongSectionResponse(Guid Id, Guid SongId, string Name, int Order, string? Lyrics,
    string? Chords, long Revision, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)
{
    public static SongSectionResponse From(SongSection section) => new(section.Id, section.SongId,
        section.Name, section.Order, section.Lyrics, section.Chords, section.Revision, section.CreatedAt, section.UpdatedAt);
}

public sealed class CreateSongSectionRequestValidator : AbstractValidator<CreateSongSectionRequest>
{
    public CreateSongSectionRequestValidator() => Configure(this, x => x.Name, x => x.Order, x => x.Lyrics, x => x.Chords);

    internal static void Configure<T>(AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> name,
        System.Linq.Expressions.Expression<Func<T, int>> order,
        System.Linq.Expressions.Expression<Func<T, string?>> lyrics,
        System.Linq.Expressions.Expression<Func<T, string?>> chords)
    {
        validator.RuleFor(name).NotEmpty().MaximumLength(100);
        validator.RuleFor(order).GreaterThanOrEqualTo(0);
        validator.RuleFor(lyrics).MaximumLength(50_000);
        validator.RuleFor(chords).MaximumLength(50_000);
    }
}

public sealed class UpdateSongSectionRequestValidator : AbstractValidator<UpdateSongSectionRequest>
{
    public UpdateSongSectionRequestValidator()
    {
        CreateSongSectionRequestValidator.Configure(this, x => x.Name, x => x.Order, x => x.Lyrics, x => x.Chords);
        RuleFor(x => x.ExpectedRevision).GreaterThan(0);
    }
}
