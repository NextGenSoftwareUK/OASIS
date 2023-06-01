using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONode.Core.Objects
{
    public class PurchaseOlandResponse
    {
        public string PurchaseResult { get; set; }

        public PurchaseOlandResponse(string purchaseResult)
        {
            PurchaseResult = purchaseResult;
        }
    }
}
