
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class LoadHolonRequest : BaseLoadHolonRequest
    {
        public Guid Id { get; set; }

        /// <summary>Optional HyperDrive V2 strategy for this point read.</summary>
        public string LoadBalancingStrategy { get; set; }
    }
}