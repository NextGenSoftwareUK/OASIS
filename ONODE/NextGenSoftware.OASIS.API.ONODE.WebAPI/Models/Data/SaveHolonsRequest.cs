
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class SaveHolonsRequest : HolonWriteIntentRequest
    {
        public List<Holon> Holons { get; set; }
    }
}
