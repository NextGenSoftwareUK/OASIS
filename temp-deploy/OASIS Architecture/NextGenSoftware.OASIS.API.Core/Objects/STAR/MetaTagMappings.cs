using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET
{
    public class MetaTagMappings : IMetaTagMappings
    {
        public List<MetaHolonTag> MetaHolonTags { get; set; } = new List<MetaHolonTag>();
        public Dictionary<string, string> MetaTags { get; set; } = new Dictionary<string, string>();
    }
}