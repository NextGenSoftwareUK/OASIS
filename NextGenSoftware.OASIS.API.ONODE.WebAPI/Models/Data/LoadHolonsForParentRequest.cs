
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class LoadHolonsForParentRequest : BaseLoadHolonRequest
    {
        public Guid Id { get; set; }
        public string HolonType { get; set; }
    }
}