using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Failure prediction model
    /// </summary>
    public class FailurePredictionModel
    {
        public List<FailureEvent> HistoricalFailures { get; set; } = new List<FailureEvent>();
        public double BaseFailureRate { get; set; } = 0.01;
        public Dictionary<string, double> RiskFactors { get; set; } = new Dictionary<string, double>();

        public void UpdateModel(FailureEvent failureEvent)
        {
            HistoricalFailures.Add(failureEvent);
            
            // Update base failure rate
            BaseFailureRate = HistoricalFailures.Count / (double)Math.Max(1, HistoricalFailures.Count);

            // Update risk factors based on failure patterns
            UpdateRiskFactors(failureEvent);
        }

        private void UpdateRiskFactors(FailureEvent failureEvent)
        {
            // Simple risk factor update based on failure patterns
            // In a real implementation, this would use machine learning
            var factorKey = $"{failureEvent.FailureType}_{failureEvent.Cause}";
            
            if (RiskFactors.ContainsKey(factorKey))
            {
                RiskFactors[factorKey] += 0.1;
            }
            else
            {
                RiskFactors[factorKey] = 0.1;
            }
        }
    }
}
