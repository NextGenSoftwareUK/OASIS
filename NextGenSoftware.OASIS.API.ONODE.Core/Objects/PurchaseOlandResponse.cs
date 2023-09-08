using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONode.Core.Objects
{
    public class PurchaseOlandResponse
    {
        public string TransactionHash { get; set; }
        public List<Guid> OlandIds { get; set; }
        public Guid OLandPurchaseId { get; set; }
    }
}
