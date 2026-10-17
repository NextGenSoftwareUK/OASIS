using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web9.Core.Models
{
    /// <summary>The live, real-time status of a single OASIS layer (WEB4-WEB8), as observed by an actual HTTP probe.</summary>
    public class LayerStatus
    {
        public string LayerName { get; set; }

        public string BaseUrl { get; set; }

        public bool IsReachable { get; set; }

        public double ResponseTimeMs { get; set; }

        /// <summary>Cheap, real metrics pulled from the layer's own endpoints where available, e.g. "registeredAgents": "3".</summary>
        public Dictionary<string, string> Metrics { get; set; } = new Dictionary<string, string>();

        public string Error { get; set; }

        public DateTime CheckedUtc { get; set; } = DateTime.UtcNow;
    }

    /// <summary>The aggregate, unified view across every probed layer - "the network observing itself".</summary>
    public class UnifiedStatusReport
    {
        public List<LayerStatus> Layers { get; set; } = new List<LayerStatus>();

        public bool AllLayersHealthy { get; set; }

        public int HealthyLayerCount { get; set; }

        public int TotalLayerCount { get; set; }

        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
    }
}
