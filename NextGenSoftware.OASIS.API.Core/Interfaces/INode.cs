namespace NextGenSoftware.OASIS.API.Core
{
    public interface INode
    {
        string NodeName { get; set; }
        NodeType NodeType { get; set; }
    }
}