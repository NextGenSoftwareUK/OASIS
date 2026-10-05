using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET
{
    public interface IMetaTagMappings
    {
        List<MetaHolonTag> MetaHolonTags { get; set; }
        Dictionary<string, string> MetaTags { get; set; }
    }
}