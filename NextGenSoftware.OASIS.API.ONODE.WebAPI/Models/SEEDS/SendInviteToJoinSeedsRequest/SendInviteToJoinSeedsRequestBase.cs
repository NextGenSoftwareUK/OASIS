

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models
{
    public abstract class SendInviteToJoinSeedsRequestBase : SeedsKarmaBase
    {
        public int TransferQuanitity { get; set; }
        public int SowQuanitity { get; set; }
    }
}