using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Web9.Core.Managers;
using NextGenSoftware.OASIS.Web9.Core.Models;

namespace NextGenSoftware.OASIS.Web9.WebAPI.GraphQL
{
    /// <summary>Root Query type for the WEB9 Singularity GraphQL endpoint.</summary>
    public class Query
    {
        private static readonly SingularityAggregationManager _manager = new SingularityAggregationManager();

        /// <summary>
        /// Probes WEB4-WEB8 in parallel and returns one unified status report — the Singularity layer observing itself.
        /// </summary>
        [GraphQLDescription("Probes all OASIS layers (WEB4-WEB8) in parallel and returns a unified status report.")]
        public async Task<UnifiedStatusReport> GetUnifiedStatus()
        {
            return await _manager.GetUnifiedStatusAsync();
        }

        /// <summary>
        /// Returns the live status of a single OASIS layer by name (WEB4, WEB5, WEB6, WEB7, WEB8).
        /// Returns null if the specified layer name is not known.
        /// </summary>
        [GraphQLDescription("Returns the live probe status for a single named OASIS layer (WEB4, WEB5, WEB6, WEB7, WEB8).")]
        public async Task<LayerStatus> GetLayerStatus(string layerName)
        {
            UnifiedStatusReport report = await _manager.GetUnifiedStatusAsync();
            return report.Layers.FirstOrDefault(l =>
                string.Equals(l.LayerName, layerName, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
