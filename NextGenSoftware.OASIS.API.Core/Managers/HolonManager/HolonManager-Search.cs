using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        public OASISResult<IEnumerable<IHolon>> SearchHolons(string searchTerm, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            //TODO: Finish implementing a wraparound of SearchManager here...
            SearchManager.Instance.SearchAsync(new SearchParams() {  })
            return new OASISResult<IEnumerable<IHolon>>();
        }

        public Task<OASISResult<IEnumerable<IHolon>>> SearchHolonsAsync(string searchTerm, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            //TODO: Finish implementing a wraparound of SearchManager here...
            //SearchManager.Instance.SearchAsync()
            return null;
        }

        public OASISResult<IEnumerable<T>> SearchHolons<T>(string searchTerm, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            //TODO: Finish implementing a wraparound of SearchManager here...
            //SearchManager.Instance.SearchAsync()
            return new OASISResult<IEnumerable<T>>();
        }

        public Task<OASISResult<IEnumerable<T>>> SearchHolonsAsync<T>(string searchTerm, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            //TODO: Finish implementing a wraparound of SearchManager here...
            //SearchManager.Instance.SearchAsync()
            return null;
        }
    }
} 