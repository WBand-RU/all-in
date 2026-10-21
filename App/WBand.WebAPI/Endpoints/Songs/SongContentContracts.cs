using FluentValidation;
using WBand.WebAPI.Data;

namespace WBand.WebAPI.Endpoints.Songs;

/// <summary>A song's shared sections, partitions and populated intersection cells.</summary>
public sealed record SongContentResponse(IReadOnlyList<SongSectionResponse> Sections,
    IReadOnlyList<PartitionResponse> Partitions, IReadOnlyList<SongContentCellResponse> Cells);

/// <summary>Text for one section/partition pair. Missing cells are empty and have revision zero.</summary>
public sealed record SongContentCellResponse(Guid SectionId, Guid PartitionId, string Content, long Revision)
{
    /// <summary>Maps a stored matrix cell.</summary>
    public static SongContentCellResponse From(SongContentCell cell) => new(cell.SectionId, cell.PartitionId, cell.Content, cell.Revision);
}

/// <summary>Replacement cell content; use revision zero when creating a previously empty cell.</summary>
public sealed record UpdateSongContentCellRequest(string Content, long ExpectedRevision);

public sealed class UpdateSongContentCellRequestValidator : AbstractValidator<UpdateSongContentCellRequest>
{
    public UpdateSongContentCellRequestValidator()
    {
        RuleFor(x => x.Content).NotNull().MaximumLength(50_000);
        RuleFor(x => x.ExpectedRevision).GreaterThanOrEqualTo(0);
    }
}

/// <summary>One section's draft within an atomic partition content update.</summary>
public sealed record PartitionSectionUpdate(Guid SectionId, string Content, long ExpectedRevision);

/// <summary>Saves the supplied sections of one partition together, or rejects the entire update.</summary>
public sealed record UpdatePartitionContentRequest(IReadOnlyList<PartitionSectionUpdate> Cells);

public sealed class UpdatePartitionContentRequestValidator : AbstractValidator<UpdatePartitionContentRequest>
{
    public UpdatePartitionContentRequestValidator()
    {
        RuleFor(x => x.Cells).NotEmpty();
        RuleFor(x => x.Cells).Must(cells => cells is null || cells.Where(x => x is not null).Select(x => x.SectionId).Distinct().Count() == cells.Count)
            .WithMessage("Each section can only occur once.");
        RuleForEach(x => x.Cells).NotNull().ChildRules(cell =>
        {
            cell.RuleFor(x => x.SectionId).NotEmpty();
            cell.RuleFor(x => x.Content).NotNull().MaximumLength(50_000);
            cell.RuleFor(x => x.ExpectedRevision).GreaterThanOrEqualTo(0);
        });
    }
}
