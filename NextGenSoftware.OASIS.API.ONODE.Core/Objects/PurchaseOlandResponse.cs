using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Objects
{
    public class PurchaseOlandResponse : IPurchaseOlandResponse
    {
        public string TransactionHash { get; set; }
        public List<Guid> OlandIds { get; set; }
        public Guid OLandPurchaseId { get; set; }
    }
}