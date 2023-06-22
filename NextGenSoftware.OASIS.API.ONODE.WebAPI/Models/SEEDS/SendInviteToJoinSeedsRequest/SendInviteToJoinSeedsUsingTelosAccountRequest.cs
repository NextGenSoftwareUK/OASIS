
namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models
{
    public class SendInviteToJoinSeedsUsingTelosAccountRequest : SendInviteToJoinSeedsRequestBase
    {
        public string SponsorTelosAccountName { get; set; }
        public string SponsorTelosAccountPrivateKey { get; set; }
        public string RefererTelosAccountName { get; set; }
    }
}