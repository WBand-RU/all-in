using Microsoft.AspNetCore.Mvc;
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

/// <summary>An email invitation with an Admin or Member role.</summary>
public sealed record CreateInvitationRequest(
    string InviteeEmail,
    BandMemberRole Role = BandMemberRole.Member
);

/// <summary>Accepts or declines a pending invitation.</summary>
public sealed record RespondInvitationRequest(bool Accept);

/// <summary>Explains why a new invitation cannot be created.</summary>
public sealed record InvitationConflictResponse(string Code);

/// <summary>An invitation with the band and inviter information.</summary>
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
    [WolverineGet("/band-api/bands/{bandId}/invitations", OperationId = "GetBandInvitationList")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BandInvitation>>), StatusCodes.Status200OK)]
    public static async Task<IResult> Get(
        Guid bandId,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        if (!await BandAccess.IsOwner(db, bandId, user.Id, ct))
            return Results.Forbid();
        var invitations = await db.QueryAsync<BandInvitation>("select * from public.band_invitations where band_id = @bandId", new { bandId }, ct);
        return Results.Ok(ApiResponse<IReadOnlyList<BandInvitation>>.Success(invitations));
    }

    [Authorize]
    [WolverinePost("/band-api/bands/{bandId}/invitations", OperationId = "CreateBandInvitation")]
    [ProducesResponseType(typeof(ApiResponse<BandInvitation>), StatusCodes.Status200OK)]
    public static async Task<IResult> Post(
        Guid bandId,
        CreateInvitationRequest request,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        [Microsoft.AspNetCore.Mvc.FromServices] IMessageBus bus,
        CancellationToken ct
    )
    {
        await using var transaction = await db.BeginTransactionAsync(ct);
        if (!await BandAccess.IsOwner(db, bandId, user.Id, ct))
            return Results.Forbid();
        var email = request.InviteeEmail.Trim().ToLowerInvariant();
        var existingMember = await db.SingleOrDefaultAsync<BandMember>("select * from public.band_members where band_id = @bandId and email = @email", new { bandId, email }, ct);
        if (existingMember is not null)
            return Results.Conflict(new InvitationConflictResponse("member_already_exists"));

        var now = DateTimeOffset.UtcNow;
        var existingInvitation = await db.SingleOrDefaultAsync<BandInvitation>("select * from public.band_invitations where band_id = @bandId and invitee_email = @email", new { bandId, email }, ct);
        if (existingInvitation is not null)
        {
            var isPending =
                existingInvitation.Status == InvitationStatus.Pending
                && existingInvitation.ExpiresAt > now;
            return Results.Conflict(
                new InvitationConflictResponse(
                    isPending ? "invitation_already_pending" : "invitation_already_created"
                )
            );
        }

        var invitation = new BandInvitation
        {
            Id = Guid.CreateVersion7(),
            BandId = bandId,
            InviterId = user.Id,
            InviterEmail = user.Email.Trim().ToLowerInvariant(),
            InviteeEmail = email,
            Role = request.Role,
            Status = InvitationStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
        };

        await db.InsertAsync(invitation, ct);
        await transaction.CommitAsync(ct);
        await bus.PublishAsync(new MemberInvited(invitation.Id, bandId, email));
        return Results.Ok(ApiResponse<BandInvitation>.Success(invitation));
    }
}

public static class RevokeInvitationEndpoint
{
    [Authorize]
    [WolverineDelete("/band-api/bands/{bandId}/invitations/{invitationId}", OperationId = "RevokeInvitation")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public static async Task<IResult> Delete(
        Guid bandId,
        Guid invitationId,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        await using var transaction = await db.BeginTransactionAsync(ct);
        if (!await BandAccess.IsOwner(db, bandId, user.Id, ct))
            return Results.Forbid();
        var invitation = await db.FindAsync<BandInvitation>(invitationId, ct);
        if (invitation is null || invitation.BandId != bandId)
            return Results.NotFound();
        if (invitation.Status != InvitationStatus.Pending)
            return Results.Conflict();
        invitation.Status = InvitationStatus.Revoked;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        await db.UpdateAsync(invitation, ct);

        await transaction.CommitAsync(ct);
        return Results.Ok(ApiResponse<bool>.Success(true));
    }
}

public static class MyInvitationsEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/user/invitations", OperationId = "GetMyInvitations")]
    public static async Task<ApiResponse<IReadOnlyList<MyInvitationResponse>>> Get(
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        await using var transaction = await db.BeginTransactionAsync(ct);
        var email = user.Email.Trim().ToLowerInvariant();
        await db.ExecuteAsync("""
            update public.band_invitations set status = 4
            where invitee_email = @email and status = 0 and expires_at <= clock_timestamp()
            """, new { email }, ct);
        var invitations = await db.QueryAsync<BandInvitation>("select * from public.band_invitations where invitee_email = @email order by band_id", new { email }, ct);
        var result = new List<MyInvitationResponse>();
        foreach (var invitation in invitations)
        {
            var band = await db.FindAsync<Band>(invitation.BandId, ct);
            if (band is not null)
                result.Add(
                    new(
                        invitation.Id,
                        invitation.InviteeEmail,
                        band.Id,
                        band.Name,
                        invitation.InviterEmail,
                        invitation.Role,
                        invitation.Status,
                        invitation.ExpiresAt
                    )
                );
        }

        await transaction.CommitAsync(ct);
        return ApiResponse<IReadOnlyList<MyInvitationResponse>>.Success(result);
    }
}

public static class InvitationCountEndpoint
{
    [Authorize]
    [WolverineGet("/band-api/user/invitations/count", OperationId = "GetMyInvitationsCount")]
    public static async Task<ApiResponse<int>> Get(
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        CancellationToken ct
    )
    {
        var email = user.Email.Trim().ToLowerInvariant();
        var count = await db.SingleOrDefaultAsync<int>("select count(*)::integer from public.band_invitations where invitee_email = @email and status = 0 and expires_at > clock_timestamp()", new { email }, ct);
        return ApiResponse<int>.Success(count);
    }
}

public static class RespondInvitationEndpoint
{
    [Authorize]
    [WolverinePut("/band-api/user/invitations/{invitationId}", OperationId = "RespondInvitation")]
    [ProducesResponseType(typeof(ApiResponse<BandInvitation>), StatusCodes.Status200OK)]
    public static async Task<IResult> Put(
        Guid invitationId,
        RespondInvitationRequest request,
        ICurrentUser user,
        [Microsoft.AspNetCore.Mvc.FromServices] DataContext db,
        [Microsoft.AspNetCore.Mvc.FromServices] IMessageBus bus,
        CancellationToken ct
    )
    {
        await using var transaction = await db.BeginTransactionAsync(ct);
        var invitation = await db.FindAsync<BandInvitation>(invitationId, ct);
        if (invitation is null)
            return Results.NotFound();
        await db.LockBandAsync(invitation.BandId, ct);
        invitation = await db.FindAsync<BandInvitation>(invitationId, ct);
        if (invitation is null) return Results.NotFound();
        if (
            !string.Equals(
                invitation.InviteeEmail,
                user.Email.Trim(),
                StringComparison.OrdinalIgnoreCase
            )
        )
            return Results.Forbid();
        if (invitation.Status != InvitationStatus.Pending)
            return Results.Conflict();
        if (invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await db.UpdateAsync(invitation, ct);
            await transaction.CommitAsync(ct);
            return Results.Conflict();
        }
        invitation.Status = request.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.RespondedAt = DateTimeOffset.UtcNow;
        await db.UpdateAsync(invitation, ct);

        if (request.Accept)
        {
            await db.InsertAsync(
            new BandMember
                {
                    Id = Guid.CreateVersion7(),
                    BandId = invitation.BandId,
                    UserId = user.Id,
                    Email = user.Email.Trim().ToLowerInvariant(),
                    Role = invitation.Role,
                    JoinedAt = DateTimeOffset.UtcNow,
                }, ct);
        }

        await transaction.CommitAsync(ct);
        if (request.Accept)
            await bus.PublishAsync(new MembershipChanged(invitation.BandId, user.Id, true));
        return Results.Ok(ApiResponse<BandInvitation>.Success(invitation));
    }
}
