
namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models
{
    public class PayWithSeedsUsingTelosAccountRequest : PayWithSeedsRequestBase
    {
        public string FromTelosAccountName { get; set; }
        public string FromTelosAccountPrivateKey { get; set; }
        public string ToTelosAccountName { get; set; }
    }
}