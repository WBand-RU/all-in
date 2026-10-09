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

public sealed record UpdateMemberRoleRequest(BandMemberRole NewRole);
public sealed record MyBandAccessResponse(Guid BandId, BandMemberRole Role, bool CanEditContent);

public static class MyBandAccessEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/user/band-access")]
    public static async Task<ApiResponse<IReadOnlyList<MyBandAccessResponse>>> Get(
        ICurrentUser user,
        IQuerySession session,
        CancellationToken ct)
    {
        var memberships = await session.Query<BandMember>()
            .Where(x => x.UserId == user.GetUserId)
            .ToListAsync(ct);
        var access = memberships
            .GroupBy(x => x.BandId)
            .Select(group => group.OrderByDescending(x => x.JoinedAt).First())
            .Select(member => new MyBandAccessResponse(
                member.BandId,
                member.Role,
                member.Role is BandMemberRole.Owner or BandMemberRole.Admin))
            .ToList();
        return ApiResponse<IReadOnlyList<MyBandAccessResponse>>.Success(access);
    }
}

public static class MembersEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/bands/{bandId}/members")]
    public static async Task<IResult> Get(Guid bandId, ICurrentUser user, IQuerySession session, CancellationToken ct)
    {
        if (await BandAccess.FindMembership(session, bandId, user.GetUserId, ct) is null) return Results.Forbid();
        var members = await session.Query<BandMember>().Where(x => x.BandId == bandId).ToListAsync(ct);
        return Results.Ok(ApiResponse<IReadOnlyList<BandMember>>.Success(members));
    }
}

public static class UpdateMemberRoleEndpoint
{
    [Authorize]
    [WolverinePut("/band-api/bands/{bandId}/members/{memberId}/role")]
    public static async Task<IResult> Put(
        Guid bandId,
        Guid memberId,
        UpdateMemberRoleRequest request,
        ICurrentUser user,
        IDocumentSession session,
        CancellationToken ct
    )
    {
        if (!await BandAccess.IsOwner(session, bandId, user.GetUserId, ct))
            return Results.Forbid();
        if (request.NewRole is not (BandMemberRole.Admin or BandMemberRole.Member))
            return Results.BadRequest(ApiResponse<BandMember>.Error(ApiCodes.ValidationFailed));

        var member = await session.LoadAsync<BandMember>(memberId, ct);
        if (member is null || member.BandId != bandId) return Results.NotFound();
        if (member.Role == BandMemberRole.Owner) return Results.Forbid();

        member.Role = request.NewRole;
        session.Store(member);
        await session.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<BandMember>.Success(member));
    }
}

public static class RemoveMemberEndpoint
{
    [Authorize]
    [WolverineDelete("/band-api/bands/{bandId}/members/{memberId}")]
    public static async Task<IResult> Delete(Guid bandId, Guid memberId, ICurrentUser user, IDocumentSession session, IMessageBus bus, CancellationToken ct)
    {
        if (!await BandAccess.IsOwner(session, bandId, user.GetUserId, ct)) return Results.Forbid();
        var member = await session.LoadAsync<BandMember>(memberId, ct);
        if (member is null || member.BandId != bandId) return Results.NotFound();
        if (member.Role == BandMemberRole.Owner) return Results.Forbid();
        session.Store(new BandDeparture
        {
            Id = Guid.CreateVersion7(), BandId = bandId, UserId = member.UserId,
            Email = member.Email, Reason = BandDepartureReason.RemovedByOwner,
            LeftAt = DateTimeOffset.UtcNow,
        });
        session.Delete(member);
        await session.SaveChangesAsync(ct);
        await bus.PublishAsync(new MembershipChanged(bandId, member.UserId, false));
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}

public static class LeaveBandEndpoint
{
    [Authorize]
    [WolverineDelete("/band-api/bands/{bandId}/membership")]
    public static async Task<IResult> Delete(
        Guid bandId,
        ICurrentUser user,
        IDocumentSession session,
        IMessageBus bus,
        CancellationToken ct)
    {
        var member = await BandAccess.FindMembership(session, bandId, user.GetUserId, ct);
        if (member is null) return Results.NotFound();
        if (member.Role == BandMemberRole.Owner)
            return Results.Conflict(new { code = "owner_cannot_leave" });

        session.Store(new BandDeparture
        {
            Id = Guid.CreateVersion7(), BandId = bandId, UserId = member.UserId,
            Email = member.Email, Reason = BandDepartureReason.Voluntary,
            LeftAt = DateTimeOffset.UtcNow,
        });
        session.Delete(member);
        await session.SaveChangesAsync(ct);
        await bus.PublishAsync(new MembershipChanged(bandId, member.UserId, false));
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}

public sealed record FormerBandResponse(Guid BandId, string BandName, DateTimeOffset LeftAt);

public static class FormerBandsEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/user/former-bands")]
    public static async Task<ApiResponse<IReadOnlyList<FormerBandResponse>>> Get(
        ICurrentUser user,
        IQuerySession session,
        CancellationToken ct)
    {
        var departures = await session.Query<BandDeparture>()
            .Where(x => x.UserId == user.GetUserId && x.Reason == BandDepartureReason.Voluntary && x.RejoinedAt == null)
            .ToListAsync(ct);
        var result = new List<FormerBandResponse>();
        foreach (var departure in departures.GroupBy(x => x.BandId).Select(x => x.OrderByDescending(y => y.LeftAt).First()))
        {
            var band = await session.LoadAsync<Band>(departure.BandId, ct);
            if (band is not null) result.Add(new(band.Id, band.Name, departure.LeftAt));
        }

        // Compatibility for memberships left before departure reasons were introduced.
        var email = user.GetUserEmail.Trim().ToLowerInvariant();
        var acceptedInvitations = await session.Query<BandInvitation>()
            .Where(x => x.InviteeEmail == email && x.Status == InvitationStatus.Accepted)
            .ToListAsync(ct);
        var activeMemberships = await session.Query<BandMember>()
            .Where(x => x.UserId == user.GetUserId)
            .ToListAsync(ct);
        var allDepartures = await session.Query<BandDeparture>()
            .Where(x => x.UserId == user.GetUserId)
            .ToListAsync(ct);
        foreach (var invitation in acceptedInvitations)
        {
            if (activeMemberships.Any(x => x.BandId == invitation.BandId)
                || result.Any(x => x.BandId == invitation.BandId)) continue;
            var latestDeparture = allDepartures.Where(x => x.BandId == invitation.BandId)
                .OrderByDescending(x => x.LeftAt).FirstOrDefault();
            if (latestDeparture?.Reason == BandDepartureReason.RemovedByOwner) continue;
            var band = await session.LoadAsync<Band>(invitation.BandId, ct);
            if (band is not null) result.Add(new(band.Id, band.Name, invitation.RespondedAt ?? invitation.CreatedAt));
        }
        return ApiResponse<IReadOnlyList<FormerBandResponse>>.Success(result);
    }
}

public static class RejoinBandEndpoint
{
    [Authorize]
    [WolverinePost("/band-api/bands/{bandId}/membership/rejoin")]
    public static async Task<IResult> Post(
        Guid bandId,
        ICurrentUser user,
        IDocumentSession session,
        IMessageBus bus,
        CancellationToken ct)
    {
        if (await BandAccess.FindMembership(session, bandId, user.GetUserId, ct) is not null)
            return Results.Conflict(new { code = "already_member" });

        var departures = await session.Query<BandDeparture>()
            .Where(x => x.BandId == bandId && x.UserId == user.GetUserId)
            .ToListAsync(ct);
        var departure = departures.OrderByDescending(x => x.LeftAt).FirstOrDefault();
        if (departure is not null
            && (departure.Reason != BandDepartureReason.Voluntary || departure.RejoinedAt is not null))
            return Results.Forbid();
        if (departure is null)
        {
            var email = user.GetUserEmail.Trim().ToLowerInvariant();
            var acceptedInvitation = await session.Query<BandInvitation>().FirstOrDefaultAsync(
                x => x.BandId == bandId && x.InviteeEmail == email && x.Status == InvitationStatus.Accepted,
                ct);
            if (acceptedInvitation is null) return Results.Forbid();
        }
        if (await session.LoadAsync<Band>(bandId, ct) is null) return Results.NotFound();

        if (departure is not null)
        {
            departure.RejoinedAt = DateTimeOffset.UtcNow;
            session.Store(departure);
        }
        session.Store(new BandMember
        {
            Id = Guid.CreateVersion7(), BandId = bandId, UserId = user.GetUserId,
            Email = user.GetUserEmail.Trim().ToLowerInvariant(), Role = BandMemberRole.Member,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await session.SaveChangesAsync(ct);
        await bus.PublishAsync(new MembershipChanged(bandId, user.GetUserId, true));
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}
