
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class SaveHolonRequest : HolonWriteIntentRequest
    {
        public Holon Holon { get; set; }
        /// <summary>Optional exact top-level metadata keys holding UTC timestamps or null.</summary>
        public string[] UtcMetadataKeys { get; set; }
        public string OnChainProvider { get; set; }
        public string OffChainProvider { get; set; }
    }
}