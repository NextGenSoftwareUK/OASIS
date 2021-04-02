

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public abstract class SendInviteToJoinSeedsRequestBase : SeedsKarmaBase
    {
        public float TransferQuanitity { get; set; }
        public float SowQuanitity { get; set; }
    }
}