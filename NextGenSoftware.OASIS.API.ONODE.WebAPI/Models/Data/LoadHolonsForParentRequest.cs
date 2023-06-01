
using System;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models.Data
{
    public class LoadHolonsForParentRequest : BaseLoadHolonRequest
    {
        public Guid Id { get; set; }
        public string HolonType { get; set; }
    }
}