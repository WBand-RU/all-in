namespace BandService.Endpoints.User;

public sealed class UserMapper
{
    public void Register(IEndpointRouteBuilder builder)
    {
        var group = builder
            .MapGroup("user")
            .WithTags("User")
            .WithDisplayName("User")
            .RequireAuthorization();

        Invitations.GetMyInvitations.GetMyInvitationsEndpoint.Build(group);
        Invitations.RespondInvitation.RespondInvitationEndpoint.Build(group);
        Invitations.GetMyInvitationsCount.GetMyInvitationsCountEndpoint.Build(group);
    }
}
