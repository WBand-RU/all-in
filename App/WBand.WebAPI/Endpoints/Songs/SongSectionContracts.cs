using FluentValidation;
using WBand.WebAPI.Data;

namespace WBand.WebAPI.Endpoints.Songs;

/// <summary>A named, ordered row in the song content matrix.</summary>
public sealed record CreateSongSectionRequest(string Name, int Order);

/// <summary>Replacement section metadata with optimistic concurrency.</summary>
public sealed record UpdateSongSectionRequest(string Name, int Order, long ExpectedRevision);

/// <summary>A song-wide section; its text is stored in section/partition cells.</summary>
public sealed record SongSectionResponse(Guid Id, Guid SongId, string Name, int Order,
    long Revision, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)
{
    /// <summary>Maps a stored section to its HTTP representation.</summary>
    public static SongSectionResponse From(SongSection section) => new(section.Id, section.SongId,
        section.Name, section.Order, section.Revision, section.CreatedAt, section.UpdatedAt);
}

public sealed class CreateSongSectionRequestValidator : AbstractValidator<CreateSongSectionRequest>
{
    public CreateSongSectionRequestValidator() => Configure(this, x => x.Name, x => x.Order);

    internal static void Configure<T>(AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> name,
        System.Linq.Expressions.Expression<Func<T, int>> order)
    {
        validator.RuleFor(name).NotEmpty().MaximumLength(100);
        validator.RuleFor(order).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateSongSectionRequestValidator : AbstractValidator<UpdateSongSectionRequest>
{
    public UpdateSongSectionRequestValidator()
    {
        CreateSongSectionRequestValidator.Configure(this, x => x.Name, x => x.Order);
        RuleFor(x => x.ExpectedRevision).GreaterThan(0);
    }
}
