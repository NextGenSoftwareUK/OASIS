
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public class SendInviteToJoinSeedsUsingTelosAccountRequest : SendInviteToJoinSeedsRequestBase
    {
        public string SponsorTelosAccountName { get; set; }
        public string SponsorTelosAccountPrivateKey { get; set; }
        public string RefererTelosAccountName { get; set; }
    }
}