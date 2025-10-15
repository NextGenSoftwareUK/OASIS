using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
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
    /// Handles provider configuration, settings, and provider list management
    /// </summary>
    public class ProviderConfigurator
    {
        private static ProviderConfigurator _instance;
        private static readonly object _lock = new object();

        public static ProviderConfigurator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ProviderConfigurator();
                    }
                }
                return _instance;
            }
        }

        private ProviderConfigurator() { }

        private readonly ProviderRegistry _registry = ProviderRegistry.Instance;
        private readonly OASISHyperDriveConfigManager _configManager = OASISHyperDriveConfigManager.Instance;

        // Configuration flags
        public bool IsAutoFailOverEnabled { get; set; } = true;
        public bool IsAutoReplicationEnabled { get; set; } = true;
        public bool IsAutoLoadBalanceEnabled { get; set; } = true;

        /// <summary>
        /// Adds a provider to the auto-failover list
        /// </summary>
        public OASISResult<bool> AddToAutoFailOverList(ProviderType providerType)
        {
            try
            {
                if (!_registry.ProviderAutoFailOverList.Any(p => p.Value == providerType))
                {
                    _registry.ProviderAutoFailOverList.Add(new EnumValue<ProviderType>(providerType));
                    
                    return new OASISResult<bool>
                    {
                        Result = true,
                        Message = $"Provider {providerType} added to auto-failover list"
                    };
                }

                return new OASISResult<bool>
                {
                    Result = false,
                    Message = $"Provider {providerType} is already in auto-failover list"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to add provider to auto-failover list: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Removes a provider from the auto-failover list
        /// </summary>
        public OASISResult<bool> RemoveFromAutoFailOverList(ProviderType providerType)
        {
            try
            {
                var provider = _registry.ProviderAutoFailOverList.FirstOrDefault(p => p.Value == providerType);
                if (provider != null)
                {
                    _registry.ProviderAutoFailOverList.Remove(provider);
                    
                    return new OASISResult<bool>
                    {
                        Result = true,
                        Message = $"Provider {providerType} removed from auto-failover list"
                    };
                }

                return new OASISResult<bool>
                {
                    Result = false,
                    Message = $"Provider {providerType} was not in auto-failover list"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to remove provider from auto-failover list: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Adds a provider to the auto-replication list
        /// </summary>
        public OASISResult<bool> AddToAutoReplicationList(ProviderType providerType)
        {
            try
            {
                if (!_registry.ProviderAutoReplicationList.Any(p => p.Value == providerType))
                {
                    _registry.ProviderAutoReplicationList.Add(new EnumValue<ProviderType>(providerType));
                    
                    return new OASISResult<bool>
                    {
                        Result = true,
                        Message = $"Provider {providerType} added to auto-replication list"
                    };
                }

                return new OASISResult<bool>
                {
                    Result = false,
                    Message = $"Provider {providerType} is already in auto-replication list"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to add provider to auto-replication list: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Removes a provider from the auto-replication list
        /// </summary>
        public OASISResult<bool> RemoveFromAutoReplicationList(ProviderType providerType)
        {
            try
            {
                var provider = _registry.ProviderAutoReplicationList.FirstOrDefault(p => p.Value == providerType);
                if (provider != null)
                {
                    _registry.ProviderAutoReplicationList.Remove(provider);
                    
                    return new OASISResult<bool>
                    {
                        Result = true,
                        Message = $"Provider {providerType} removed from auto-replication list"
                    };
                }

                return new OASISResult<bool>
                {
                    Result = false,
                    Message = $"Provider {providerType} was not in auto-replication list"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to remove provider from auto-replication list: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Adds a provider to the auto-load-balance list
        /// </summary>
        public OASISResult<bool> AddToAutoLoadBalanceList(ProviderType providerType)
        {
            try
            {
                if (!_registry.ProviderAutoLoadBalanceList.Any(p => p.Value == providerType))
                {
                    _registry.ProviderAutoLoadBalanceList.Add(new EnumValue<ProviderType>(providerType));
                    
                    return new OASISResult<bool>
                    {
                        Result = true,
                        Message = $"Provider {providerType} added to auto-load-balance list"
                    };
                }

                return new OASISResult<bool>
                {
                    Result = false,
                    Message = $"Provider {providerType} is already in auto-load-balance list"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to add provider to auto-load-balance list: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Removes a provider from the auto-load-balance list
        /// </summary>
        public OASISResult<bool> RemoveFromAutoLoadBalanceList(ProviderType providerType)
        {
            try
            {
                var provider = _registry.ProviderAutoLoadBalanceList.FirstOrDefault(p => p.Value == providerType);
                if (provider != null)
                {
                    _registry.ProviderAutoLoadBalanceList.Remove(provider);
                    
                    return new OASISResult<bool>
                    {
                        Result = true,
                        Message = $"Provider {providerType} removed from auto-load-balance list"
                    };
                }

                return new OASISResult<bool>
                {
                    Result = false,
                    Message = $"Provider {providerType} was not in auto-load-balance list"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to remove provider from auto-load-balance list: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Gets all provider lists
        /// </summary>
        public OASISResult<ProviderLists> GetProviderLists()
        {
            try
            {
                return new OASISResult<ProviderLists>
                {
                    Result = new ProviderLists
                    {
                        AutoFailOverList = _registry.ProviderAutoFailOverList.ToList(),
                        AutoReplicationList = _registry.ProviderAutoReplicationList.ToList(),
                        AutoLoadBalanceList = _registry.ProviderAutoLoadBalanceList.ToList(),
                        IsAutoFailOverEnabled = IsAutoFailOverEnabled,
                        IsAutoReplicationEnabled = IsAutoReplicationEnabled,
                        IsAutoLoadBalanceEnabled = IsAutoLoadBalanceEnabled
                    },
                    
                    Message = "Provider lists retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<ProviderLists>
                {
                    IsError = true,
                    Message = $"Failed to get provider lists: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Sets provider lists
        /// </summary>
        public OASISResult<bool> SetProviderLists(ProviderLists providerLists)
        {
            try
            {
                _registry.ProviderAutoFailOverList = providerLists.AutoFailOverList ?? new List<EnumValue<ProviderType>>();
                _registry.ProviderAutoReplicationList = providerLists.AutoReplicationList ?? new List<EnumValue<ProviderType>>();
                _registry.ProviderAutoLoadBalanceList = providerLists.AutoLoadBalanceList ?? new List<EnumValue<ProviderType>>();
                
                IsAutoFailOverEnabled = providerLists.IsAutoFailOverEnabled;
                IsAutoReplicationEnabled = providerLists.IsAutoReplicationEnabled;
                IsAutoLoadBalanceEnabled = providerLists.IsAutoLoadBalanceEnabled;

                return new OASISResult<bool>
                {
                    Result = true,
                    Message = "Provider lists updated successfully"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to set provider lists: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Enables or disables auto-failover
        /// </summary>
        public OASISResult<bool> SetAutoFailOverEnabled(bool enabled)
        {
            try
            {
                IsAutoFailOverEnabled = enabled;
                
                return new OASISResult<bool>
                {
                    Result = true,
                    Message = $"Auto-failover {(enabled ? "enabled" : "disabled")}"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to set auto-failover: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Enables or disables auto-replication
        /// </summary>
        public OASISResult<bool> SetAutoReplicationEnabled(bool enabled)
        {
            try
            {
                IsAutoReplicationEnabled = enabled;
                
                return new OASISResult<bool>
                {
                    Result = true,
                    Message = $"Auto-replication {(enabled ? "enabled" : "disabled")}"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to set auto-replication: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Enables or disables auto-load-balancing
        /// </summary>
        public OASISResult<bool> SetAutoLoadBalanceEnabled(bool enabled)
        {
            try
            {
                IsAutoLoadBalanceEnabled = enabled;
                
                return new OASISResult<bool>
                {
                    Result = true,
                    Message = $"Auto-load-balancing {(enabled ? "enabled" : "disabled")}"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<bool>
                {
                    IsError = true,
                    Message = $"Failed to set auto-load-balancing: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Gets provider configuration summary
        /// </summary>
        public OASISResult<ProviderConfiguration> GetProviderConfiguration()
        {
            try
            {
                return new OASISResult<ProviderConfiguration>
                {
                    Result = new ProviderConfiguration
                    {
                        CurrentStorageProvider = _registry.CurrentStorageProviderType.Value,
                        CurrentNetworkProvider = _registry.CurrentNetworkProviderType.Value,
                        CurrentKeyManagerProvider = _registry.CurrentKeyManagerProviderType.Value,
                        CurrentSearchProvider = _registry.CurrentSearchProviderType.Value,
                        CurrentNFTProvider = _registry.CurrentNFTProviderType.Value,
                        CurrentMapProvider = _registry.CurrentMapProviderType.Value,
                        AutoFailOverList = _registry.ProviderAutoFailOverList.ToList(),
                        AutoReplicationList = _registry.ProviderAutoReplicationList.ToList(),
                        AutoLoadBalanceList = _registry.ProviderAutoLoadBalanceList.ToList(),
                        IsAutoFailOverEnabled = IsAutoFailOverEnabled,
                        IsAutoReplicationEnabled = IsAutoReplicationEnabled,
                        IsAutoLoadBalanceEnabled = IsAutoLoadBalanceEnabled
                    },
                    
                    Message = "Provider configuration retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<ProviderConfiguration>
                {
                    IsError = true,
                    Message = $"Failed to get provider configuration: {ex.Message}",
                    Exception = ex
                };
            }
        }
    }

    /// <summary>
    /// Provider lists configuration
    /// </summary>
    public class ProviderLists
    {
        public List<EnumValue<ProviderType>> AutoFailOverList { get; set; } = new List<EnumValue<ProviderType>>();
        public List<EnumValue<ProviderType>> AutoReplicationList { get; set; } = new List<EnumValue<ProviderType>>();
        public List<EnumValue<ProviderType>> AutoLoadBalanceList { get; set; } = new List<EnumValue<ProviderType>>();
        public bool IsAutoFailOverEnabled { get; set; } = true;
        public bool IsAutoReplicationEnabled { get; set; } = true;
        public bool IsAutoLoadBalanceEnabled { get; set; } = true;
    }

    /// <summary>
    /// Complete provider configuration
    /// </summary>
    public class ProviderConfiguration
    {
        public ProviderType CurrentStorageProvider { get; set; }
        public ProviderType CurrentNetworkProvider { get; set; }
        public ProviderType CurrentKeyManagerProvider { get; set; }
        public ProviderType CurrentSearchProvider { get; set; }
        public ProviderType CurrentNFTProvider { get; set; }
        public ProviderType CurrentMapProvider { get; set; }
        public List<EnumValue<ProviderType>> AutoFailOverList { get; set; } = new List<EnumValue<ProviderType>>();
        public List<EnumValue<ProviderType>> AutoReplicationList { get; set; } = new List<EnumValue<ProviderType>>();
        public List<EnumValue<ProviderType>> AutoLoadBalanceList { get; set; } = new List<EnumValue<ProviderType>>();
        public bool IsAutoFailOverEnabled { get; set; } = true;
        public bool IsAutoReplicationEnabled { get; set; } = true;
        public bool IsAutoLoadBalanceEnabled { get; set; } = true;
    }
}
