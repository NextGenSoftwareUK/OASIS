
using System;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models
{
    public class PayWithSeedsUsingAvatarRequest : PayWithSeedsRequestBase
    {
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
    }
}