
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class SaveHolonRequest : BaseHolonRequest
    {
        public Holon Holon { get; set; }
        public bool SaveChildren { get; set; } = true;
        public string OnChainProvider { get; set; }
        public string OffChainProvider { get; set; }
    }
}