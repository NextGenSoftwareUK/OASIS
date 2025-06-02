using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects
{
    public interface IPurchaseOlandResponse
    {
        List<Guid> OlandIds { get; set; }
        Guid OLandPurchaseId { get; set; }
        string TransactionHash { get; set; }
    }
}