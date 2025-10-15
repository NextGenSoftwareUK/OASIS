using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Failure prediction result
    /// </summary>
    public class FailoverPrediction
    {
        public DateTime Timestamp { get; set; }
        public List<ProviderFailurePrediction> Predictions { get; set; } = new List<ProviderFailurePrediction>();
        public List<string> RecommendedActions { get; set; } = new List<string>();
        public RiskLevel RiskLevel { get; set; }
    }
}
