
using System;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models
{
    public class AcceptInviteToJoinSeedsUsingAvatarRequest : AcceptInviteToJoinSeedsRequestBase
    {
        public Guid AvatarId { get; set; }
    }
}