using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web8.Core.Models
{
    /// <summary>A message to be routed across the mesh from a source node to a destination node, multi-hop if needed.</summary>
    public class MeshMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SourceNodeId { get; set; }

        public Guid DestinationNodeId { get; set; }

        public string Payload { get; set; }

        /// <summary>Time-to-live - the maximum number of hops before the message is dropped (mirrors IP TTL, prevents routing loops).</summary>
        public int Ttl { get; set; } = 16;
    }

    /// <summary>The result of routing/relaying a MeshMessage - the computed path and per-hop relay outcomes.</summary>
    public class MeshRouteResult
    {
        public Guid MessageId { get; set; }

        public List<Guid> Path { get; set; } = new List<Guid>();

        public double TotalLatencyMs { get; set; }

        public bool Delivered { get; set; }

        public List<string> RelayLog { get; set; } = new List<string>();
    }
}
