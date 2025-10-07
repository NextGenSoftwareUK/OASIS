using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// NEW ProviderManager - Refactored for better separation of concerns
    /// This class acts as a facade/orchestrator for the new provider management system
    /// </summary>
    public class ProviderManagerNew : OASISManager
    {
        private static ProviderManagerNew _instance;
        private static readonly object _lock = new object();

        public static ProviderManagerNew Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ProviderManagerNew();
                    }
                }
                return _instance;
            }
        }

        // Ensure base constructor is satisfied; pass nulls to keep it inactive by default
        private ProviderManagerNew() : base(null, null) { }

        // New specialized managers
        private readonly ProviderRegistry _registry = ProviderRegistry.Instance;
        private readonly ProviderSelector _selector = ProviderSelector.Instance;
        private readonly ProviderSwitcher _switcher = ProviderSwitcher.Instance;
        private readonly ProviderConfigurator _configurator = ProviderConfigurator.Instance;

        #region Provider Registry Facade Methods

        /// <summary>
        /// Gets all available providers
        /// </summary>
        public List<EnumValue<ProviderType>> GetAvailableProviders()
        {
            return _registry.GetAvailableProviders();
        }

        /// <summary>
        /// Gets available providers for a specific category
        /// </summary>
        public List<EnumValue<ProviderType>> GetAvailableProviders(ProviderCategory category)
        {
            return _registry.GetAvailableProviders(category);
        }

        /// <summary>
        /// Gets current storage provider
        /// </summary>
        public EnumValue<ProviderType> CurrentStorageProviderType => _registry.CurrentStorageProviderType;

        /// <summary>
        /// Gets current network provider
        /// </summary>
        public EnumValue<ProviderType> CurrentNetworkProviderType => _registry.CurrentNetworkProviderType;

        /// <summary>
        /// Gets current key manager provider
        /// </summary>
        public EnumValue<ProviderType> CurrentKeyManagerProviderType => _registry.CurrentKeyManagerProviderType;

        /// <summary>
        /// Gets current search provider
        /// </summary>
        public EnumValue<ProviderType> CurrentSearchProviderType => _registry.CurrentSearchProviderType;

        /// <summary>
        /// Gets current NFT provider
        /// </summary>
        public EnumValue<ProviderType> CurrentNFTProviderType => _registry.CurrentNFTProviderType;

        /// <summary>
        /// Gets current map provider
        /// </summary>
        public EnumValue<ProviderType> CurrentMapProviderType => _registry.CurrentMapProviderType;

        /// <summary>
        /// Gets provider lists
        /// </summary>
        public List<EnumValue<ProviderType>> ProviderAutoFailOverList => _registry.ProviderAutoFailOverList;
        public List<EnumValue<ProviderType>> ProviderAutoReplicationList => _registry.ProviderAutoReplicationList;
        public List<EnumValue<ProviderType>> ProviderAutoLoadBalanceList => _registry.ProviderAutoLoadBalanceList;

        #endregion

        #region Provider Selection Facade Methods

        /// <summary>
        /// Selects optimal provider for load balancing
        /// </summary>
        public EnumValue<ProviderType> SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
        {
            return _selector.SelectOptimalProviderForLoadBalancing(strategy);
        }

        /// <summary>
        /// Selects failover provider
        /// </summary>
        public EnumValue<ProviderType> SelectFailoverProvider(EnumValue<ProviderType> currentProvider)
        {
            return _selector.SelectFailoverProvider(currentProvider);
        }

        /// <summary>
        /// Selects replication provider
        /// </summary>
        public EnumValue<ProviderType> SelectReplicationProvider(EnumValue<ProviderType> currentProvider)
        {
            return _selector.SelectReplicationProvider(currentProvider);
        }

        #endregion

        #region Provider Switching Facade Methods

        /// <summary>
        /// Switches storage provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchStorageProviderAsync(ProviderType newProviderType)
        {
            return await _switcher.SwitchStorageProviderAsync(newProviderType);
        }

        /// <summary>
        /// Switches network provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchNetworkProviderAsync(ProviderType newProviderType)
        {
            return await _switcher.SwitchNetworkProviderAsync(newProviderType);
        }

        /// <summary>
        /// Switches key manager provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchKeyManagerProviderAsync(ProviderType newProviderType)
        {
            return await _switcher.SwitchKeyManagerProviderAsync(newProviderType);
        }

        /// <summary>
        /// Switches search provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchSearchProviderAsync(ProviderType newProviderType)
        {
            return await _switcher.SwitchSearchProviderAsync(newProviderType);
        }

        /// <summary>
        /// Switches NFT provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchNFTProviderAsync(ProviderType newProviderType)
        {
            return await _switcher.SwitchNFTProviderAsync(newProviderType);
        }

        /// <summary>
        /// Switches map provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchMapProviderAsync(ProviderType newProviderType)
        {
            return await _switcher.SwitchMapProviderAsync(newProviderType);
        }

        /// <summary>
        /// Auto-switches provider based on performance
        /// </summary>
        public async Task<OASISResult<bool>> AutoSwitchProviderAsync(ProviderCategory category, string reason = "Performance issue")
        {
            return await _switcher.AutoSwitchProviderAsync(category, reason);
        }

        /// <summary>
        /// Gets switching status
        /// </summary>
        public OASISResult<ProviderSwitchStatus> GetSwitchStatus()
        {
            return _switcher.GetSwitchStatus();
        }

        #endregion

        #region Provider Configuration Facade Methods

        /// <summary>
        /// Adds provider to auto-failover list
        /// </summary>
        public OASISResult<bool> AddToAutoFailOverList(ProviderType providerType)
        {
            return _configurator.AddToAutoFailOverList(providerType);
        }

        /// <summary>
        /// Removes provider from auto-failover list
        /// </summary>
        public OASISResult<bool> RemoveFromAutoFailOverList(ProviderType providerType)
        {
            return _configurator.RemoveFromAutoFailOverList(providerType);
        }

        /// <summary>
        /// Adds provider to auto-replication list
        /// </summary>
        public OASISResult<bool> AddToAutoReplicationList(ProviderType providerType)
        {
            return _configurator.AddToAutoReplicationList(providerType);
        }

        /// <summary>
        /// Removes provider from auto-replication list
        /// </summary>
        public OASISResult<bool> RemoveFromAutoReplicationList(ProviderType providerType)
        {
            return _configurator.RemoveFromAutoReplicationList(providerType);
        }

        /// <summary>
        /// Adds provider to auto-load-balance list
        /// </summary>
        public OASISResult<bool> AddToAutoLoadBalanceList(ProviderType providerType)
        {
            return _configurator.AddToAutoLoadBalanceList(providerType);
        }

        /// <summary>
        /// Removes provider from auto-load-balance list
        /// </summary>
        public OASISResult<bool> RemoveFromAutoLoadBalanceList(ProviderType providerType)
        {
            return _configurator.RemoveFromAutoLoadBalanceList(providerType);
        }

        /// <summary>
        /// Gets provider lists
        /// </summary>
        public OASISResult<ProviderLists> GetProviderLists()
        {
            return _configurator.GetProviderLists();
        }

        /// <summary>
        /// Sets provider lists
        /// </summary>
        public OASISResult<bool> SetProviderLists(ProviderLists providerLists)
        {
            return _configurator.SetProviderLists(providerLists);
        }

        /// <summary>
        /// Gets complete provider configuration
        /// </summary>
        public OASISResult<ProviderConfiguration> GetProviderConfiguration()
        {
            return _configurator.GetProviderConfiguration();
        }

        #endregion

        #region Configuration Properties

        /// <summary>
        /// Auto-failover enabled flag
        /// </summary>
        public bool IsAutoFailOverEnabled
        {
            get => _configurator.IsAutoFailOverEnabled;
            set => _configurator.SetAutoFailOverEnabled(value);
        }

        /// <summary>
        /// Auto-replication enabled flag
        /// </summary>
        public bool IsAutoReplicationEnabled
        {
            get => _configurator.IsAutoReplicationEnabled;
            set => _configurator.SetAutoReplicationEnabled(value);
        }

        /// <summary>
        /// Auto-load-balancing enabled flag
        /// </summary>
        public bool IsAutoLoadBalanceEnabled
        {
            get => _configurator.IsAutoLoadBalanceEnabled;
            set => _configurator.SetAutoLoadBalanceEnabled(value);
        }

        #endregion

        #region Provider Registration Methods

        /// <summary>
        /// Registers a storage provider
        /// </summary>
        public void RegisterStorageProvider(ProviderType providerType, IOASISStorageProvider provider)
        {
            _registry.RegisterStorageProvider(providerType, provider);
        }

        /// <summary>
        /// Registers a network provider
        /// </summary>
        public void RegisterNetworkProvider(ProviderType providerType, IOASISNETProvider provider)
        {
            _registry.RegisterNetworkProvider(providerType, provider);
        }

        /// <summary>
        /// Registers a key manager provider
        /// </summary>
        public void RegisterKeyManagerProvider(ProviderType providerType, IOASISKeyManagerProvider provider)
        {
            _registry.RegisterKeyManagerProvider(providerType, provider);
        }

        /// <summary>
        /// Registers a search provider
        /// </summary>
        public void RegisterSearchProvider(ProviderType providerType, IOASISSearchProvider provider)
        {
            _registry.RegisterSearchProvider(providerType, provider);
        }

        /// <summary>
        /// Registers an NFT provider
        /// </summary>
        public void RegisterNFTProvider(ProviderType providerType, IOASISNFTProvider provider)
        {
            _registry.RegisterNFTProvider(providerType, provider);
        }

        /// <summary>
        /// Registers a map provider
        /// </summary>
        public void RegisterMapProvider(ProviderType providerType, IOASISMapProvider provider)
        {
            _registry.RegisterMapProvider(providerType, provider);
        }

        #endregion

        #region Provider Instance Access

        /// <summary>
        /// Gets storage provider instance
        /// </summary>
        public IOASISStorageProvider GetStorageProvider(ProviderType providerType)
        {
            return _registry.GetStorageProvider(providerType);
        }

        /// <summary>
        /// Gets network provider instance
        /// </summary>
        public IOASISNETProvider GetNetworkProvider(ProviderType providerType)
        {
            return _registry.GetNetworkProvider(providerType);
        }

        /// <summary>
        /// Gets key manager provider instance
        /// </summary>
        public IOASISKeyManagerProvider GetKeyManagerProvider(ProviderType providerType)
        {
            return _registry.GetKeyManagerProvider(providerType);
        }

        /// <summary>
        /// Gets search provider instance
        /// </summary>
        public IOASISSearchProvider GetSearchProvider(ProviderType providerType)
        {
            return _registry.GetSearchProvider(providerType);
        }

        /// <summary>
        /// Gets NFT provider instance
        /// </summary>
        public IOASISNFTProvider GetNFTProvider(ProviderType providerType)
        {
            return _registry.GetNFTProvider(providerType);
        }

        /// <summary>
        /// Gets map provider instance
        /// </summary>
        public IOASISMapProvider GetMapProvider(ProviderType providerType)
        {
            return _registry.GetMapProvider(providerType);
        }

        #endregion
    }
}
