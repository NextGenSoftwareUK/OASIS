using System.Threading.Tasks;
using NextGenSoftware.OASIS.Web10.Core.Models;
using NextGenSoftware.OASIS.Web9.Core.Managers;
using OASISBootLoaderClass = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;

namespace NextGenSoftware.OASIS.Web10.Core.Managers
{
    /// <summary>
    /// "The Source" / WEB0 - the ground every other layer derives from and returns to. Deliberately does not expose
    /// the raw OASIS_DNA configuration (which carries provider connection strings/keys); instead it returns the
    /// safe, public runtime/version identity of the foundation plus WEB9's live unified status across every layer
    /// built on top of it - the Alpha and the Omega, as one real endpoint.
    /// </summary>
    public class SourceManager
    {
        private readonly SingularityAggregationManager _singularityManager = new SingularityAggregationManager();

        public async Task<SourceReport> GetSourceAsync()
        {
            return new SourceReport
            {
                OasisRuntimeVersion = OASISBootLoaderClass.OASISRuntimeVersion,
                OasisApiVersion = OASISBootLoaderClass.OASISAPIVersion,
                StarApiVersion = OASISBootLoaderClass.STARAPIVersion,
                UnifiedStatus = await _singularityManager.GetUnifiedStatusAsync()
            };
        }
    }
}
