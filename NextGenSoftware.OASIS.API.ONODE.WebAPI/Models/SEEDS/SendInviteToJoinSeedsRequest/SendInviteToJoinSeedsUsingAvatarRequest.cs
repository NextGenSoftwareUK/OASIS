
using System;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models
{
    public class SendInviteToJoinSeedsUsingAvatarRequest : SendInviteToJoinSeedsRequestBase
    {
        public Guid SponsorAvatarId { get; set; }
        public Guid RefererAvatarId { get; set; }
    }
}