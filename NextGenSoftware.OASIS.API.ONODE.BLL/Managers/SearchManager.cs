using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
{
    public class SearchManager : OASISManager, ISearchManager
    {
        public SearchManager() : base()
        {

        }

        public async Task<ISearchResults> SearchAsync(ISearchParams searchParams, ProviderType provider = ProviderType.Default)
        {
            OASISResult<ISearchResults> result = await ((IOASISStorageProvider)ProviderManager.SetAndActivateCurrentStorageProvider(provider)).SearchAsync(searchParams);
            return result.Result;
        }
    }
}