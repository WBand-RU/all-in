namespace BandService.Endpoints.Bands.Invitations;

public sealed class BandInvitationMapper
{
    public void Register(IEndpointRouteBuilder builder)
    {
        var group = builder
            .MapGroup("invitations")
            .WithTags("invitations")
            .WithDisplayName("Invitations")
            .RequireAuthorization();

        GetInvitationList.GetInvitationListEndpoint.Build(group);
        DeleteInvitation.DeleteInvitationEndpoint.Build(group);
        CreateInvitation.CreateInvitationEndpoint.Build(group);
    }
}
