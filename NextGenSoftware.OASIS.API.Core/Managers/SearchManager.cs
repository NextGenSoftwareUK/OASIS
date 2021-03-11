using System.Collections.Generic;
using System.Threading.Tasks;

using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class SearchManager : OASISManager
    {
       // private SearchManagerConfig _config;

        public List<IOASISStorage> OASISStorageProviders { get; set; }
        

       //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public SearchManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        {

        }

        public async Task<ISearchResults> SearchAsync(ISearchParams searchParams, ProviderType provider = ProviderType.Default)
        {
            return await ((IOASISStorage)ProviderManager.SetAndActivateCurrentStorageProvider(provider)).SearchAsync(searchParams);
        }

    }
}
