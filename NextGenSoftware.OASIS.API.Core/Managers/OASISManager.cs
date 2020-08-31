using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISManager
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
        public delegate void OASISManagerError(object sender, OASISErrorEventArgs e);
        public event OASISManagerError OnOASISManagerError;

        public delegate void StorageProviderError(object sender, OASISErrorEventArgs e);

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
        public OASISManager(IOASISStorage OASISStorageProvider)
        {
            if (!ProviderManager.IsProviderRegistered(OASISStorageProvider))
                ProviderManager.RegisterProvider(OASISStorageProvider);

            ProviderManager.SwitchCurrentStorageProvider(OASISStorageProvider.ProviderType);
        }

        private void OASISStorageProvider_OnStorageProviderError(object sender, OASISErrorEventArgs e)
        {
            //TODO: Not sure if we need to have a OnSearchManagerError as well as the StorageProvider Error Event?
            OnOASISManagerError?.Invoke(this, e);
        }
    }
}
