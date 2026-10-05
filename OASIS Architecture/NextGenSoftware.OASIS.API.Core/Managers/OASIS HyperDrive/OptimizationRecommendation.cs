using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive
{
    /// <summary>
    /// Represents an AI-powered optimization recommendation for HyperDrive
    /// </summary>
    public class OptimizationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public OptimizationType Type { get; set; }
        public PriorityLevel Priority { get; set; }
        public decimal EstimatedImpact { get; set; }
        public decimal EstimatedCost { get; set; }
        public List<ProviderType> AffectedProviders { get; set; } = new List<ProviderType>();
        public List<string> ImplementationSteps { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool IsImplemented { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public string CreatedBy { get; set; }
        public string Reason { get; set; }
        public List<string> Prerequisites { get; set; } = new List<string>();
        public decimal ConfidenceScore { get; set; }
    }

    /// <summary>
    /// Types of optimization recommendations
    /// </summary>
    public enum OptimizationType
    {
        Performance,
        Cost,
        Reliability,
        Security,
        Scalability,
        Latency,
        Throughput,
        ResourceUtilization,
        FailoverOptimization,
        ReplicationOptimization,
        LoadBalancing,
        Caching,
        DataCompression,
        NetworkOptimization
    }

    /// <summary>
    /// Priority levels for optimization recommendations
    /// </summary>
    public enum PriorityLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
        Emergency = 5
    }
}
