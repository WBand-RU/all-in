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
        session.Delete(member);
        await session.SaveChangesAsync(ct);
        await bus.PublishAsync(new MembershipChanged(bandId, member.UserId, false));
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}
