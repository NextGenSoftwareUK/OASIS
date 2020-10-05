using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISManager
    {        
        public Task<ISearch> LoadSearchAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        //Events
        public delegate void OASISManagerError(object sender, OASISErrorEventArgs e);
        public event OASISManagerError OnOASISManagerError;

        public delegate void StorageProviderError(object sender, OASISErrorEventArgs e);

       //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public OASISManager(IOASISStorage OASISStorageProvider)
        {
            if (OASISStorageProvider != null)
            {
                ProviderManager.SetAndActivateCurrentStorageProvider(OASISStorageProvider);
                OASISStorageProvider.StorageProviderError += OASISStorageProvider_StorageProviderError;
            }

            //TODO: Need to unsubscribe events to stop memory leaks...
        }

        //TODO: {URGENT} Move this into ProviderManager ASAP!
        protected IOASISStorage SetAndActivateCurrentStorageProvider(ProviderType providerType)
        {
            if (providerType == ProviderType.Default)
                return ProviderManager.SetAndActivateCurrentStorageProvider();
            else
                return ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
        }

        private void OASISStorageProvider_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            OnOASISManagerError?.Invoke(this, new OASISErrorEventArgs() { ErrorDetails = e.ErrorDetails, Reason = e.Reason });
        }
    }
}
