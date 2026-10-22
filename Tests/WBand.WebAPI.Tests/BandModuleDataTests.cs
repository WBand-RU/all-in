using WBand.Modules.BandModule.Application;
using WBand.Modules.BandModule.Contracts;
using Microsoft.EntityFrameworkCore;
using WBand.Modules.BandModule.Data;
using Xunit;

namespace WBand.WebAPI.Tests;

[Collection("Database")]
public sealed class BandModuleDataTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task Save_WhenAllBandTablesAreUsed_PreservesIdsAndActorSubjects()
    {
        await using var db = fixture.CreateBandSession();
        var owner = Profile();
        var departed = Profile();
        var band = new Band { Id = Guid.CreateVersion7(), Name = "Module data", CreatedBy = owner.KeycloakId, CreatedAt = owner.CreatedAt };
        var member = new BandMember { Id = Guid.CreateVersion7(), BandId = band.Id, UserId = owner.KeycloakId, Email = owner.Email, Role = BandMemberRole.Owner, JoinedAt = band.CreatedAt };
        var invitation = new BandInvitation
        {
            Id = Guid.CreateVersion7(), BandId = band.Id, InviterId = owner.KeycloakId,
            InviterEmail = owner.Email, InviteeEmail = $"{Guid.NewGuid():N}@example.invalid",
            Role = BandMemberRole.Member, Status = InvitationStatus.Pending,
            CreatedAt = band.CreatedAt, ExpiresAt = band.CreatedAt.AddDays(7),
        };
        var departure = new BandDeparture { Id = Guid.CreateVersion7(), BandId = band.Id, UserId = departed.KeycloakId, Email = departed.Email, Reason = BandDepartureReason.Voluntary, LeftAt = band.CreatedAt };
        await using (var transaction = await db.BeginTransactionAsync())
        {
            await db.InsertAsync(owner);
            await db.InsertAsync(departed);
            await db.InsertAsync(band);
            await db.InsertAsync(member);
            await db.InsertAsync(invitation);
            await db.InsertAsync(departure);
            await transaction.CommitAsync();
        }
        await using var reader = fixture.CreateBandSession();
        Assert.NotEqual(owner.Id, owner.KeycloakId);
        Assert.Equal(owner.KeycloakId, (await reader.FindAsync<User>(owner.Id))!.KeycloakId);
        Assert.Equal(owner.KeycloakId, (await reader.FindAsync<Band>(band.Id))!.CreatedBy);
        Assert.Equal(owner.KeycloakId, (await reader.FindAsync<BandMember>(member.Id))!.UserId);
        Assert.Equal(invitation.ExpiresAt, (await reader.FindAsync<BandInvitation>(invitation.Id))!.ExpiresAt);
        Assert.Equal(departed.KeycloakId, (await reader.FindAsync<BandDeparture>(departure.Id))!.UserId);
    }

    [Fact]
    public async Task Save_WhenOwnerMembershipFails_RollsBackBandAndProfile()
    {
        await using var db = fixture.CreateBandSession();
        var owner = Profile();
        var band = new Band { Id = Guid.CreateVersion7(), Name = "Rollback", CreatedBy = owner.KeycloakId, CreatedAt = owner.CreatedAt };
        await using (var transaction = await db.BeginTransactionAsync())
        {
            await db.InsertAsync(owner);
            await db.InsertAsync(band);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.InsertAsync(new BandMember
            {
                Id = Guid.CreateVersion7(), BandId = band.Id, UserId = Guid.NewGuid(),
                Email = owner.Email, Role = BandMemberRole.Owner, JoinedAt = band.CreatedAt,
            }));
        }
        await using var reader = fixture.CreateBandSession();
        Assert.Null(await reader.FindAsync<User>(owner.Id));
        Assert.Null(await reader.FindAsync<Band>(band.Id));
    }

    [Fact]
    public async Task Contributors_WhenMembershipEnded_ReturnsProfileWithoutAnotherBandsRole()
    {
        await using var db = fixture.CreateBandSession();
        var owner = Profile();
        var contributor = Profile();
        var band = new Band { Id = Guid.CreateVersion7(), Name = "History", CreatedBy = owner.KeycloakId, CreatedAt = owner.CreatedAt };
        var other = new Band { Id = Guid.CreateVersion7(), Name = "Other", CreatedBy = contributor.KeycloakId, CreatedAt = contributor.CreatedAt };
        await using (var transaction = await db.BeginTransactionAsync())
        {
            await db.InsertAsync(owner);
            await db.InsertAsync(contributor);
            await db.InsertAsync(band);
            await db.InsertAsync(other);
            await db.InsertAsync(new BandMember { Id = Guid.CreateVersion7(), BandId = band.Id, UserId = owner.KeycloakId, Email = owner.Email, Role = BandMemberRole.Owner, JoinedAt = band.CreatedAt });
            await db.InsertAsync(new BandMember { Id = Guid.CreateVersion7(), BandId = other.Id, UserId = contributor.KeycloakId, Email = contributor.Email, Role = BandMemberRole.Owner, JoinedAt = other.CreatedAt });
            await transaction.CommitAsync();
        }
        var result = await GetBandContributorInfoHandler.Handle(new(band.Id,
            [owner.KeycloakId, contributor.KeycloakId, contributor.KeycloakId, Guid.NewGuid()]), db, default);
        Assert.Equal(2, result.Length);
        Assert.Equal("Owner", Assert.Single(result, row => row.UserId == owner.KeycloakId).Role);
        var former = Assert.Single(result, row => row.UserId == contributor.KeycloakId);
        Assert.Equal(contributor.Email, former.Email);
        Assert.Null(former.Role);
    }

    private static User Profile()
    {
        var subject = Guid.NewGuid();
        var now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        return new User { KeycloakId = subject, Email = $"{subject:N}@example.invalid", CreatedAt = now, UpdatedAt = now, LastLoginAt = default };
    }
}
