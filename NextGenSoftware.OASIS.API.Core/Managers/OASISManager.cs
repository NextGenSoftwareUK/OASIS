using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISManager
    {
     //   private IOASISStorage _currentOASISStorageProvider;

        //public IOASISStorage CurrentOASISStorageProvider
        //{
        //    get
        //    {
        //        return _currentOASISStorageProvider;
        //    }
        //}

      //  public List<IOASISStorage> OASISStorageProviders { get; set; }
        
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
            if (OASISStorageProvider != null)
            {
                ProviderManager.SwitchCurrentStorageProvider(OASISStorageProvider);
                //OASISStorageProvider.StorageProviderError += OASISStorageProvider_StorageProviderError;
                OASISStorageProvider.StorageProviderError += OASISStorageProvider_StorageProviderError;
            }

            //TODO: Need to unsubscribe events to stop memory leaks...
        }


        private void OASISStorageProvider_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            OnOASISManagerError?.Invoke(this, new OASISErrorEventArgs() { ErrorDetails = e.ErrorDetails, Reason = e.Reason });
        }

        //public void SetOASISStorageProvider(IOASISStorage OASISStorageProvider)
        //{
        //    if (OASISStorageProvider != null)
        //    {
        //        if (!ProviderManager.IsProviderRegistered(OASISStorageProvider))
        //            ProviderManager.RegisterProvider(OASISStorageProvider);

        //        ProviderManager.SwitchCurrentStorageProvider(OASISStorageProvider.ProviderType);
        //        //_currentOASISStorageProvider = OASISStorageProvider;
        //    }
        //}

        protected IOASISStorage GetOASISStorageProvider(ProviderType providerType = ProviderType.Default)
        {
            //   IOASISProvider provider = null;
            //if (providerType != ProviderType.Default)
          //  if (providerType != null)
          //  {
                if (providerType == ProviderType.Default)
                    providerType = ProviderManager.CurrentStorageProviderType;

                IOASISProvider provider = ProviderManager.GetAndActivateProvider(providerType);

                if (provider != null)
                    return (IOASISStorage)provider;

                throw new InvalidOperationException(string.Concat(Enum.GetName(typeof(ProviderType), providerType), " ProviderType is not registered. Please call SetOASISStorageProvider() method to register the provider before calling this method."));
        //    }

            return null;
        }


        //private void OASISStorageProvider_OnStorageProviderError(object sender, OASISErrorEventArgs e)
        //{
        //    //TODO: Not sure if we need to have a OnSearchManagerError as well as the StorageProvider Error Event?
        //    OnOASISManagerError?.Invoke(this, e);
        //}
    }
}
