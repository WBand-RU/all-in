using WBand.Modules.BandModule.Endpoints;
using System.Net;
using System.Net.Http.Json;
using WBand.Modules.BandModule.Data;
using WBand.WebAPI.Shared;
using WBand.Modules.BandModule.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Web API")]
public sealed class BandsApiTests(BandsApiFixture fixture)
{
    [Fact]
    public async Task OpenApi_WhenBandEndpointsReturnResults_DescribesSuccessPayloads()
    {
        using var client = fixture.CreateClient();
        using var document = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.Equal("WBand.WebAPI.Endpoints.Bands.CreateBandEndpoint",
            paths.GetProperty("/band-api/bands").GetProperty("post").GetProperty("tags")[0].GetString());
        Assert.Equal("WBand.WebAPI.Endpoints.CurrentUserEndpoint",
            paths.GetProperty("/api/user/me").GetProperty("get").GetProperty("tags")[0].GetString());
        var contracts = new[]
        {
            ("/band-api/bands/{bandId}", "get", "ApiResponseOfBand"),
            ("/band-api/bands/{bandId}", "put", "ApiResponse"),
            ("/band-api/bands/{bandId}/members", "get", "ApiResponseOfIReadOnlyListOfBandMember"),
            ("/band-api/bands/{bandId}/invitations", "get", "ApiResponseOfIReadOnlyListOfBandInvitation"),
            ("/band-api/bands/{bandId}/invitations", "post", "ApiResponseOfBandInvitation"),
            ("/band-api/user/invitations/{invitationId}", "put", "ApiResponseOfBandInvitation"),
        };
        foreach (var (path, method, schema) in contracts)
        {
            var response = paths.GetProperty(path).GetProperty(method).GetProperty("responses")
                .GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema");
            Assert.Equal($"#/components/schemas/{schema}", response.GetProperty("$ref").GetString());
        }
    }

    [Fact]
    public async Task CreateBand_WhenNameIsValid_PersistsBandAndOwner()
    {
        var ownerId = Guid.NewGuid();
        using var owner = fixture.CreateClient(ownerId);
        var band = await CreateBand(owner);
        await using var db = fixture.CreateBandContext();
        var membership = await db.SingleOrDefaultAsync<BandMember>("select * from band_members where band_id = @bandId", new { bandId = band.Id });
        Assert.NotNull(membership);
        Assert.Equal(ownerId, membership.UserId);
        Assert.Equal(BandMemberRole.Owner, membership.Role);
        Assert.Equal("Test Band", (await db.SingleOrDefaultAsync<Band>("select * from bands where id = @bandId", new { bandId = band.Id }))!.Name);
        Assert.Equal(band.Id, Assert.Single((await owner.GetFromJsonAsync<ApiResponse<List<Band>>>("/band-api/bands"))!.Value).Id);
    }

    [Fact]
    public async Task CreateBand_WhenPublicationIsUnavailable_PersistsBandAndOwner()
    {
        var ownerId = Guid.NewGuid();
        using var owner = fixture.CreateClient(ownerId);
        fixture.RejectPublications = true;
        try
        {
            // Verify the fault remains active even without test event subscribers.
            using (var scope = fixture.Services.CreateScope())
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await scope.ServiceProvider.GetRequiredService<IMessageBus>()
                        .PublishAsync(new BandCreated(Guid.NewGuid(), ownerId, "Fault probe")));
            }
            var band = await CreateBand(owner);
            await using var db = fixture.CreateBandContext();
            var stored = await db.SingleOrDefaultAsync<Band>(
                "select * from bands where id = @id", new { id = band.Id });
            Assert.NotNull(stored);
            var member = await db.SingleOrDefaultAsync<BandMember>(
                "select * from band_members where band_id = @id", new { id = band.Id });
            Assert.NotNull(member);
            Assert.Equal(ownerId, member.UserId);
            Assert.Equal(BandMemberRole.Owner, member.Role);
        }
        finally
        {
            fixture.RejectPublications = false;
        }
    }

    [Fact]
    public async Task Band_WhenUserIsNotMember_DeniesAccessAndChanges()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        using var outsider = fixture.CreateClient(Guid.NewGuid(), "outsider@example.com");
        using var anonymous = fixture.CreateClient();
        var band = await CreateBand(owner);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/band-api/bands")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/band-api/bands/{band.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PutAsJsonAsync($"/band-api/bands/{band.Id}", new UpdateBandRequest("Other"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.DeleteAsync($"/band-api/bands/{band.Id}")).StatusCode);
        Assert.Empty((await outsider.GetFromJsonAsync<ApiResponse<List<Band>>>("/band-api/bands"))!.Value);
    }

    [Fact]
    public async Task CreateBand_WhenCommitFails_RollsBackBandOwnerAndEvent()
    {
        var ownerId = Guid.NewGuid();
        using var owner = fixture.CreateClient(ownerId);
        var response = await owner.PostAsJsonAsync("/band-api/bands", new CreateBandRequest("fail_commit"));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        await using var db = fixture.CreateBandContext();
        Assert.False(await db.SingleOrDefaultAsync<bool>("select exists(select 1 from bands where created_by = @ownerId)", new { ownerId = ownerId }));
        Assert.False(await db.SingleOrDefaultAsync<bool>("select exists(select 1 from band_members where user_id = @ownerId)", new { ownerId = ownerId }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateBand_WhenNameIsEmpty_RejectsRequest(string name)
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var response = await owner.PostAsJsonAsync("/band-api/bands", new CreateBandRequest(name));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Invitation_WhenPublicationIsUnavailable_PersistsMembershipAndRejoin()
    {
        fixture.RejectPublications = true;
        try
        {
            using var owner = fixture.CreateClient(Guid.NewGuid());
            var inviteeId = Guid.NewGuid();
            var email = $"{inviteeId:N}@example.com";
            using var invitee = fixture.CreateClient(inviteeId, email.ToUpperInvariant());
            var band = await CreateBand(owner);
            var invitation = await Invite(owner, band.Id, email, BandMemberRole.Admin);
            var count = await invitee.GetFromJsonAsync<ApiResponse<int>>("/band-api/user/invitations/count");
            Assert.Equal(1, count!.Value);
            Assert.Equal(HttpStatusCode.OK, (await invitee.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true))).StatusCode);
            await using var db = fixture.CreateBandContext();
            var member = await db.SingleOrDefaultAsync<BandMember>("select * from band_members where band_id = @bandId and user_id = @inviteeId", new { bandId = band.Id, inviteeId = inviteeId });
            Assert.NotNull(member);
            Assert.Equal(BandMemberRole.Admin, member.Role);
            Assert.Equal(HttpStatusCode.Conflict, (await invitee.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await invitee.DeleteAsync($"/band-api/bands/{band.Id}/membership")).StatusCode);
            Assert.Single((await invitee.GetFromJsonAsync<ApiResponse<List<FormerBandResponse>>>("/band-api/user/former-bands"))!.Value);
            Assert.Equal(HttpStatusCode.OK, (await invitee.PostAsync($"/band-api/bands/{band.Id}/membership/rejoin", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await invitee.PostAsync($"/band-api/bands/{band.Id}/membership/rejoin", null)).StatusCode);

            var rejoined = (await db.SingleOrDefaultAsync<BandMember>("select * from band_members where band_id = @bandId and user_id = @inviteeId", new { bandId = band.Id, inviteeId = inviteeId }))!;
            Assert.Equal(BandMemberRole.Member, rejoined.Role);
            Assert.NotNull((await db.SingleOrDefaultAsync<BandDeparture>("select * from band_departures where band_id = @bandId", new { bandId = band.Id }))!.RejoinedAt);
            Assert.Equal(HttpStatusCode.OK, (await owner.DeleteAsync($"/band-api/bands/{band.Id}/members/{rejoined.Id}")).StatusCode);
            Assert.Null(await db.SingleOrDefaultAsync<BandMember>("select * from band_members where id = @id", new { id = rejoined.Id }));
        }
        finally
        {
            fixture.RejectPublications = false;
        }
    }

    [Fact]
    public async Task Invitation_WhenExpiredOrWrongRecipient_DoesNotAddMember()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var id = Guid.NewGuid();
        var email = $"{id:N}@example.com";
        using var invitee = fixture.CreateClient(id, email);
        using var outsider = fixture.CreateClient(Guid.NewGuid(), "outsider@example.com");
        var band = await CreateBand(owner);
        var invitation = await Invite(owner, band.Id, email);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true))).StatusCode);
        await using var db = fixture.CreateBandContext();
        // ExpiresAt is immutable in application code; expire it through a SQL update for the scenario.
        await db.ExecuteAsync("update band_invitations set expires_at = @expiresAt where id = @id", new { expiresAt = DateTimeOffset.UtcNow.AddDays(-1), id = invitation.Id });
        Assert.Equal(HttpStatusCode.Conflict, (await invitee.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true))).StatusCode);
        Assert.Equal(InvitationStatus.Expired, (await db.SingleOrDefaultAsync<BandInvitation>("select * from band_invitations where id = @invitationId", new { invitationId = invitation.Id }))!.Status);
        Assert.False(await db.SingleOrDefaultAsync<bool>("select exists(select 1 from band_members where user_id = @id)", new { id = id }));
    }

    [Fact]
    public async Task Owner_WhenManagingMembers_CannotLeaveOrChangeOwnerRole()
    {
        var ownerId = Guid.NewGuid();
        using var owner = fixture.CreateClient(ownerId);
        var memberId = Guid.NewGuid();
        var email = $"{memberId:N}@example.com";
        using var member = fixture.CreateClient(memberId, email);
        var band = await CreateBand(owner);
        var invitation = await Invite(owner, band.Id, email);
        (await member.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true))).EnsureSuccessStatusCode();
        await using var db = fixture.CreateBandContext();
        var membership = await db.SingleOrDefaultAsync<BandMember>("select * from band_members where band_id = @bandId and user_id = @memberId", new { bandId = band.Id, memberId = memberId });
        Assert.NotNull(membership);
        var ownerMembership = await db.SingleOrDefaultAsync<BandMember>("select * from band_members where band_id = @bandId and user_id = @ownerId", new { bandId = band.Id, ownerId = ownerId });
        Assert.NotNull(ownerMembership);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/band-api/bands/{band.Id}/members/{membership.Id}/role", new UpdateMemberRoleRequest(BandMemberRole.Admin))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await owner.PutAsJsonAsync($"/band-api/bands/{band.Id}/members/{ownerMembership.Id}/role", new UpdateMemberRoleRequest(BandMemberRole.Member))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.DeleteAsync($"/band-api/bands/{band.Id}/membership")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.DeleteAsync($"/band-api/bands/{band.Id}/members/{membership.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await member.PostAsync($"/band-api/bands/{band.Id}/membership/rejoin", null)).StatusCode);
        Assert.Empty((await member.GetFromJsonAsync<ApiResponse<List<FormerBandResponse>>>("/band-api/user/former-bands"))!.Value);
    }

    [Fact]
    public async Task DeleteBand_WhenOwnerDeletes_CascadesRelatedRecords()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        await Invite(owner, band.Id, $"{Guid.NewGuid():N}@example.com");
        (await owner.PutAsJsonAsync($"/band-api/bands/{band.Id}", new UpdateBandRequest("Renamed"))).EnsureSuccessStatusCode();
        (await owner.DeleteAsync($"/band-api/bands/{band.Id}")).EnsureSuccessStatusCode();
        await using var db = fixture.CreateBandContext();
        Assert.False(await db.SingleOrDefaultAsync<bool>("select exists(select 1 from bands where id = @bandId)", new { bandId = band.Id }));
        Assert.False(await db.SingleOrDefaultAsync<bool>("select exists(select 1 from band_members where band_id = @bandId)", new { bandId = band.Id }));
        Assert.False(await db.SingleOrDefaultAsync<bool>("select exists(select 1 from band_invitations where band_id = @bandId)", new { bandId = band.Id }));
        Assert.False(await db.SingleOrDefaultAsync<bool>("select exists(select 1 from band_departures where band_id = @bandId)", new { bandId = band.Id }));
    }

    [Fact]
    public async Task Invitation_WhenDeclinedOrRevoked_DoesNotGrantMembership()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        var id = Guid.NewGuid();
        var email = $"{id:N}@example.com";
        using var invitee = fixture.CreateClient(id, email);
        var invitation = await Invite(owner, band.Id, email);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/band-api/bands/{band.Id}/invitations", new CreateInvitationRequest(email))).StatusCode);
        (await invitee.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(false))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await invitee.PostAsync($"/band-api/bands/{band.Id}/membership/rejoin", null)).StatusCode);
        var otherId = Guid.NewGuid();
        var otherEmail = $"{otherId:N}@example.com";
        using var other = fixture.CreateClient(otherId, otherEmail);
        var revoked = await Invite(owner, band.Id, otherEmail);
        (await owner.DeleteAsync($"/band-api/bands/{band.Id}/invitations/{revoked.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await other.PutAsJsonAsync($"/band-api/user/invitations/{revoked.Id}", new RespondInvitationRequest(true))).StatusCode);
        await using var db = fixture.CreateBandContext();
        Assert.Equal(InvitationStatus.Declined, (await db.SingleOrDefaultAsync<BandInvitation>("select * from band_invitations where id = @invitationId", new { invitationId = invitation.Id }))!.Status);
        Assert.Equal(InvitationStatus.Revoked, (await db.SingleOrDefaultAsync<BandInvitation>("select * from band_invitations where id = @revokedId", new { revokedId = revoked.Id }))!.Status);
        Assert.False(await db.SingleOrDefaultAsync<bool>("select exists(select 1 from band_members where user_id = @id or user_id = @otherId)", new { id = id, otherId = otherId }));
    }

    [Fact]
    public async Task Permissions_WhenMembershipExists_RespectsOwnerAdminAndMemberRoles()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        using var owner = fixture.CreateClient(ownerId);
        using var admin = fixture.CreateClient(adminId, $"{adminId:N}@example.com");
        var band = await CreateBand(owner);
        var invitation = await Invite(owner, band.Id, $"{adminId:N}@example.com", BandMemberRole.Admin);
        (await admin.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true))).EnsureSuccessStatusCode();
        var memberId = Guid.NewGuid();
        using var member = fixture.CreateClient(memberId, $"{memberId:N}@example.com");
        var memberInvitation = await Invite(owner, band.Id, $"{memberId:N}@example.com", BandMemberRole.Member);
        (await member.PutAsJsonAsync($"/band-api/user/invitations/{memberInvitation.Id}", new RespondInvitationRequest(true))).EnsureSuccessStatusCode();
        await using var scope = fixture.Services.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        Assert.True(await bus.InvokeAsync<bool>(new CheckBandPermission(band.Id, ownerId, BandPermission.ManageBand)));
        Assert.True(await bus.InvokeAsync<bool>(new CheckBandPermission(band.Id, adminId, BandPermission.EditContent)));
        Assert.False(await bus.InvokeAsync<bool>(new CheckBandPermission(band.Id, adminId, BandPermission.ManageBand)));
        Assert.False(await bus.InvokeAsync<bool>(new CheckBandPermission(band.Id, Guid.NewGuid(), BandPermission.View)));
        Assert.True(await bus.InvokeAsync<bool>(new CheckBandPermission(band.Id, memberId, BandPermission.View)));
        Assert.False(await bus.InvokeAsync<bool>(new CheckBandPermission(band.Id, memberId, BandPermission.EditContent)));
        Assert.False(await bus.InvokeAsync<bool>(new CheckBandPermission(band.Id, memberId, BandPermission.ManageBand)));
        Assert.Equal("Admin", (await bus.InvokeAsync<BandMemberInfo>(new GetBandMemberInfo(band.Id, adminId))).Role);
        Assert.Equal(band.Id, Assert.Single(await bus.InvokeAsync<Guid[]>(new GetUserBandIds(adminId))));
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync($"/band-api/bands/{band.Id}/invitations", new CreateInvitationRequest("another@example.com"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/band-api/bands/{band.Id}/invitations", new CreateInvitationRequest("another@example.com", BandMemberRole.Owner))).StatusCode);
    }

    [Fact]
    public async Task Membership_WhenInsertedTwice_DatabaseRejectsDuplicate()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        await using var db = fixture.CreateBandContext();
        var existing = await db.SingleOrDefaultAsync<BandMember>("select * from band_members where band_id = @bandId", new { bandId = band.Id });
        await using var transaction = await db.BeginTransactionAsync();
        var duplicate = new BandMember
        {
            Id = Guid.NewGuid(), BandId = band.Id, UserId = existing!.UserId,
            Email = "duplicate@example.com", Role = BandMemberRole.Member, JoinedAt = DateTimeOffset.UtcNow,
        };
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() => db.InsertAsync(duplicate));
    }

    [Fact]
    public async Task Invitation_WhenAcceptedConcurrently_AddsOneMembership()
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        var userId = Guid.NewGuid();
        var email = $"{userId:N}@example.com";
        using var member = fixture.CreateClient(userId, email);
        var invitation = await Invite(owner, band.Id, email);
        var results = await Task.WhenAll(
            member.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true)),
            member.PutAsJsonAsync($"/band-api/user/invitations/{invitation.Id}", new RespondInvitationRequest(true)));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        await using var db = fixture.CreateBandContext();
        Assert.Equal(1, await db.SingleOrDefaultAsync<int>("select count(*)::int from band_members where band_id = @bandId and user_id = @userId", new { bandId = band.Id, userId }));
    }

    [Theory]
    [InlineData("Band")]
    [InlineData("Member")]
    [InlineData("Invitation")]
    [InlineData("Departure")]
    public async Task UserForeignKey_WhenUserDoesNotExist_RejectsWrite(string entity)
    {
        using var owner = fixture.CreateClient(Guid.NewGuid());
        var band = await CreateBand(owner);
        var unknownUserId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using var db = fixture.CreateBandContext();
        object orphan = entity switch
        {
            "Band" => new Band { Id = Guid.NewGuid(), Name = "Orphan", CreatedBy = unknownUserId, CreatedAt = now },
            "Member" => new BandMember { Id = Guid.NewGuid(), BandId = band.Id, UserId = unknownUserId, Email = "orphan@example.com", Role = BandMemberRole.Member, JoinedAt = now },
            "Invitation" => new BandInvitation { Id = Guid.NewGuid(), BandId = band.Id, InviterId = unknownUserId, InviterEmail = "orphan@example.com", InviteeEmail = "recipient@example.com", Status = InvitationStatus.Pending, CreatedAt = now, ExpiresAt = now.AddDays(7) },
            "Departure" => new BandDeparture { Id = Guid.NewGuid(), BandId = band.Id, UserId = unknownUserId, Email = "orphan@example.com", Reason = BandDepartureReason.Voluntary, LeftAt = now },
            _ => throw new ArgumentOutOfRangeException(nameof(entity)),
        };
        await using var transaction = await db.BeginTransactionAsync();
        var updateError = await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(async () =>
        {
            switch (orphan)
            {
                case Band value: await db.InsertAsync(value); break;
                case BandMember value: await db.InsertAsync(value); break;
                case BandInvitation value: await db.InsertAsync(value); break;
                case BandDeparture value: await db.InsertAsync(value); break;
            }
        });
        Assert.Equal(Npgsql.PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<Npgsql.PostgresException>(updateError.InnerException).SqlState);
    }

    [Fact]
    public async Task DeleteUser_WhenUserOwnsBand_DatabasePreservesOwnership()
    {
        var ownerId = Guid.NewGuid();
        using var owner = fixture.CreateClient(ownerId);
        var band = await CreateBand(owner);
        await using var db = fixture.CreateBandContext();
        await using var transaction = await db.BeginTransactionAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.ExecuteAsync("delete from users where keycloak_id = @ownerId", new { ownerId }));

        await using var reader = fixture.CreateBandContext();
        Assert.True(await reader.SingleOrDefaultAsync<bool>("select exists(select 1 from users where keycloak_id = @ownerId)", new { ownerId = ownerId }));
        Assert.True(await reader.SingleOrDefaultAsync<bool>("select exists(select 1 from bands where id = @bandId)", new { bandId = band.Id }));
    }

    internal static async Task<Band> CreateBand(HttpClient owner)
    {
        var response = await owner.PostAsJsonAsync("/band-api/bands", new CreateBandRequest(" Test Band "));
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<Band>>())!.Value;
    }

    private static async Task<BandInvitation> Invite(HttpClient owner, Guid bandId, string email, BandMemberRole role = BandMemberRole.Member)
    {
        var response = await owner.PostAsJsonAsync($"/band-api/bands/{bandId}/invitations", new CreateInvitationRequest(email, role));
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<BandInvitation>>())!.Value;
    }
}
