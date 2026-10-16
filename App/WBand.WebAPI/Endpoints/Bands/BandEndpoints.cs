using FluentValidation;
using WBand.WebAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using WBand.WebAPI.Shared;
using WBand.WebAPI.Services;
using WBand.WebAPI.Application.Bands;
using WBand.Modules.BandModule.Contracts;
using Wolverine;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints.Bands;

/// <summary>The name of a new band.</summary>
public sealed record CreateBandRequest(string Name);

/// <summary>The replacement name of an existing band.</summary>
public sealed record UpdateBandRequest(string Name);

public sealed class CreateBandRequestValidator : AbstractValidator<CreateBandRequest>
{
    public CreateBandRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
}

public sealed class UpdateBandRequestValidator : AbstractValidator<UpdateBandRequest>
{
    public UpdateBandRequestValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
}

public static class CreateBandEndpoint
{
    [Authorize]
    [WolverinePost("/band-api/bands")]
    public static async Task<ApiResponse<Band>> Post(
        CreateBandRequest request,
        ICurrentUser currentUser,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        [Microsoft.AspNetCore.Mvc.FromServices] IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        var band = new Band
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name.Trim(),
            CreatedBy = currentUser.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var member = new BandMember
        {
            Id = Guid.CreateVersion7(),
            BandId = band.Id,
            UserId = currentUser.Id,
            Email = currentUser.Email.Trim().ToLowerInvariant(),
            Role = BandMemberRole.Owner,
            JoinedAt = DateTimeOffset.UtcNow,
        };

        await db.InsertAsync(band, cancellationToken);
        await db.InsertAsync(member, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        await bus.PublishAsync(new BandCreated(band.Id, band.CreatedBy, band.Name));
        return ApiResponse<Band>.Success(band);
    }
}

public static class ListBandsEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/bands")]
    public static async Task<ApiResponse<IReadOnlyList<Band>>> Get(
        ICurrentUser currentUser,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken cancellationToken
    )
    {
        var bands = await db.QueryAsync<Band>("select b.* from public.bands b where exists (select 1 from public.band_members m where m.band_id = b.id and m.user_id = @id)", new { id = currentUser.Id }, cancellationToken);
        return ApiResponse<IReadOnlyList<Band>>.Success(bands);
    }
}

public static class GetBandEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/bands/{bandId}")]
    public static async Task<IResult> Get(
        Guid bandId,
        ICurrentUser currentUser,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken cancellationToken
    )
    {
        if (
            await BandAccess.FindMembership(db, bandId, currentUser.Id, cancellationToken)
            is null
        )
            return Results.Forbid();
        var band = await db.FindAsync<Band>(bandId, cancellationToken);
        return band is null
            ? Results.NotFound(ApiResponse<Band>.Error(ApiCodes.NotFound))
            : Results.Ok(ApiResponse<Band>.Success(band));
    }
}

public static class UpdateBandEndpoint
{
    [Authorize]
    [WolverinePut("/band-api/bands/{bandId}")]
    public static async Task<IResult> Put(
        Guid bandId,
        UpdateBandRequest request,
        ICurrentUser currentUser,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        if (!await BandAccess.IsOwner(db, bandId, currentUser.Id, cancellationToken))
            return Results.Forbid();
        var band = await db.FindAsync<Band>(bandId, cancellationToken);
        if (band is null)
            return Results.NotFound();
        band.Name = request.Name.Trim();
        band.UpdatedAt = DateTimeOffset.UtcNow;
        await db.UpdateAsync(band, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(ApiResponse.Success());
    }
}

public static class DeleteBandEndpoint
{
    [Authorize]
    [WolverineDelete("/band-api/bands/{bandId}")]
    public static async Task<IResult> Delete(
        Guid bandId,
        ICurrentUser currentUser,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await db.BeginTransactionAsync(cancellationToken);
        if (!await BandAccess.IsOwner(db, bandId, currentUser.Id, cancellationToken))
            return Results.Forbid();
        var band = await db.FindAsync<Band>(bandId, cancellationToken) ?? throw new InvalidOperationException("Band is missing.");
        await db.DeleteAsync<Band>(band.Id, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(ApiResponse.Success());
    }
}
