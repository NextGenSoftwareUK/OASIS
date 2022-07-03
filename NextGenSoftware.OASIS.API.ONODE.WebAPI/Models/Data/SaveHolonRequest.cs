
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class SaveHolonRequest : OASISRequest
    {
        public Holon Holon { get; set; }
        public string OnChainProvider { get; set; }
        public string OffChainProvider { get; set; }
    }
}