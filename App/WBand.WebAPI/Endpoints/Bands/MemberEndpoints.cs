using Microsoft.AspNetCore.Mvc;
using WBand.WebAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using WBand.WebAPI.Shared;
using WBand.WebAPI.Services;
using WBand.WebAPI.Application.Bands;
using Wolverine.Http;

namespace WBand.WebAPI.Endpoints.Bands;

/// <summary>Changes a non-owner membership role.</summary>
public sealed record UpdateMemberRoleRequest(BandMemberRole NewRole);

/// <summary>Reports the current user role and content permissions.</summary>
public sealed record MyBandAccessResponse(Guid BandId, BandMemberRole Role, bool CanEditContent);

public static class MyBandAccessEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/user/band-access", OperationId = "GetMyBandAccess")]
    public static async Task<ApiResponse<IReadOnlyList<MyBandAccessResponse>>> Get(
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        var memberships = await db.QueryAsync<BandMember>("select * from public.band_members where user_id = @id", new { id = user.Id }, ct);
        var access = memberships
            .GroupBy(x => x.BandId)
            .Select(group => group.OrderByDescending(x => x.JoinedAt).First())
            .Select(member => new MyBandAccessResponse(
                member.BandId,
                member.Role,
                member.Role is BandMemberRole.Owner or BandMemberRole.Admin
            ))
            .ToList();
        return ApiResponse<IReadOnlyList<MyBandAccessResponse>>.Success(access);
    }
}

public static class MembersEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/bands/{bandId}/members", OperationId = "GetMembers")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BandMember>>), StatusCodes.Status200OK)]
    public static async Task<IResult> Get(
        Guid bandId,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        if (await BandAccess.FindMembership(db, bandId, user.Id, ct) is null)
            return Results.Forbid();
        var members = await db.QueryAsync<BandMember>("select * from public.band_members where band_id = @bandId", new { bandId }, ct);
        return Results.Ok(ApiResponse<IReadOnlyList<BandMember>>.Success(members));
    }
}

public static class UpdateMemberRoleEndpoint
{
    [Authorize]
    [WolverinePut("/band-api/bands/{bandId}/members/{memberId}/role", OperationId = "UpdateMemberRole")]
    [ProducesResponseType(typeof(ApiResponse<BandMember>), StatusCodes.Status200OK)]
    public static async Task<IResult> Put(
        Guid bandId,
        Guid memberId,
        UpdateMemberRoleRequest request,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        await using var transaction = await db.BeginTransactionAsync(ct);
        if (!await BandAccess.IsOwner(db, bandId, user.Id, ct))
            return Results.Forbid();
        if (request.NewRole is not (BandMemberRole.Admin or BandMemberRole.Member))
            return Results.BadRequest(ApiResponse<BandMember>.Error(ApiCodes.ValidationFailed));

        var member = await db.FindAsync<BandMember>(memberId, ct);
        if (member is null || member.BandId != bandId)
            return Results.NotFound();
        if (member.Role == BandMemberRole.Owner)
            return Results.Forbid();

        member.Role = request.NewRole;
        await db.UpdateAsync(member, ct);

        await transaction.CommitAsync(ct);
        return Results.Ok(ApiResponse<BandMember>.Success(member));
    }
}

public static class RemoveMemberEndpoint
{
    [Authorize]
    [WolverineDelete("/band-api/bands/{bandId}/members/{memberId}", OperationId = "RemoveMember")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public static async Task<IResult> Delete(
        Guid bandId,
        Guid memberId,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        await using var transaction = await db.BeginTransactionAsync(ct);
        if (!await BandAccess.IsOwner(db, bandId, user.Id, ct))
            return Results.Forbid();
        var member = await db.FindAsync<BandMember>(memberId, ct);
        if (member is null || member.BandId != bandId)
            return Results.NotFound();
        if (member.Role == BandMemberRole.Owner)
            return Results.Forbid();
        await db.InsertAsync(
            new BandDeparture
            {
                Id = Guid.CreateVersion7(),
                BandId = bandId,
                UserId = member.UserId,
                Email = member.Email,
                Reason = BandDepartureReason.RemovedByOwner,
                LeftAt = DateTimeOffset.UtcNow,
            }, ct);
        await db.DeleteAsync<BandMember>(member.Id, ct);

        await transaction.CommitAsync(ct);
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}

public static class LeaveBandEndpoint
{
    [Authorize]
    [WolverineDelete("/band-api/bands/{bandId}/membership", OperationId = "LeaveBand")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public static async Task<IResult> Delete(
        Guid bandId,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        await using var transaction = await db.BeginTransactionAsync(ct);
        var member = await BandAccess.FindMembership(db, bandId, user.Id, ct);
        if (member is null)
            return Results.NotFound();
        if (member.Role == BandMemberRole.Owner)
            return Results.Conflict(new { code = "owner_cannot_leave" });

        await db.InsertAsync(
            new BandDeparture
            {
                Id = Guid.CreateVersion7(),
                BandId = bandId,
                UserId = member.UserId,
                Email = member.Email,
                Reason = BandDepartureReason.Voluntary,
                LeftAt = DateTimeOffset.UtcNow,
            }, ct);
        await db.DeleteAsync<BandMember>(member.Id, ct);

        await transaction.CommitAsync(ct);
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}

/// <summary>A previously joined band eligible for voluntary return.</summary>
public sealed record FormerBandResponse(Guid BandId, string BandName, DateTimeOffset LeftAt);

public static class FormerBandsEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/user/former-bands", OperationId = "GetFormerBands")]
    public static async Task<ApiResponse<IReadOnlyList<FormerBandResponse>>> Get(
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        var departures = await db.QueryAsync<BandDeparture>("select * from public.band_departures where user_id = @id and reason = 0 and rejoined_at is null", new { id = user.Id }, ct);
        var result = new List<FormerBandResponse>();
        foreach (
            var departure in departures
                .GroupBy(x => x.BandId)
                .Select(x => x.OrderByDescending(y => y.LeftAt).First())
        )
        {
            var band = await db.FindAsync<Band>(departure.BandId, ct);
            if (band is not null)
                result.Add(new(band.Id, band.Name, departure.LeftAt));
        }

        // Compatibility for memberships left before departure reasons were introduced.
        var email = user.Email.Trim().ToLowerInvariant();
        var acceptedInvitations = await db.QueryAsync<BandInvitation>("select * from public.band_invitations where invitee_email = @email and status = 1", new { email }, ct);
        var activeMemberships = await db.QueryAsync<BandMember>("select * from public.band_members where user_id = @id", new { id = user.Id }, ct);
        var allDepartures = await db.QueryAsync<BandDeparture>("select * from public.band_departures where user_id = @id", new { id = user.Id }, ct);
        foreach (var invitation in acceptedInvitations)
        {
            if (
                activeMemberships.Any(x => x.BandId == invitation.BandId)
                || result.Any(x => x.BandId == invitation.BandId)
            )
                continue;
            var latestDeparture = allDepartures
                .Where(x => x.BandId == invitation.BandId)
                .OrderByDescending(x => x.LeftAt)
                .FirstOrDefault();
            if (latestDeparture?.Reason == BandDepartureReason.RemovedByOwner)
                continue;
            var band = await db.FindAsync<Band>(invitation.BandId, ct);
            if (band is not null)
                result.Add(new(band.Id, band.Name, invitation.RespondedAt ?? invitation.CreatedAt));
        }
        return ApiResponse<IReadOnlyList<FormerBandResponse>>.Success(result);
    }
}

public static class RejoinBandEndpoint
{
    [Authorize]
    [WolverinePost("/band-api/bands/{bandId}/membership/rejoin", OperationId = "RejoinBand")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public static async Task<IResult> Post(
        Guid bandId,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        await using var transaction = await db.BeginTransactionAsync(ct);
        if (await BandAccess.FindMembership(db, bandId, user.Id, ct) is not null)
            return Results.Conflict(new { code = "already_member" });

        var departures = await db.QueryAsync<BandDeparture>("select * from public.band_departures where band_id = @bandId and user_id = @id", new { bandId, id = user.Id }, ct);
        var departure = departures.OrderByDescending(x => x.LeftAt).FirstOrDefault();
        if (
            departure is not null
            && (
                departure.Reason != BandDepartureReason.Voluntary
                || departure.RejoinedAt is not null
            )
        )
            return Results.Forbid();
        if (departure is null)
        {
            var email = user.Email.Trim().ToLowerInvariant();
            var acceptedInvitation = await db.SingleOrDefaultAsync<BandInvitation>("select * from public.band_invitations where band_id = @bandId and invitee_email = @email and status = 1", new { bandId, email }, ct);
            if (acceptedInvitation is null)
                return Results.Forbid();
        }
        if (await db.FindAsync<Band>(bandId, ct) is null)
            return Results.NotFound();

        if (departure is not null)
        {
            departure.RejoinedAt = DateTimeOffset.UtcNow;
            await db.UpdateAsync(departure, ct);

        }
        await db.InsertAsync(
            new BandMember
            {
                Id = Guid.CreateVersion7(),
                BandId = bandId,
                UserId = user.Id,
                Email = user.Email.Trim().ToLowerInvariant(),
                Role = BandMemberRole.Member,
                JoinedAt = DateTimeOffset.UtcNow,
            }, ct);

        await transaction.CommitAsync(ct);
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}
