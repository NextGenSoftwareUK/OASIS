
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class LoadHolonsByMetaDataRequest : BaseLoadHolonRequest
    {
        public string MetaKey { get; set; }
        public string MetaValue { get; set; }
        public string HolonType { get; set; } = "All";
        // Multi-key variant (optional; used instead of MetaKey/MetaValue when provided)
        public Dictionary<string, string> MetaKeyValuePairs { get; set; }
        public string MetaKeyValuePairMatchMode { get; set; } = "All";
        /// <summary>When true also returns holons from other avatars that are explicitly marked IsPublic=true. Defaults to false.</summary>
        public bool IncludePublic { get; set; } = false;
    }
}
