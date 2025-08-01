
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public class PayWithSeedsUsingAvatarRequest : PayWithSeedsRequestBase
    {
        public Guid FromAvatarId { get; set; }
        public Guid ToAvatarId { get; set; }
    }
}