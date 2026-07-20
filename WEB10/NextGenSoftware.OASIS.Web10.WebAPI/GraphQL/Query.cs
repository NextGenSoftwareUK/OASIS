using System.Threading.Tasks;
using NextGenSoftware.OASIS.Web10.Core.Managers;
using NextGenSoftware.OASIS.Web10.Core.Models;
using NextGenSoftware.OASIS.Web9.Core.Models;

namespace NextGenSoftware.OASIS.Web10.WebAPI.GraphQL
{
    /// <summary>
    /// Root GraphQL Query type for the WEB10 Source layer.
    /// "The Omega that is the Alpha" — the foundation of the entire OASIS stack.
    /// </summary>
    public class Query
    {
        private static readonly SourceManager _manager = new SourceManager();

        /// <summary>
        /// Returns the full WEB10 source report: OASIS runtime/version identity plus
        /// the live unified status across every layer (WEB4-WEB8).
        /// </summary>
        [GraphQLDescription("Returns the WEB10 source report — runtime versions and live unified ecosystem status.")]
        public async Task<SourceReport> GetSource() =>
            await _manager.GetSourceAsync();

        /// <summary>
        /// Alias for GetSource — returns the complete source report.
        /// </summary>
        [GraphQLDescription("Alias for GetSource. Returns the complete WEB10 source report.")]
        public async Task<SourceReport> GetSourceReport() =>
            await _manager.GetSourceAsync();

        /// <summary>
        /// Returns the live unified health status of every OASIS layer above the foundation.
        /// </summary>
        [GraphQLDescription("Returns the live unified health status across WEB4-WEB8 as reported by WEB9.")]
        public async Task<UnifiedStatusReport> GetUnifiedStatus()
        {
            SourceReport report = await _manager.GetSourceAsync();
            return report.UnifiedStatus;
        }
    }
}
