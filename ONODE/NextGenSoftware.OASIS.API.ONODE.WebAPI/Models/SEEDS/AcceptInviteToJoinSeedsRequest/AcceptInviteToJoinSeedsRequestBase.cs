

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public abstract class AcceptInviteToJoinSeedsRequestBase : SeedsKarmaBase
    {
        public string InviteSecret { get; set; }
    }
}