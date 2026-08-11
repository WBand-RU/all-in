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

public sealed record CreateInvitationRequest(
    string InviteeEmail,
    BandMemberRole Role = BandMemberRole.Member
);
public sealed record RespondInvitationRequest(bool Accept);
public sealed record MyInvitationResponse(
    Guid Id,
    string InviteeEmail,
    Guid BandId,
    string BandName,
    string InviterEmail,
    BandMemberRole Role,
    InvitationStatus Status,
    DateTimeOffset ExpiresAt
);

public sealed class CreateInvitationRequestValidator : AbstractValidator<CreateInvitationRequest>
{
    public CreateInvitationRequestValidator()
    {
        RuleFor(x => x.InviteeEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Role)
            .Must(role => role is BandMemberRole.Admin or BandMemberRole.Member)
            .WithMessage("Only Admin or Member can be assigned through an invitation.");
    }
}

public static class BandInvitationsEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/bands/{bandId}/invitations")]
    public static async Task<IResult> Get(Guid bandId, ICurrentUser user, IQuerySession session, CancellationToken ct)
    {
        if (!await BandAccess.IsOwner(session, bandId, user.GetUserId, ct)) return Results.Forbid();
        var invitations = await session.Query<BandInvitation>().Where(x => x.BandId == bandId).ToListAsync(ct);
        return Results.Ok(ApiResponse<IReadOnlyList<BandInvitation>>.Success(invitations));
    }

    [Authorize]
    [WolverinePost("/band-api/bands/{bandId}/invitations")]
    public static async Task<IResult> Post(
        Guid bandId,
        CreateInvitationRequest request,
        ICurrentUser user,
        IDocumentSession session,
        IMessageBus bus,
        CancellationToken ct
    )
    {
        if (!await BandAccess.IsOwner(session, bandId, user.GetUserId, ct)) return Results.Forbid();
        var email = request.InviteeEmail.Trim().ToLowerInvariant();
        var existingMember = await session.Query<BandMember>().FirstOrDefaultAsync(x => x.BandId == bandId && x.Email == email, ct);
        var existingInvitation = await session.Query<BandInvitation>().FirstOrDefaultAsync(x => x.BandId == bandId && x.InviteeEmail == email && x.Status == InvitationStatus.Pending, ct);
        if (existingMember is not null || existingInvitation is not null)
            return Results.Conflict(ApiResponse<BandInvitation>.Error(ApiCodes.Conflict));
        var invitation = new BandInvitation
        {
            Id = Guid.CreateVersion7(),
            BandId = bandId,
            InviterId = user.GetUserId,
            InviterEmail = user.GetUserEmail.Trim().ToLowerInvariant(),
            InviteeEmail = email,
            Role = request.Role,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        };
        session.Store(invitation);
        await session.SaveChangesAsync(ct);
        await bus.PublishAsync(new MemberInvited(invitation.Id, bandId, email));
        return Results.Ok(ApiResponse<BandInvitation>.Success(invitation));
    }
}

public static class RevokeInvitationEndpoint
{
    [Authorize]
    [WolverineDelete("/band-api/bands/{bandId}/invitations/{invitationId}")]
    public static async Task<IResult> Delete(Guid bandId, Guid invitationId, ICurrentUser user, IDocumentSession session, CancellationToken ct)
    {
        if (!await BandAccess.IsOwner(session, bandId, user.GetUserId, ct)) return Results.Forbid();
        var invitation = await session.LoadAsync<BandInvitation>(invitationId, ct);
        if (invitation is null || invitation.BandId != bandId) return Results.NotFound();
        if (invitation.Status != InvitationStatus.Pending) return Results.Conflict();
        invitation.Status = InvitationStatus.Revoked;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        session.Store(invitation);
        await session.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}

public static class MyInvitationsEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/user/invitations")]
    public static async Task<ApiResponse<IReadOnlyList<MyInvitationResponse>>> Get(ICurrentUser user, IDocumentSession session, CancellationToken ct)
    {
        var email = user.GetUserEmail.Trim().ToLowerInvariant();
        var invitations = await session.Query<BandInvitation>().Where(x => x.InviteeEmail == email).ToListAsync(ct);
        var result = new List<MyInvitationResponse>();
        foreach (var invitation in invitations)
        {
            if (invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                session.Store(invitation);
            }
            var band = await session.LoadAsync<Band>(invitation.BandId, ct);
            if (band is not null) result.Add(new(invitation.Id, invitation.InviteeEmail, band.Id, band.Name, invitation.InviterEmail, invitation.Role, invitation.Status, invitation.ExpiresAt));
        }
        await session.SaveChangesAsync(ct);
        return ApiResponse<IReadOnlyList<MyInvitationResponse>>.Success(result);
    }
}

public static class InvitationCountEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/user/invitations/count")]
    public static async Task<ApiResponse<int>> Get(ICurrentUser user, IQuerySession session, CancellationToken ct)
    {
        var email = user.GetUserEmail.Trim().ToLowerInvariant();
        var count = await session.Query<BandInvitation>().CountAsync(x => x.InviteeEmail == email && x.Status == InvitationStatus.Pending && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
        return ApiResponse<int>.Success(count);
    }
}

public static class RespondInvitationEndpoint
{
    [Authorize]
    [WolverinePut("/band-api/user/invitations/{invitationId}")]
    public static async Task<IResult> Put(Guid invitationId, RespondInvitationRequest request, ICurrentUser user, IDocumentSession session, IMessageBus bus, CancellationToken ct)
    {
        var invitation = await session.LoadAsync<BandInvitation>(invitationId, ct);
        if (invitation is null) return Results.NotFound();
        if (!string.Equals(invitation.InviteeEmail, user.GetUserEmail.Trim(), StringComparison.OrdinalIgnoreCase)) return Results.Forbid();
        if (invitation.Status != InvitationStatus.Pending) return Results.Conflict();
        if (invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            session.Store(invitation);
            await session.SaveChangesAsync(ct);
            return Results.Conflict();
        }
        invitation.Status = request.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        session.Store(invitation);
        if (request.Accept)
        {
            session.Store(new BandMember
            {
                Id = Guid.CreateVersion7(), BandId = invitation.BandId, UserId = user.GetUserId,
                Email = user.GetUserEmail.Trim().ToLowerInvariant(), Role = invitation.Role,
                JoinedAt = DateTimeOffset.UtcNow,
            });
        }
        await session.SaveChangesAsync(ct);
        if (request.Accept) await bus.PublishAsync(new MembershipChanged(invitation.BandId, user.GetUserId, true));
        return Results.Ok(ApiResponse<BandInvitation>.Success(invitation));
    }
}
