
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Core
{
    public enum ProviderCat
    {
        Storage,
        Network,
        Renderer
    }

    public enum ProviderType
    {
        HoloOASIS,
        EthereumOASIS,
        EOSOASIS,
        LoonOASIS,
        StellarOASIS,
        BlockStackOASIS,
        SOLIDOASIS,
        IPFSOASIS,
        ActivityPubOASIS,
        ScuttleBugOASIS,
        All,
        None
    }

    public class ProviderManager
    {
        public ProviderType CurrentProvider { get; private set; }
        
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

        public void SwitchProvider(ProviderType provider)
        {
            this.CurrentProvider = provider;
        }

        public void ActivateProvider(ProviderType type)
        {
            IOASISProvider provider = RegisteredProviders.FirstOrDefault(x => x.Type == type);

            if (provider != null)
            {
                provider.ActivateProvider();
            }
        }
    }
}
