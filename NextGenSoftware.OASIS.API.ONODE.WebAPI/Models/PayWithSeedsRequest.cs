
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public class PayWithSeedsRequest
    {
        public string FromTelosAccountName { get; set; }
        public string ToTelosAccountName { get; set; }

        public float Qty { get; set; }

        public string Memo { get; set; }
    }
}