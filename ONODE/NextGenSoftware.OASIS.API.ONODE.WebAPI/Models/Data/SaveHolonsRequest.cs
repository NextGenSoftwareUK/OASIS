
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class SaveHolonsRequest : BaseHolonRequest
    {
        public List<Holon> Holons { get; set; }
        public bool SaveChildren { get; set; } = true;
    }
}
