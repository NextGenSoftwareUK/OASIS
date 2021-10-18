using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
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
            return await ((IOASISStorage)ProviderManager.SetAndActivateCurrentStorageProvider(provider)).SearchAsync(searchParams);
        }
    }
}