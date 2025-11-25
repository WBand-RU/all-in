namespace BandService.Endpoints.Bands.Members;

public sealed class BandMemberMapper
{
    public void Register(IEndpointRouteBuilder builder)
    {
        var group = builder
            .MapGroup("members")
            .WithTags("members")
            .WithDisplayName("Members")
            .RequireAuthorization();

        GetMembers.GetMembersEndpoint.Build(group);
        UpdateMemberRole.UpdateMemberRoleEndpoint.Build(group);
        RemoveMember.RemoveMemberEndpoint.Build(group);
    }
}
