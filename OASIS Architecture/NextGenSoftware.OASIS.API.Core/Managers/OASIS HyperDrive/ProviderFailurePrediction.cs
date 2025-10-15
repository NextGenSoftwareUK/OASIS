using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Provider failure prediction
    /// </summary>
    public class ProviderFailurePrediction
    {
        public ProviderType ProviderType { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public double FailureProbability { get; set; }
        public DateTime PredictedFailureTime { get; set; }
        public double Confidence { get; set; }
        public List<string> RiskFactors { get; set; } = new List<string>();
        public List<string> RecommendedActions { get; set; } = new List<string>();
    }
}
