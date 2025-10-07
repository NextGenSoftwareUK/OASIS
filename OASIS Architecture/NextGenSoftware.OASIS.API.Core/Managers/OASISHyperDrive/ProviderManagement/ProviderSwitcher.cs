using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// Handles provider switching logic and state management
    /// </summary>
    public class ProviderSwitcher
    {
        private static ProviderSwitcher _instance;
        private static readonly object _lock = new object();

        public static ProviderSwitcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ProviderSwitcher();
                    }
                }
                return _instance;
            }
        }

        private ProviderSwitcher() { }

        private readonly ProviderRegistry _registry = ProviderRegistry.Instance;
        private readonly ProviderSelector _selector = ProviderSelector.Instance;
        private readonly PerformanceMonitor _performanceMonitor = PerformanceMonitor.Instance;

        // Switching state
        private bool _isSwitching = false;
        private readonly object _switchingLock = new object();

        /// <summary>
        /// Switches to a new storage provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchStorageProviderAsync(ProviderType newProviderType)
        {
            return await SwitchProviderAsync(newProviderType, ProviderCategory.Storage, 
                () => _registry.CurrentStorageProviderType = new EnumValue<ProviderType>(newProviderType));
        }

        /// <summary>
        /// Switches to a new network provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchNetworkProviderAsync(ProviderType newProviderType)
        {
            return await SwitchProviderAsync(newProviderType, ProviderCategory.Network,
                () => _registry.CurrentNetworkProviderType = new EnumValue<ProviderType>(newProviderType));
        }

        /// <summary>
        /// Switches to a new key manager provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchKeyManagerProviderAsync(ProviderType newProviderType)
        {
            return await SwitchProviderAsync(newProviderType, ProviderCategory.KeyManager,
                () => _registry.CurrentKeyManagerProviderType = new EnumValue<ProviderType>(newProviderType));
        }

        /// <summary>
        /// Switches to a new search provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchSearchProviderAsync(ProviderType newProviderType)
        {
            return await SwitchProviderAsync(newProviderType, ProviderCategory.Search,
                () => _registry.CurrentSearchProviderType = new EnumValue<ProviderType>(newProviderType));
        }

        /// <summary>
        /// Switches to a new NFT provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchNFTProviderAsync(ProviderType newProviderType)
        {
            return await SwitchProviderAsync(newProviderType, ProviderCategory.NFT,
                () => _registry.CurrentNFTProviderType = new EnumValue<ProviderType>(newProviderType));
        }

        /// <summary>
        /// Switches to a new map provider
        /// </summary>
        public async Task<OASISResult<bool>> SwitchMapProviderAsync(ProviderType newProviderType)
        {
            return await SwitchProviderAsync(newProviderType, ProviderCategory.Map,
                () => _registry.CurrentMapProviderType = new EnumValue<ProviderType>(newProviderType));
        }

        /// <summary>
        /// Generic provider switching logic
        /// </summary>
        private async Task<OASISResult<bool>> SwitchProviderAsync(ProviderType newProviderType, ProviderCategory category, Action updateCurrentProvider)
        {
            lock (_switchingLock)
            {
                if (_isSwitching)
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = "Provider switch already in progress"
                    };
                }
                _isSwitching = true;
            }

            try
            {
                // Validate provider is registered
                if (!_registry.IsProviderRegistered(newProviderType, category))
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"Provider {newProviderType} is not registered for category {category}"
                    };
                }

                // Get current provider for logging
                var currentProvider = GetCurrentProvider(category);

                // Perform the switch
                updateCurrentProvider();

                // Log the switch
                await LogProviderSwitchAsync(currentProvider, newProviderType, category);

                // Update performance metrics
                await _performanceMonitor.RecordProviderSwitchAsync(currentProvider, newProviderType);

                return new OASISResult<bool>
                {
                    Result = true,
                    Message = $"Successfully switched {category} provider from {currentProvider} to {newProviderType}"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to switch provider: {ex.Message}",
                    Exception = ex
                };
            }
            finally
            {
                lock (_switchingLock)
                {
                    _isSwitching = false;
                }
            }
        }

        /// <summary>
        /// Gets current provider for a category
        /// </summary>
        private ProviderType GetCurrentProvider(ProviderCategory category)
        {
            return category switch
            {
                ProviderCategory.Storage => _registry.CurrentStorageProviderType.Value,
                ProviderCategory.Network => _registry.CurrentNetworkProviderType.Value,
                ProviderCategory.KeyManager => _registry.CurrentKeyManagerProviderType.Value,
                ProviderCategory.Search => _registry.CurrentSearchProviderType.Value,
                ProviderCategory.NFT => _registry.CurrentNFTProviderType.Value,
                ProviderCategory.Map => _registry.CurrentMapProviderType.Value,
                _ => ProviderType.Default
            };
        }

        /// <summary>
        /// Logs provider switch for audit purposes
        /// </summary>
        private async Task LogProviderSwitchAsync(ProviderType fromProvider, ProviderType toProvider, ProviderCategory category)
        {
            try
            {
                // This would integrate with logging system
                var logMessage = $"Provider switch: {category} from {fromProvider} to {toProvider} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
                
                // For now, just use console logging
                Console.WriteLine($"[ProviderSwitcher] {logMessage}");
                
                // TODO: Integrate with proper logging system
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProviderSwitcher] Failed to log provider switch: {ex.Message}");
            }
        }

        /// <summary>
        /// Auto-switches provider based on performance issues
        /// </summary>
        public async Task<OASISResult<bool>> AutoSwitchProviderAsync(ProviderCategory category, string reason = "Performance issue")
        {
            try
            {
                var currentProvider = GetCurrentProvider(category);
                var availableProviders = _registry.GetAvailableProviders(category)
                    .Where(p => p.Value != currentProvider)
                    .ToList();

                if (!availableProviders.Any())
                {
                    return new OASISResult<bool>
                    {
                        IsError = true,
                        Message = $"No alternative providers available for {category}"
                    };
                }

                // Select best alternative provider
                var newProvider = _selector.SelectPerformanceBasedProvider(availableProviders);

                // Perform the switch
                var switchResult = category switch
                {
                    ProviderCategory.Storage => await SwitchStorageProviderAsync(newProvider.Value),
                    ProviderCategory.Network => await SwitchNetworkProviderAsync(newProvider.Value),
                    ProviderCategory.KeyManager => await SwitchKeyManagerProviderAsync(newProvider.Value),
                    ProviderCategory.Search => await SwitchSearchProviderAsync(newProvider.Value),
                    ProviderCategory.NFT => await SwitchNFTProviderAsync(newProvider.Value),
                    ProviderCategory.Map => await SwitchMapProviderAsync(newProvider.Value),
                    _ => new OASISResult<bool> { IsError = true, Message = "Invalid provider category" }
                };

                if (!switchResult.IsError)
                {
                    switchResult.Message = $"Auto-switched {category} provider due to: {reason}. {switchResult.Message}";
                }

                return switchResult;
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Auto-switch failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Checks if a provider switch is currently in progress
        /// </summary>
        public bool IsSwitching => _isSwitching;

        /// <summary>
        /// Gets switching status for monitoring
        /// </summary>
        public OASISResult<ProviderSwitchStatus> GetSwitchStatus()
        {
            return new OASISResult<ProviderSwitchStatus>
            {
                Result = new ProviderSwitchStatus
                {
                    IsSwitching = _isSwitching,
                    CurrentStorageProvider = _registry.CurrentStorageProviderType.Value,
                    CurrentNetworkProvider = _registry.CurrentNetworkProviderType.Value,
                    CurrentKeyManagerProvider = _registry.CurrentKeyManagerProviderType.Value,
                    CurrentSearchProvider = _registry.CurrentSearchProviderType.Value,
                    CurrentNFTProvider = _registry.CurrentNFTProviderType.Value,
                    CurrentMapProvider = _registry.CurrentMapProviderType.Value,
                    LastSwitchTime = DateTime.UtcNow // TODO: Track actual last switch time
                }
            };
        }
    }

    /// <summary>
    /// Provider switch status information
    /// </summary>
    public class ProviderSwitchStatus
    {
        public bool IsSwitching { get; set; }
        public ProviderType CurrentStorageProvider { get; set; }
        public ProviderType CurrentNetworkProvider { get; set; }
        public ProviderType CurrentKeyManagerProvider { get; set; }
        public ProviderType CurrentSearchProvider { get; set; }
        public ProviderType CurrentNFTProvider { get; set; }
        public ProviderType CurrentMapProvider { get; set; }
        public DateTime LastSwitchTime { get; set; }
    }
}
