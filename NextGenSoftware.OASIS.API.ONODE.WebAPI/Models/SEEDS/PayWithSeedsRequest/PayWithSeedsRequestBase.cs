

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models
{
    public abstract class PayWithSeedsRequestBase : SeedsKarmaBase
    {
        public int Quanitity { get; set; }
        public string Memo { get; set; }
    }
}