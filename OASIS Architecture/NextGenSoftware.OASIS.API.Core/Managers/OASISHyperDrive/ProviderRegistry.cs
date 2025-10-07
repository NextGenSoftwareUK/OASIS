using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Manages provider registration, discovery, and basic provider operations
    /// </summary>
    public class ProviderRegistry
    {
        private static ProviderRegistry _instance;
        private static readonly object _lock = new object();

        public static ProviderRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ProviderRegistry();
                    }
                }
                return _instance;
            }
        }

        private ProviderRegistry() { }

        // Provider Lists
        public List<EnumValue<ProviderType>> ProviderAutoFailOverList { get; set; } = new List<EnumValue<ProviderType>>();
        public List<EnumValue<ProviderType>> ProviderAutoReplicationList { get; set; } = new List<EnumValue<ProviderType>>();
        public List<EnumValue<ProviderType>> ProviderAutoLoadBalanceList { get; set; } = new List<EnumValue<ProviderType>>();

        // Current Providers
        public EnumValue<ProviderType> CurrentStorageProviderType { get; set; } = new EnumValue<ProviderType>(ProviderType.Default);
        public EnumValue<ProviderType> CurrentNetworkProviderType { get; set; } = new EnumValue<ProviderType>(ProviderType.Default);
        public EnumValue<ProviderType> CurrentKeyManagerProviderType { get; set; } = new EnumValue<ProviderType>(ProviderType.Default);
        public EnumValue<ProviderType> CurrentSearchProviderType { get; set; } = new EnumValue<ProviderType>(ProviderType.Default);
        public EnumValue<ProviderType> CurrentNFTProviderType { get; set; } = new EnumValue<ProviderType>(ProviderType.Default);
        public EnumValue<ProviderType> CurrentMapProviderType { get; set; } = new EnumValue<ProviderType>(ProviderType.Default);

        // Provider Instances
        public Dictionary<ProviderType, IOASISStorageProvider> StorageProviders { get; set; } = new Dictionary<ProviderType, IOASISStorageProvider>();
        public Dictionary<ProviderType, IOASISNetworkProvider> NetworkProviders { get; set; } = new Dictionary<ProviderType, IOASISNetworkProvider>();
        public Dictionary<ProviderType, IOASISKeyManagerProvider> KeyManagerProviders { get; set; } = new Dictionary<ProviderType, IOASISKeyManagerProvider>();
        public Dictionary<ProviderType, IOASISSearchProvider> SearchProviders { get; set; } = new Dictionary<ProviderType, IOASISSearchProvider>();
        public Dictionary<ProviderType, IOASISNFTProvider> NFTProviders { get; set; } = new Dictionary<ProviderType, IOASISNFTProvider>();
        public Dictionary<ProviderType, IOASISMapProvider> MapProviders { get; set; } = new Dictionary<ProviderType, IOASISMapProvider>();

        /// <summary>
        /// Gets all available providers for a specific type
        /// </summary>
        public List<EnumValue<ProviderType>> GetAvailableProviders(ProviderCategory category = ProviderCategory.All)
        {
            var providers = new List<EnumValue<ProviderType>>();

            switch (category)
            {
                case ProviderCategory.Storage:
                    providers.AddRange(StorageProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    break;
                case ProviderCategory.Network:
                    providers.AddRange(NetworkProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    break;
                case ProviderCategory.KeyManager:
                    providers.AddRange(KeyManagerProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    break;
                case ProviderCategory.Search:
                    providers.AddRange(SearchProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    break;
                case ProviderCategory.NFT:
                    providers.AddRange(NFTProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    break;
                case ProviderCategory.Map:
                    providers.AddRange(MapProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    break;
                case ProviderCategory.All:
                default:
                    providers.AddRange(StorageProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    providers.AddRange(NetworkProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    providers.AddRange(KeyManagerProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    providers.AddRange(SearchProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    providers.AddRange(NFTProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    providers.AddRange(MapProviders.Keys.Select(k => new EnumValue<ProviderType>(k)));
                    break;
            }

            return providers.Distinct().ToList();
        }

        /// <summary>
        /// Gets all available providers (legacy method for backward compatibility)
        /// </summary>
        public List<EnumValue<ProviderType>> GetAvailableProviders()
        {
            return GetAvailableProviders(ProviderCategory.All);
        }

        /// <summary>
        /// Registers a storage provider
        /// </summary>
        public void RegisterStorageProvider(ProviderType providerType, IOASISStorageProvider provider)
        {
            StorageProviders[providerType] = provider;
        }

        /// <summary>
        /// Registers a network provider
        /// </summary>
        public void RegisterNetworkProvider(ProviderType providerType, IOASISNetworkProvider provider)
        {
            NetworkProviders[providerType] = provider;
        }

        /// <summary>
        /// Registers a key manager provider
        /// </summary>
        public void RegisterKeyManagerProvider(ProviderType providerType, IOASISKeyManagerProvider provider)
        {
            KeyManagerProviders[providerType] = provider;
        }

        /// <summary>
        /// Registers a search provider
        /// </summary>
        public void RegisterSearchProvider(ProviderType providerType, IOASISSearchProvider provider)
        {
            SearchProviders[providerType] = provider;
        }

        /// <summary>
        /// Registers an NFT provider
        /// </summary>
        public void RegisterNFTProvider(ProviderType providerType, IOASISNFTProvider provider)
        {
            NFTProviders[providerType] = provider;
        }

        /// <summary>
        /// Registers a map provider
        /// </summary>
        public void RegisterMapProvider(ProviderType providerType, IOASISMapProvider provider)
        {
            MapProviders[providerType] = provider;
        }

        /// <summary>
        /// Gets a storage provider instance
        /// </summary>
        public IOASISStorageProvider GetStorageProvider(ProviderType providerType)
        {
            return StorageProviders.TryGetValue(providerType, out var provider) ? provider : null;
        }

        /// <summary>
        /// Gets a network provider instance
        /// </summary>
        public IOASISNetworkProvider GetNetworkProvider(ProviderType providerType)
        {
            return NetworkProviders.TryGetValue(providerType, out var provider) ? provider : null;
        }

        /// <summary>
        /// Gets a key manager provider instance
        /// </summary>
        public IOASISKeyManagerProvider GetKeyManagerProvider(ProviderType providerType)
        {
            return KeyManagerProviders.TryGetValue(providerType, out var provider) ? provider : null;
        }

        /// <summary>
        /// Gets a search provider instance
        /// </summary>
        public IOASISSearchProvider GetSearchProvider(ProviderType providerType)
        {
            return SearchProviders.TryGetValue(providerType, out var provider) ? provider : null;
        }

        /// <summary>
        /// Gets an NFT provider instance
        /// </summary>
        public IOASISNFTProvider GetNFTProvider(ProviderType providerType)
        {
            return NFTProviders.TryGetValue(providerType, out var provider) ? provider : null;
        }

        /// <summary>
        /// Gets a map provider instance
        /// </summary>
        public IOASISMapProvider GetMapProvider(ProviderType providerType)
        {
            return MapProviders.TryGetValue(providerType, out var provider) ? provider : null;
        }

        /// <summary>
        /// Checks if a provider is registered
        /// </summary>
        public bool IsProviderRegistered(ProviderType providerType, ProviderCategory category)
        {
            return category switch
            {
                ProviderCategory.Storage => StorageProviders.ContainsKey(providerType),
                ProviderCategory.Network => NetworkProviders.ContainsKey(providerType),
                ProviderCategory.KeyManager => KeyManagerProviders.ContainsKey(providerType),
                ProviderCategory.Search => SearchProviders.ContainsKey(providerType),
                ProviderCategory.NFT => NFTProviders.ContainsKey(providerType),
                ProviderCategory.Map => MapProviders.ContainsKey(providerType),
                ProviderCategory.All => StorageProviders.ContainsKey(providerType) || 
                                       NetworkProviders.ContainsKey(providerType) ||
                                       KeyManagerProviders.ContainsKey(providerType) ||
                                       SearchProviders.ContainsKey(providerType) ||
                                       NFTProviders.ContainsKey(providerType) ||
                                       MapProviders.ContainsKey(providerType),
                _ => false
            };
        }
    }
}
