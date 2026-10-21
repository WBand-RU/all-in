using FluentValidation;
using WBand.WebAPI.Data;

namespace WBand.WebAPI.Endpoints.Songs;

/// <summary>A named, ordered column in the song content matrix.</summary>
public sealed record CreatePartitionRequest(string Name, int Order);

/// <summary>Replacement partition metadata with optimistic concurrency.</summary>
public sealed record UpdatePartitionRequest(string Name, int Order, long ExpectedRevision);

/// <summary>A song-wide partition; its text is stored in section/partition cells.</summary>
public sealed record PartitionResponse(Guid Id, Guid SongId, string Name, int Order,
    long Revision, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)
{
    /// <summary>Maps a stored partition to its HTTP representation.</summary>
    public static PartitionResponse From(Partition partition) => new(partition.Id, partition.SongId,
        partition.Name, partition.Order, partition.Revision, partition.CreatedAt, partition.UpdatedAt);
}

public sealed class CreatePartitionRequestValidator : AbstractValidator<CreatePartitionRequest>
{
    public CreatePartitionRequestValidator() => Configure(this, x => x.Name, x => x.Order);

    internal static void Configure<T>(AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string>> name,
        System.Linq.Expressions.Expression<Func<T, int>> order)
    {
        validator.RuleFor(name).NotEmpty().MaximumLength(100);
        validator.RuleFor(order).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePartitionRequestValidator : AbstractValidator<UpdatePartitionRequest>
{
    public UpdatePartitionRequestValidator()
    {
        CreatePartitionRequestValidator.Configure(this, x => x.Name, x => x.Order);
        RuleFor(x => x.ExpectedRevision).GreaterThan(0);
    }
}
