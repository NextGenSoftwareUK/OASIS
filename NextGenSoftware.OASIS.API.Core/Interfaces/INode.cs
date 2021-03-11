using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface INode
    {
        string NodeName { get; set; }
        NodeType NodeType { get; set; }
    }
}