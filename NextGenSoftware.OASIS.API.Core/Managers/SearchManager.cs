using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class SearchManager : OASISManager
    {
       // private SearchManagerConfig _config;

        public List<IOASISStorage> OASISStorageProviders { get; set; }
        
        public Task<ISearch> LoadSearchAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        //public SearchManagerConfig Config
        //{
        //    get
        //    {
        //        if (_config == null)
        //        {
        //            _config = new SearchManagerConfig();
        //        }

        //        return _config;
        //    }
        //}

        //Events
        //public delegate void SearchManagerError(object sender, SearchManagerErrorEventArgs e);
        //public event SearchManagerError OnSearchManagerError;

        //public delegate void StorageProviderError(object sender, SearchManagerErrorEventArgs e);

        /*
        public SearchManager(List<IOASISStorage> OASISStorageProviders)
        {
            this.OASISStorageProviders = OASISStorageProviders;

            foreach (IOASISStorage provider in OASISStorageProviders)
            {
                provider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;
                provider.ActivateProvider();
            }
        }*/

       //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public SearchManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
        {
            if (!ProviderManager.IsProviderRegistered(OASISStorageProvider))
                ProviderManager.RegisterProvider(OASISStorageProvider);

            ProviderManager.SetAndActivateCurrentStorageProvider(OASISStorageProvider.ProviderType);
        }

        //private void OASISStorageProvider_OnStorageProviderError(object sender, SearchManagerErrorEventArgs e)
        //{
        //    //TODO: Not sure if we need to have a OnSearchManagerError as well as the StorageProvider Error Event?
        //    OnSearchManagerError?.Invoke(this, e);
        //}

        public async Task<ISearchResults> SearchAsync(string searchTerm, ProviderType provider = ProviderType.Default)
        {
            return await ((IOASISStorage)ProviderManager.SetAndActivateCurrentStorageProvider(provider)).SearchAsync(searchTerm);
        }

    }
}
