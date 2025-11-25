using BandService.Domain;

namespace BandService.Endpoints.Bands.Members.UpdateMemberRole;

public sealed record UpdateMemberRoleRequest(MemberRole NewRole);
