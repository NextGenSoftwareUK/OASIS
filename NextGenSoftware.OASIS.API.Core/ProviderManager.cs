
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Core
{
    public class ProviderManager
    {
        public static ProviderType CurrentStorageProviderType { get; private set; }

        public static IOASISStorage CurrentStorageProvider { get; private set; } //TODO: Need to work this out because in future there can be more than one provider active at a time (even more than one storage provider)

        public List<IOASISProvider> RegisteredProviders { get; set; }

        public void RegisterProvider(IOASISProvider provider)
        {
            RegisteredProviders.Add(provider);
        }

        public void UnRegisterProvider(IOASISProvider provider)
        {
            RegisteredProviders.Remove(provider);
        }

        public List<IOASISProvider> GetProvidersOfCat(ProviderCat category)
        {
            return RegisteredProviders.Where(x => x.Category == category).ToList();
        }

        public List<IOASISStorage> GetStorageProviders()
        {
            List<IOASISStorage> storageProviders = new List<IOASISStorage>();

            foreach (IOASISProvider provider in RegisteredProviders.Where(x => x.Category == ProviderCat.Storage).ToList())
                storageProviders.Add((IOASISStorage)provider);  

            return storageProviders;
        }

        //TODO: In future more than one StorageProvider will be active at a time so we need to work out how to handle this...
        public void SwitchStorageProvider(ProviderType providerType)
        {
            IOASISProvider provider = RegisteredProviders.FirstOrDefault(x => x.Type == providerType);

            if (provider != null && provider.Category == ProviderCat.Storage)
            {
                ProviderManager.CurrentStorageProviderType = providerType;
                ProviderManager.CurrentStorageProvider = (IOASISStorage)provider;
            }
        }

        public void ActivateProvider(ProviderType type)
        {
            IOASISProvider provider = RegisteredProviders.FirstOrDefault(x => x.Type == type);

            if (provider != null)
                provider.ActivateProvider();
        }

        public void DeaAtivateProvider(ProviderType type)
        {
            IOASISProvider provider = RegisteredProviders.FirstOrDefault(x => x.Type == type);

            if (provider != null)
                provider.DeActivateProvider();
        }
    }
}
