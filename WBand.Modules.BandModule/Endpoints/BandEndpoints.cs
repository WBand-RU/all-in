using FluentValidation;
using Marten;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared;
using Shared.Services;
using WBand.Modules.BandModule.Application;
using WBand.Modules.BandModule.Contracts;
using WBand.Modules.BandModule.Domain;
using Wolverine;
using Wolverine.Http;

namespace WBand.Modules.BandModule.Endpoints;

public sealed record CreateBandRequest(string Name);
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
        IDocumentSession session,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var band = new Band
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name.Trim(),
            CreatedBy = currentUser.GetUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var member = new BandMember
        {
            Id = Guid.CreateVersion7(),
            BandId = band.Id,
            UserId = currentUser.GetUserId,
            Email = currentUser.GetUserEmail.Trim().ToLowerInvariant(),
            Role = BandMemberRole.Owner,
            JoinedAt = DateTimeOffset.UtcNow,
        };

        session.Store(band);
        session.Store(member);
        await session.SaveChangesAsync(cancellationToken);
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
        IQuerySession session,
        CancellationToken cancellationToken
    )
    {
        var memberships = await session.Query<BandMember>()
            .Where(x => x.UserId == currentUser.GetUserId)
            .ToListAsync(cancellationToken);
        var bands = new List<Band>(memberships.Count);
        foreach (var membership in memberships)
        {
            var band = await session.LoadAsync<Band>(membership.BandId, cancellationToken);
            if (band is not null) bands.Add(band);
        }
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
        IQuerySession session,
        CancellationToken cancellationToken
    )
    {
        if (await BandAccess.FindMembership(session, bandId, currentUser.GetUserId, cancellationToken) is null)
            return Results.Forbid();
        var band = await session.LoadAsync<Band>(bandId, cancellationToken);
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
        IDocumentSession session,
        CancellationToken cancellationToken
    )
    {
        if (!await BandAccess.IsOwner(session, bandId, currentUser.GetUserId, cancellationToken))
            return Results.Forbid();
        var band = await session.LoadAsync<Band>(bandId, cancellationToken);
        if (band is null) return Results.NotFound();
        band.Name = request.Name.Trim();
        band.UpdatedAt = DateTimeOffset.UtcNow;
        session.Store(band);
        await session.SaveChangesAsync(cancellationToken);
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
        IDocumentSession session,
        CancellationToken cancellationToken
    )
    {
        if (!await BandAccess.IsOwner(session, bandId, currentUser.GetUserId, cancellationToken))
            return Results.Forbid();
        session.Delete<Band>(bandId);
        foreach (var member in await session.Query<BandMember>().Where(x => x.BandId == bandId).ToListAsync(cancellationToken))
            session.Delete(member);
        foreach (var invitation in await session.Query<BandInvitation>().Where(x => x.BandId == bandId).ToListAsync(cancellationToken))
            session.Delete(invitation);
        await session.SaveChangesAsync(cancellationToken);
        return Results.Ok(ApiResponse.Success());
    }
}
