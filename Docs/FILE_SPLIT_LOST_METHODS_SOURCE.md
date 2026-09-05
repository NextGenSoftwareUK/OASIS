# Recovered source for the lost methods

Each block is the method exactly as it existed immediately before the split commit that deleted it. Restore by pasting into the relevant partial class.

---

## `ActivateRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> ActivateRuntimeTemplate(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Activate((Runtime)Runtime, avatarId, providerType));
        }
```

## `ActivateRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> ActivateRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.ActivateAsync((Runtime)Runtime, avatarId, providerType));
        }
```

## `AddToAutoFailOverListNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public OASISResult<bool> AddToAutoFailOverListNew(ProviderType providerType)
    {
        return NewProviderManager.AddToAutoFailOverList(providerType);
    }
```

## `AddToAutoLoadBalanceListNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public OASISResult<bool> AddToAutoLoadBalanceListNew(ProviderType providerType)
    {
        return NewProviderManager.AddToAutoLoadBalanceList(providerType);
    }
```

## `AddToAutoReplicationListNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public OASISResult<bool> AddToAutoReplicationListNew(ProviderType providerType)
    {
        return NewProviderManager.AddToAutoReplicationList(providerType);
    }
```

## `CalculateHealthFromMetrics`

From `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/NetworkMetricsService.cs` at `88817e70b^`

```csharp
        public double CalculateHealthFromMetrics(object networkMetrics)
        {
            try
            {
                if (networkMetrics == null)
                    return 0.0;

                // Parse real network metrics from Holochain conductor
                // This would parse the actual network metrics JSON/object
                // For now, we'll extract key metrics that indicate health
                
                // Extract real metrics from Holochain conductor response
                var metrics = ParseNetworkMetrics(networkMetrics);
                
                // Calculate health based on real metrics
                var connectionHealth = CalculateConnectionHealth(metrics);
                var latencyHealth = CalculateLatencyHealth(metrics);
                var throughputHealth = CalculateThroughputHealth(metrics);
                
                // Weighted average of different health factors
                var overallHealth = (connectionHealth * 0.4) + (latencyHealth * 0.3) + (throughputHealth * 0.3);
                
                return Math.Max(0.0, Math.Min(1.0, overallHealth));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating health from metrics: {ex.Message}");
                // Calculate actual network health based on real metrics
                try
                {
                    var latency = await _holoNETClient.GetNetworkLatencyAsync();
                    var bandwidth = await _holoNETClient.GetNetworkBandwidthAsync();
                    var uptime = await _holoNETClient.GetNetworkUptimeAsync();
                    
                    // Calculate health score (0-1)
                    var latencyScore = Math.Max(0, 1.0 - (latency / 1000.0)); // Lower latency = higher score
                    var bandwidthScore = Math.Min(1.0, bandwidth / 1000.0); // Higher bandwidth = higher score
                    var uptimeScore = uptime / 100.0; // Uptime percentage
                    
                    var healthScore = (latencyScore * 0.3 + bandwidthScore * 0.3 + uptimeScore * 0.4);
                    return Math.Max(0.0, Math.Min(1.0, healthScore));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calculating network health: {ex.Message}");
                }
                
                return 0.5; // Default health on error
            }
        }
```

## `CreateRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> CreateRuntimeTemplate(string name, string description, RuntimeType runtimeType, Guid avatarId, string fullPathToRuntimeTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(name, description, runtimeType, avatarId, fullPathToRuntimeTemplate, providerType));
        }
```

## `CreateRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> CreateRuntimeTemplateAsync(string name, string description, RuntimeType runtimeType, Guid avatarId, string fullPathToRuntimeTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(name, description, runtimeType, avatarId, fullPathToRuntimeTemplate, providerType));
        }
```

## `DeactivateRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> DeactivateRuntimeTemplate(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Deactivate(avatarId, (Runtime)Runtime, providerType));
        }
```

## `DeactivateRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> DeactivateRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeactivateAsync(avatarId, (Runtime)Runtime, providerType));
        }
```

## `DeleteRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> DeleteRuntimeTemplate(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Delete(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }
```

## `DeleteRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> DeleteRuntimeTemplateAsync(Guid avatarId, Guid oappTemplateId, int version, bool softDelete = true, bool deleteDownload = true, bool deleteInstall = true, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DeleteAsync(avatarId, oappTemplateId, version, softDelete, deleteDownload, deleteInstall, providerType));
        }
```

## `DownloadAndInstallRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IInstalledRuntime> DownloadAndInstallRuntimeTemplate(Guid avatarId, IRuntime Runtime, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.DownloadAndInstall(avatarId, (Runtime)Runtime, fullInstallPath, fullDownloadPath, createRuntimeTemplateDirectory, reInstall, providerType));
        }
```

## `DownloadAndInstallRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IInstalledRuntime>> DownloadAndInstallRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, string fullInstallPath, string fullDownloadPath = "", bool createRuntimeTemplateDirectory = true, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAndInstallAsync(avatarId, (Runtime)Runtime, fullInstallPath, fullDownloadPath, createRuntimeTemplateDirectory, reInstall, providerType));
        }
```

## `DownloadRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IDownloadedRuntime> DownloadRuntimeTemplate(Guid avatarId, IRuntime Runtime, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Download(avatarId, (Runtime)Runtime, fullDownloadPath, reInstall, providerType));
        }
```

## `DownloadRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IDownloadedRuntime>> DownloadRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, string fullDownloadPath, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.DownloadAsync(avatarId, (Runtime)Runtime, fullDownloadPath, reInstall, providerType));
        }
```

## `EditRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> EditRuntimeTemplateAsync(Guid RuntimeTemplateId, IOAPPSystemHolonDNA newRuntimeTemplateDNA, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.EditAsync(RuntimeTemplateId, newRuntimeTemplateDNA, avatarId, providerType));
        }
```

## `GetAllAvatarDetail`

From `NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/AvatarRepository.cs` at `7b673485a^`

```csharp
        public IEnumerable<AvatarDetail> GetAllAvatarDetail()
        {
            return _dbContext.AvatarDetail.Find(_ => true).ToEnumerable();
        }
```

## `GetAllAvatarDetailAsync`

From `NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/AvatarRepository.cs` at `7b673485a^`

```csharp
        public async Task<IEnumerable<AvatarDetail>> GetAllAvatarDetailAsync()
        {
            var cursor = await _dbContext.AvatarDetail.FindAsync(_ => true);
            return cursor.ToEnumerable();
        }
```

## `GetAvailableProvidersNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public List<EnumValue<ProviderType>> GetAvailableProvidersNew()
    {
        return NewProviderManager.GetAvailableProviders();
    }
```

## `GetOASISGeoNFTCollectionAsync`

From `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/NFTManager.cs` at `88817e70b^`

```csharp
        public async Task<OASISResult<IOASISGeoNFTCollection>> GetOASISGeoNFTCollectionAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISGeoNFTCollection> result = new();
            string errorMessage = "Error occured in GetOASISGeoNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IHolon> holonRes = await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.GeoNFTCollection, 0, providerType: providerType);
                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    var h = holonRes.Result;
                    IOASISGeoNFTCollection coll = new OASISGeoNFTCollection()
                    {
                        Id = h.Id,
                        Title = h.MetaData.ContainsKey("OASISGEONFTCOLLECTION.Title") ? h.MetaData["OASISGEONFTCOLLECTION.Title"]?.ToString() : null,
                        Description = h.MetaData.ContainsKey("OASISGEONFTCOLLECTION.Description") ? h.MetaData["OASISGEONFTCOLLECTION.Description"]?.ToString() : null,
                        ImageUrl = h.MetaData.ContainsKey("OASISGEONFTCOLLECTION.ImageUrl") ? h.MetaData["OASISGEONFTCOLLECTION.ImageUrl"]?.ToString() : null,
                        ThumbnailUrl = h.MetaData.ContainsKey("OASISGEONFTCOLLECTION.ThumbnailUrl") ? h.MetaData["OASISGEONFTCOLLECTION.ThumbnailUrl"]?.ToString() : null,
                        MetaData = h.MetaData
                    };

                    if (h.MetaData.ContainsKey("OASISGEONFTCOLLECTION.OASISGeoNFTIds") && h.MetaData["OASISGEONFTCOLLECTION.OASISGeoNFTIds"] is IEnumerable<string> ids)
                        coll.OASISGeoNFTIds = ids.ToList();

                    result.Result = coll;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collection. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }
```

## `GetOASISNFTCollectionAsync`

From `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/NFTManager.cs` at `88817e70b^`

```csharp
        public async Task<OASISResult<IOASISNFTCollection>> GetOASISNFTCollectionAsync(Guid id, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOASISNFTCollection> result = new();
            string errorMessage = "Error occured in GetOASISNFTCollectionAsync in NFTManager. Reason:";

            try
            {
                OASISResult<IHolon> holonRes = await Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.NFTCollection, 0, providerType: providerType);
                if (holonRes != null && !holonRes.IsError && holonRes.Result != null)
                {
                    var h = holonRes.Result;
                    IOASISNFTCollection coll = new OASISNFTCollection()
                    {
                        Id = h.Id,
                        Title = h.MetaData.ContainsKey("OASISNFTCOLLECTION.Title") ? h.MetaData["OASISNFTCOLLECTION.Title"]?.ToString() : null,
                        Description = h.MetaData.ContainsKey("OASISNFTCOLLECTION.Description") ? h.MetaData["OASISNFTCOLLECTION.Description"]?.ToString() : null,
                        ImageUrl = h.MetaData.ContainsKey("OASISNFTCOLLECTION.ImageUrl") ? h.MetaData["OASISNFTCOLLECTION.ImageUrl"]?.ToString() : null,
                        ThumbnailUrl = h.MetaData.ContainsKey("OASISNFTCOLLECTION.ThumbnailUrl") ? h.MetaData["OASISNFTCOLLECTION.ThumbnailUrl"]?.ToString() : null,
                        MetaData = h.MetaData
                    };

                    if (h.MetaData.ContainsKey("OASISNFTCOLLECTION.OASISNFTIds") && h.MetaData["OASISNFTCOLLECTION.OASISNFTIds"] is IEnumerable<string> ids)
                        coll.OASISNFTIds = ids.ToList();

                    result.Result = coll;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading collection. Reason: {holonRes?.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured: {e.Message}", e);
            }

            return result;
        }
```

## `GetProviderConfigurationNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public OASISResult<ProviderConfiguration> GetProviderConfigurationNew()
    {
        return NewProviderManager.GetProviderConfiguration();
    }
```

## `GetSwitchStatusNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public OASISResult<ProviderSwitchStatus> GetSwitchStatusNew()
    {
        return NewProviderManager.GetSwitchStatus();
    }
```

## `InstallRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IInstalledRuntime> InstallRuntimeTemplate(Guid avatarId, string fullPathToPublishedRuntimeTemplateFile, string fullInstallPath, bool createRuntimeTemplateDirectory = true, IDownloadedRuntime downloadedRuntimeTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Install(avatarId, fullPathToPublishedRuntimeTemplateFile, fullInstallPath, createRuntimeTemplateDirectory, downloadedRuntimeTemplate, reInstall, providerType));
        }
```

## `InstallRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IInstalledRuntime>> InstallRuntimeTemplateAsync(Guid avatarId, string fullPathToPublishedRuntimeTemplateFile, string fullInstallPath, bool createRuntimeTemplateDirectory = true, IDownloadedRuntime downloadedRuntimeTemplate = null, bool reInstall = false, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.InstallAsync(avatarId, fullPathToPublishedRuntimeTemplateFile, fullInstallPath, createRuntimeTemplateDirectory, downloadedRuntimeTemplate, reInstall, providerType));
        }
```

## `IsRuntimeTemplateInstalled`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<bool> IsRuntimeTemplateInstalled(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return base.IsInstalled(avatarId, RuntimeTemplateId, versionSequence, providerType);
        }
```

## `IsRuntimeTemplateInstalledAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<bool>> IsRuntimeTemplateInstalledAsync(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return await base.IsInstalledAsync(avatarId, RuntimeTemplateId, versionSequence, providerType);
        }
```

## `ListDeactivatedRuntimeTemplates`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IEnumerable<IRuntime>> ListDeactivatedRuntimeTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListDeactivated(avatarId, providerType));
        }
```

## `ListDeactivatedRuntimeTemplatesAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IEnumerable<IRuntime>>> ListDeactivatedRuntimeTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListDeactivatedAsync(avatarId, providerType));
        }
```

## `ListInstalledRuntimeTemplates`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IEnumerable<IInstalledRuntime>> ListInstalledRuntimeTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListInstalled(avatarId, providerType));
        }
```

## `ListInstalledRuntimeTemplatesAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IEnumerable<IInstalledRuntime>>> ListInstalledRuntimeTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListInstalledAsync(avatarId, providerType));
        }
```

## `ListUnInstalledRuntimeTemplates`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IEnumerable<IInstalledRuntime>> ListUnInstalledRuntimeTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUninstalled(avatarId, providerType));
        }
```

## `ListUnInstalledRuntimeTemplatesAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IEnumerable<IInstalledRuntime>>> ListUnInstalledRuntimeTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUninstalledAsync(avatarId, providerType));
        }
```

## `ListUnpublishedRuntimeTemplates`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IEnumerable<IRuntime>> ListUnpublishedRuntimeTemplates(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.ListUnpublished(avatarId, providerType));
        }
```

## `ListUnpublishedRuntimeTemplatesAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IEnumerable<IRuntime>>> ListUnpublishedRuntimeTemplatesAsync(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.ListUnpublishedAsync(avatarId, providerType));
        }
```

## `LoadAllRuntimeTemplates`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IEnumerable<IRuntime>> LoadAllRuntimeTemplates(Guid avatarId, runtimeType RuntimeType = RuntimeType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAll(avatarId, RuntimeType, RuntimeType == RuntimeType.All, showAllVersions, version, providerType));
        }
```

## `LoadAllRuntimeTemplatesAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadAllRuntimeTemplatesAsync(Guid avatarId, RuntimeType runtimeType = RuntimeType.All, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllAsync(avatarId, runtimeType, RuntimeType == RuntimeType.All, showAllVersions, version, providerType));
        }
```

## `LoadAllRuntimeTemplatesForAvatar`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IEnumerable<IRuntime>> LoadAllRuntimeTemplatesForAvatar(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadAllForAvatar(avatarId, showAllVersions, version, providerType));
        }
```

## `LoadAllRuntimeTemplatesForAvatarAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadAllRuntimeTemplatesForAvatarAsync(Guid avatarId, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadAllForAvatarAsync(avatarId, showAllVersions, version, providerType));
        }
```

## `LoadInstalledRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IInstalledRuntime> LoadInstalledRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadInstalled(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }
```

## `LoadInstalledRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IInstalledRuntime>> LoadInstalledRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, int versionSequence = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadInstalledAsync(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }
```

## `LoadRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> LoadRuntimeTemplate(Guid avatarId, Guid RuntimeTemplateId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Load(avatarId, RuntimeTemplateId, version, providerType));
        }
```

## `LoadRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> LoadRuntimeTemplateAsync(Guid avatarId, Guid RuntimeTemplateId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadAsync(avatarId, RuntimeTemplateId, version, providerType));
        }
```

## `LoadRuntimeTemplateVersion`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> LoadRuntimeTemplateVersion(Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.LoadVersion(RuntimeTemplateId, version, providerType));
        }
```

## `LoadRuntimeTemplateVersionAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> LoadRuntimeTemplateVersionAsync(Guid RuntimeTemplateId, string version, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.LoadVersionAsync(RuntimeTemplateId, version, providerType));
        }
```

## `LoadRuntimeTemplateVersions`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IEnumerable<IRuntime>> LoadRuntimeTemplateVersions(Guid RuntimeTemplateId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.LoadVersions(RuntimeTemplateId, providerType));
        }
```

## `LoadRuntimeTemplateVersionsAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IEnumerable<IRuntime>>> LoadRuntimeTemplateVersionsAsync(Guid RuntimeTemplateId, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.LoadVersionsAsync(RuntimeTemplateId, providerType));
        }
```

## `OpenRuntimeTemplateFolder`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IInstalledRuntime> OpenRuntimeTemplateFolder(Guid avatarId, IInstalledRuntime Runtime)
        {
            return ProcessResult(base.OpenOAPPSystemHolonFolder(avatarId, (InstalledRuntime)Runtime));
        }
```

## `OpenRuntimeTemplateFolderAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IInstalledRuntime>> OpenRuntimeTemplateFolderAsync(Guid avatarId, Guid RuntimeTemplateId, int versionSequence, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.OpenOAPPSystemHolonFolderAsync(avatarId, RuntimeTemplateId, versionSequence, providerType));
        }
```

## `PublishOAPPTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IOAPPTemplate>> PublishOAPPTemplate(Guid avatarId, string fullPathToOAPPTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateOAPPTemplateBinary = true, bool uploadOAPPTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToOAPPTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateOAPPTemplateBinary, uploadOAPPTemplateToCloud, edit, providerType, oappBinaryProviderType));
        }
```

## `PublishRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> PublishRuntimeTemplate(Guid avatarId, string fullPathToRuntimeTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeTemplateBinary = true, bool uploadRuntimeTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(base.Publish(avatarId, fullPathToRuntimeTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateRuntimeTemplateBinary, uploadRuntimeTemplateToCloud, edit, providerType, oappBinaryProviderType));
        }
```

## `PublishRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> PublishRuntimeTemplateAsync(Guid avatarId, string fullPathToRuntimeTemplate, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool generateRuntimeTemplateBinary = true, bool uploadRuntimeTemplateToCloud = false, bool edit = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS)
        {
            return ProcessResult(await base.PublishAsync(avatarId, fullPathToRuntimeTemplate, launchTarget, fullPathToPublishTo, registerOnSTARNET, generateRuntimeTemplateBinary, uploadRuntimeTemplateToCloud, edit, providerType, oappBinaryProviderType));
        }
```

## `ReadOAPPDNAFromPublishedOAPPFile`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPDNAFromPublishedOAPPFile(string fullPathToOAPPFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromPublishedFile(fullPathToOAPPFolder);
        }
```

## `ReadOAPPDNAFromPublishedOAPPFileAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPDNAFromPublishedOAPPFileAsync(string fullPathToOAPPFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromPublishedFileAsync(fullPathToOAPPFolder);
        }
```

## `ReadOAPPDNAFromSourceOrInstalledFolder`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPDNAFromSourceOrInstalledFolder(string fullPathToOAPPFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolder(fullPathToOAPPFolder);
        }
```

## `ReadOAPPDNAFromSourceOrInstalledFolderAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPDNAFromSourceOrInstalledFolderAsync(string fullPathToOAPPFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync(fullPathToOAPPFolder);
        }
```

## `ReadOAPPSystemHolonDNAFromPublishedFile`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs` at `b51741f46^`

```csharp
        protected OASISResult<IOAPPSystemHolonDNA> ReadOAPPSystemHolonDNAFromPublishedFile(string fullPathToPublishedFile)
        {
            OASISResult<IOAPPSystemHolonDNA> result = new OASISResult<IOAPPSystemHolonDNA>();
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

                result.Result = JsonSerializer.Deserialize<OAPPSystemHolonDNA>(File.ReadAllText(Path.Combine(tempPath, OAPPSystemHolonDNAFileName)));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {OAPPSystemHolonDNAFileName} in the {fullPathToPublishedFile} file in ReadOAPPSystemHolonDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }
```

## `ReadOAPPSystemHolonDNAFromPublishedFileAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs` at `b51741f46^`

```csharp
        protected async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPSystemHolonDNAFromPublishedFileAsync(string fullPathToPublishedFile)
        {
            OASISResult<IOAPPSystemHolonDNA> result = new OASISResult<IOAPPSystemHolonDNA>();
            string tempPath = "";

            try
            {
                tempPath = Path.GetTempPath();
                tempPath = Path.Combine(tempPath, "tmp_oapp_system_holon");

                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);

                ZipFile.ExtractToDirectory(fullPathToPublishedFile, tempPath, Encoding.Default, true);

                result.Result = JsonSerializer.Deserialize<OAPPSystemHolonDNA>(await File.ReadAllTextAsync(Path.Combine(tempPath, OAPPSystemHolonDNAFileName)));
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured reading the {OAPPSystemHolonDNAFileName} in the {fullPathToPublishedFile} file in ReadOAPPSystemHolonDNAFromPublishedFile: Reason: {e.Message}");
            }
            finally
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath, true);
            }

            return result;
        }
```

## `ReadOAPPTemplateDNAFromPublishedOAPPTemplateFile`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPTemplateDNAFromPublishedOAPPTemplateFile(string fullPathToOAPPTemplateFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromPublishedFile(fullPathToOAPPTemplateFolder);
        }
```

## `ReadOAPPTemplateDNAFromPublishedOAPPTemplateFileAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPTemplateDNAFromPublishedOAPPTemplateFileAsync(string fullPathToOAPPTemplateFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromPublishedFileAsync(fullPathToOAPPTemplateFolder);
        }
```

## `ReadOAPPTemplateDNAFromSourceOrInstalledFolder`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<IOAPPSystemHolonDNA> ReadOAPPTemplateDNAFromSourceOrInstalledFolder(string fullPathToOAPPTemplateFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolder(fullPathToOAPPTemplateFolder);
        }
```

## `ReadOAPPTemplateDNAFromSourceOrInstalledFolderAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadOAPPTemplateDNAFromSourceOrInstalledFolderAsync(string fullPathToOAPPTemplateFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync(fullPathToOAPPTemplateFolder);
        }
```

## `ReadRuntimeDNAFromPublishedRuntimeFile`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<IOAPPSystemHolonDNA> ReadRuntimeDNAFromPublishedRuntimeFile(string fullPathToRuntimeFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromPublishedFile(fullPathToRuntimeFolder);
        }
```

## `ReadRuntimeDNAFromPublishedRuntimeFileAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadRuntimeDNAFromPublishedRuntimeFileAsync(string fullPathToRuntimeFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromPublishedFileAsync(fullPathToRuntimeFolder);
        }
```

## `ReadRuntimeDNAFromSourceOrInstalledFolder`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<IOAPPSystemHolonDNA> ReadRuntimeDNAFromSourceOrInstalledFolder(string fullPathToRuntimeFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolder(fullPathToRuntimeFolder);
        }
```

## `ReadRuntimeDNAFromSourceOrInstalledFolderAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadRuntimeDNAFromSourceOrInstalledFolderAsync(string fullPathToRuntimeFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync(fullPathToRuntimeFolder);
        }
```

## `ReadRuntimeTemplateDNAFromPublishedRuntimeTemplateFile`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IOAPPSystemHolonDNA> ReadRuntimeTemplateDNAFromPublishedRuntimeTemplateFile(string fullPathToRuntimeTemplateFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromPublishedFile(fullPathToRuntimeTemplateFolder);
        }
```

## `ReadRuntimeTemplateDNAFromPublishedRuntimeTemplateFileAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadRuntimeTemplateDNAFromPublishedRuntimeTemplateFileAsync(string fullPathToRuntimeTemplateFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromPublishedFileAsync(fullPathToRuntimeTemplateFolder);
        }
```

## `ReadRuntimeTemplateDNAFromSourceOrInstalledFolder`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IOAPPSystemHolonDNA> ReadRuntimeTemplateDNAFromSourceOrInstalledFolder(string fullPathToRuntimeTemplateFolder)
        {
            return base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolder(fullPathToRuntimeTemplateFolder);
        }
```

## `ReadRuntimeTemplateDNAFromSourceOrInstalledFolderAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IOAPPSystemHolonDNA>> ReadRuntimeTemplateDNAFromSourceOrInstalledFolderAsync(string fullPathToRuntimeTemplateFolder)
        {
            return await base.ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync(fullPathToRuntimeTemplateFolder);
        }
```

## `RemoveFromAutoFailOverListNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public OASISResult<bool> RemoveFromAutoFailOverListNew(ProviderType providerType)
    {
        return NewProviderManager.RemoveFromAutoFailOverList(providerType);
    }
```

## `RemoveFromAutoLoadBalanceListNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public OASISResult<bool> RemoveFromAutoLoadBalanceListNew(ProviderType providerType)
    {
        return NewProviderManager.RemoveFromAutoLoadBalanceList(providerType);
    }
```

## `RemoveFromAutoReplicationListNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public OASISResult<bool> RemoveFromAutoReplicationListNew(ProviderType providerType)
    {
        return NewProviderManager.RemoveFromAutoReplicationList(providerType);
    }
```

## `RepublishRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> RepublishRuntimeTemplate(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Republish(avatarId, (Runtime)Runtime, providerType));
        }
```

## `RepublishRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> RepublishRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.RepublishAsync(avatarId, (Runtime)Runtime, providerType));
        }
```

## `SaveRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> SaveRuntimeTemplate(Guid avatarId, IRuntime oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Save(avatarId, (Runtime)oappTemplate, providerType));
        }
```

## `SaveRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> SaveRuntimeTemplateAsync(Guid avatarId, IRuntime oappTemplate, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.SaveAsync(avatarId, (Runtime)oappTemplate, providerType));
        }
```

## `SearchRuntimeTemplates`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IEnumerable<IRuntime>> SearchRuntimeTemplates(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(base.Search(avatarId, searchTerm, HolonType.Runtime, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }
```

## `SearchRuntimeTemplatesAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IEnumerable<IRuntime>>> SearchRuntimeTemplatesAsync(Guid avatarId, string searchTerm, bool searchOnlyForCurrentAvatar = true, bool showAllVersions = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResults(await base.SearchAsync(avatarId, searchTerm, HolonType.Runtime, searchOnlyForCurrentAvatar, showAllVersions, version, providerType));
        }
```

## `SelectOptimalProviderForLoadBalancingNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public EnumValue<ProviderType> SelectOptimalProviderForLoadBalancingNew(LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
    {
        return NewProviderManager.SelectOptimalProviderForLoadBalancing(strategy);
    }
```

## `SwitchStorageProviderAsyncNew`

From `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` at `f2b2cebf2^`

```csharp
    public async Task<OASISResult<bool>> SwitchStorageProviderAsyncNew(ProviderType newProviderType)
    {
        return await NewProviderManager.SwitchStorageProviderAsync(newProviderType);
    }
```

## `UninstallRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IInstalledRuntime> UninstallRuntimeTemplate(Guid avatarId, IInstalledRuntime installedRuntimeTemplate, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(base.Uninstall(avatarId, (InstalledRuntime)installedRuntimeTemplate, errorMessage, providerType));
        }
```

## `UninstallRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IInstalledRuntime>> UninstallRuntimeTemplateAsync(IInstalledRuntime installedRuntimeTemplate, Guid avatarId, string errorMessage, ProviderType providerType)
        {
            return ProcessResult(await base.UninstallAsync(avatarId, (InstalledRuntime)installedRuntimeTemplate, errorMessage, providerType));
        }
```

## `UnpublishRuntimeTemplate`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<IRuntime> UnpublishRuntimeTemplate(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Unpublish(avatarId, (Runtime)Runtime, providerType));
        }
```

## `UnpublishRuntimeTemplateAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<IRuntime>> UnpublishRuntimeTemplateAsync(Guid avatarId, IRuntime Runtime, ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.UnpublishAsync(avatarId, (Runtime)Runtime, providerType));
        }
```

## `WriteOAPPDNA`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<bool> WriteOAPPDNA(IOAPPSystemHolonDNA OAPPDNA, string fullPathToOAPP)
        {
            return base.WriteOAPPSystemHolonDNA(OAPPDNA, fullPathToOAPP);
        }
```

## `WriteOAPPDNAAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<bool>> WriteOAPPDNAAsync(IOAPPSystemHolonDNA OAPPDNA, string fullPathToOAPP)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(OAPPDNA, fullPathToOAPP);
        }
```

## `WriteOAPPSystemHolonDNA`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs` at `3fdf511ee^`

```csharp
        protected OASISResult<bool> WriteOAPPSystemHolonDNA(IOAPPSystemHolonDNA OAPPSystemHolonDNA, string fullPathToOAPPSystemHolon)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!Directory.Exists(fullPathToOAPPSystemHolon))
                    Directory.CreateDirectory(fullPathToOAPPSystemHolon);

                File.WriteAllText(Path.Combine(fullPathToOAPPSystemHolon, OAPPSystemHolonDNAFileName), JsonSerializer.Serialize((OAPPSystemHolonDNA)OAPPSystemHolonDNA));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {OAPPSystemHolonUIName} DNA in WriteOAPPSystemHolonDNA: Reason: {ex.Message}");
            }

            return result;
        }
```

## `WriteOAPPSystemHolonDNAAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs` at `3fdf511ee^`

```csharp
        protected async Task<OASISResult<bool>> WriteOAPPSystemHolonDNAAsync(IOAPPSystemHolonDNA OAPPSystemHolonDNA, string fullPathToOAPPSystemHolon)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                JsonSerializerOptions options = new()
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    WriteIndented = true
                };

                if (!Directory.Exists(fullPathToOAPPSystemHolon))
                    Directory.CreateDirectory(fullPathToOAPPSystemHolon);

                await File.WriteAllTextAsync(Path.Combine(fullPathToOAPPSystemHolon, OAPPSystemHolonDNAFileName), JsonSerializer.Serialize((OAPPSystemHolonDNA)OAPPSystemHolonDNA, options));
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An error occured writing the {OAPPSystemHolonUIName} DNA in WriteOAPPSystemHolonDNAAsync: Reason: {ex.Message}");
            }

            return result;
        }
```

## `WriteOAPPTemplateDNA`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<bool> WriteOAPPTemplateDNA(IOAPPSystemHolonDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            return base.WriteOAPPSystemHolonDNA(OAPPTemplateDNA, fullPathToOAPPTemplate);
        }
```

## `WriteOAPPTemplateDNAAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<bool>> WriteOAPPTemplateDNAAsync(IOAPPSystemHolonDNA OAPPTemplateDNA, string fullPathToOAPPTemplate)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(OAPPTemplateDNA, fullPathToOAPPTemplate);
        }
```

## `WriteRuntimeDNA`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `3fdf511ee^`

```csharp
        public OASISResult<bool> WriteRuntimeDNA(IOAPPSystemHolonDNA RuntimeDNA, string fullPathToRuntime)
        {
            return base.WriteOAPPSystemHolonDNA(RuntimeDNA, fullPathToRuntime);
        }
```

## `WriteRuntimeDNAAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `3fdf511ee^`

```csharp
        public async Task<OASISResult<bool>> WriteRuntimeDNAAsync(IOAPPSystemHolonDNA RuntimeDNA, string fullPathToRuntime)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(RuntimeDNA, fullPathToRuntime);
        }
```

## `WriteRuntimeTemplateDNA`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public OASISResult<bool> WriteRuntimeTemplateDNA(IOAPPSystemHolonDNA RuntimeTemplateDNA, string fullPathToRuntimeTemplate)
        {
            return base.WriteOAPPSystemHolonDNA(RuntimeTemplateDNA, fullPathToRuntimeTemplate);
        }
```

## `WriteRuntimeTemplateDNAAsync`

From `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` at `b51741f46^`

```csharp
        public async Task<OASISResult<bool>> WriteRuntimeTemplateDNAAsync(IOAPPSystemHolonDNA RuntimeTemplateDNA, string fullPathToRuntimeTemplate)
        {
            return await base.WriteOAPPSystemHolonDNAAsync(RuntimeTemplateDNA, fullPathToRuntimeTemplate);
        }
```

