using BandService.Domain;
using MongoDB.Driver;
using Shared;

namespace BandService.Services;

public sealed class Repository(IMongoDatabase database)
{
    public IMongoCollection<Band> Bands => database.GetCollection<Band>("bands");

    public IMongoCollection<Invitation> Invitations =>
        database.GetCollection<Invitation>("invitations");

    public IMongoCollection<Member> Members => database.GetCollection<Member>("members");

    public Task<IClientSessionHandle> StartSessionAsync() => database.Client.StartSessionAsync();
}
