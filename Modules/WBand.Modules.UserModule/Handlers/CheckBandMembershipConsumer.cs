namespace WBand.Modules.UserModule.Handlers;

//public class CheckBandMembershipConsumer()
//{
//    public async Task<CheckBandMembershipResponse> Consume(
//        CheckBandMembershipRequest request,
//        Repository repository
//    )
//    {
//        var isMember = await repository
//            .Members.Find(m => m.UserId == request.UserId && m.BandId == request.BandId)
//            .AnyAsync();

//        return new CheckBandMembershipResponse(isMember);
//    }
//}
