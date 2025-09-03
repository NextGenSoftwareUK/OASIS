using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class MetaHolonTag : IMetaHolonTag
    {
        public string HolonName { get; set; }
        //public HolonType HolonType { get; set; }
        public string NodeName { get; set; }
        //[JsonObjectAttribute]
        //public EnumValue<NodeType> NodeType { get; set; } //TODO: Work out how to seriliaze EnumValue later! ;-)
        public string NodeType { get; set; }
        public string MetaTag { get; set; }
    }
}
