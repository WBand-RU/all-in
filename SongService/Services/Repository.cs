using MongoDB.Driver;
using Shared;

namespace SongService.Services;

public sealed class Repository(IMongoDatabase database)
{
    // public IMongoCollection<OrganizationApplication> OrganizationApplications =>
    //     database.GetCollection<OrganizationApplication>("organization_applications");

    // public IMongoCollection<Organization> Organizations =>
    //     database.GetCollection<Organization>("organizations");

    // public IMongoCollection<Member> Members => database.GetCollection<Member>("members");

    // public IMongoCollection<Invitation> Invitations =>
    //     database.GetCollection<Invitation>("invitations");
}
