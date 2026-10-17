using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Web9.WebAPI.GraphQL
{
    /// <summary>Root Mutation type for the WEB9 Singularity GraphQL endpoint.</summary>
    public class Mutation
    {
        /// <summary>
        /// Triggers a manual convergence probe across all OASIS layers and returns a summary.
        /// This is a stub — full convergence orchestration logic can be wired to SingularityAggregationManager
        /// once a dedicated convergence-trigger surface is exposed by the Core layer.
        /// </summary>
        [GraphQLDescription("Triggers a manual convergence probe across all OASIS layers (WEB4-WEB8). Returns a summary string.")]
        public Task<string> TriggerConvergence()
        {
            string timestamp = DateTime.UtcNow.ToString("o");
            return Task.FromResult(
                $"Convergence probe requested at {timestamp}. " +
                "Use GetUnifiedStatus to retrieve the live results once the probe completes.");
        }
    }
}
