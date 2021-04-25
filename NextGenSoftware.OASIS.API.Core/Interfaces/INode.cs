using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface INode
    {
        Guid Id { get; set; }
        Guid ParentId { get; set; }
        IHolon Parent { get; set; }
        string NodeName { get; set; }
        NodeType NodeType { get; set; }
    }
}