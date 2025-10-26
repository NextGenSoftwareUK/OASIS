
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class Node : INode
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public IHolon Parent { get; set; }
        public string NodeName { get; set; }
        public NodeType NodeType { get; set; }
    }
}