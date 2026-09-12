using System;
using NextGenSoftware.OASIS.Web8.Core.Enums;

namespace NextGenSoftware.OASIS.Web8.Core.Models
{
    /// <summary>A registered node in the WEB8 mesh - any external system that can receive a relayed MeshMessage at its EndpointUrl.</summary>
    public class GalacticNode
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public NodeType Type { get; set; }

        /// <summary>The HTTP endpoint this node receives relayed mesh messages at.</summary>
        public string EndpointUrl { get; set; }

        /// <summary>Every node retains full sovereignty within the mesh - it can leave/be excluded from routing at any time.</summary>
        public bool IsSovereign { get; set; } = true;

        public DateTime RegisteredUtc { get; set; } = DateTime.UtcNow;

        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>A bidirectional link (mesh edge) between two nodes, carrying its measured latency for shortest-path routing.</summary>
    public class MeshLink
    {
        public Guid Id { get; set; }

        public Guid NodeAId { get; set; }

        public Guid NodeBId { get; set; }

        public double LatencyMs { get; set; } = 50;

        public bool IsActive { get; set; } = true;
    }
}
