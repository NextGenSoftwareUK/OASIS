

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public abstract class PayWithSeedsRequestBase : SeedsKarmaBase
    {
        public float Quanitity { get; set; }
        public string Memo { get; set; }
    }
}