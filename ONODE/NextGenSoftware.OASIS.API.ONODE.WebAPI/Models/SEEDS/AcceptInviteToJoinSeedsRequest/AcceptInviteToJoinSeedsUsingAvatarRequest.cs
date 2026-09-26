
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public class AcceptInviteToJoinSeedsUsingAvatarRequest : AcceptInviteToJoinSeedsRequestBase
    {
        public Guid AvatarId { get; set; }
    }
}