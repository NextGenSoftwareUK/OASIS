

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models
{
    public abstract class AcceptInviteToJoinSeedsRequestBase : SeedsKarmaBase
    {
        public string InviteSecret { get; set; }
    }
}