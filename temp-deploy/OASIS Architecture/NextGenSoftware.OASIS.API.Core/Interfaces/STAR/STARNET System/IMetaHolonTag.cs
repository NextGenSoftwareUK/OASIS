using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{ 
    public interface IMetaHolonTag
    {
        string HolonName { get; set; }
        //HolonType HolonType { get; set; }
        string MetaTag { get; set; }
        string NodeName { get; set; }
        //EnumValue<NodeType> NodeType { get; set; } //TODO: Work out how to seriliaze EnumValue later! ;-)
        string NodeType { get; set; }
    }
}