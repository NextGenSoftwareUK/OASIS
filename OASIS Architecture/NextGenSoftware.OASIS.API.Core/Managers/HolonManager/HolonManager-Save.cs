using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        public OASISResult<Guid> SaveFile(byte[] data, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();
            string errorMessage = "An error occured in HolonManager.SaveFile. Reason: ";

            OASISResult<IHolon> holonResult = SaveHolon(new Holon() 
            { 
                MetaData = new Dictionary<string, object>()
                {
                    { "data",  data }
                }
            }, avatarId, true, true, 0, true, false, providerType);
            
            if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(holonResult, result);
                result.Result = holonResult.Result.Id;
                result.Message = "File Saved";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error saving the holon containing the file. Reason: {holonResult.Message}");

            return result;
        }

        public async Task<OASISResult<Guid>> SaveFileAsync(byte[] data, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();
            string errorMessage = "An error occured in HolonManager.SaveFileAsync. Reason: ";

            OASISResult<IHolon> holonResult = await SaveHolonAsync(new Holon()
            {
                MetaData = new Dictionary<string, object>()
                {
                    { "data",  data }
                }
            }, avatarId, true, true, 0, true, false, providerType);

            if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(holonResult, result);
                result.Result = holonResult.Result.Id;
                result.Message = "File Saved";
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error saving the holon containing the file. Reason: {holonResult.Message}");

            return result;
        }

        public OASISResult<byte[]> LoadFile(Guid id, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();
            string errorMessage = "An error occured in HolonManager.LoadFile. Reason: ";

            //OASISResult<IHolon> holonResult = LoadHolon(id, avatarId, true, true, 0, true, false, providerType);
            OASISResult<IHolon> holonResult = LoadHolon(id, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(holonResult, result);
                
                if (holonResult.Result.MetaData != null && holonResult.Result.MetaData.ContainsKey("data") && holonResult.Result.MetaData["data"] != null)
                {
                    result.Result = holonResult.Result.MetaData["data"] as byte[];
                    result.Message = "File Loaded";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error loading the metadata containing the file (metadata or metadata key not found).");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error loading the holon containing the file. Reason: {holonResult.Message}");

            return result;
        }

        public async Task<OASISResult<byte[]>> LoadFileAsync(Guid id, Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();
            string errorMessage = "An error occured in HolonManager.LoadFileAsync. Reason: ";

            //OASISResult<IHolon> holonResult = LoadHolon(id, avatarId, true, true, 0, true, false, providerType);
            OASISResult<IHolon> holonResult = await LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
            {
                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(holonResult, result);

                if (holonResult.Result.MetaData != null && holonResult.Result.MetaData.ContainsKey("data") && holonResult.Result.MetaData["data"] != null)
                {
                    result.Result = holonResult.Result.MetaData["data"] as byte[];
                    result.Message = "File Loaded";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error loading the metadata containing the file (metadata or metadata key not found).");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error loading the holon contianing the file. Reason: {holonResult.Message}");

            return result;
        }

        //public OASISResult<Guid> SaveIPFSFile(byte[] data, Guid avatarId)
        //{
        //    OASISResult<Guid> result = new OASISResult<Guid>();
        //    string errorMessage = "An error occured in HolonManager.SaveIPFSFile. Reason: ";

        //    IPFSProvider OASISProvider = ProviderManager.Instance.GetProvider(ProviderType.IPFSOASIS);

        //    if (OASISProvider != null)
        //    {

        //    }

        //    if (holonResult != null && holonResult.Result != null && !holonResult.IsError)
        //    {
        //        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(holonResult, result);
        //        result.Result = holonResult.Result.Id;
        //        result.Message = "File Saved";
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error saving the holon contianing the file. Reason: {holonResult.Message}");

        //    return result;
        //}

        public OASISResult<IHolon> SaveHolon(IHolon holon, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = SaveHolonForProviderType(PrepareHolonForSaving(holon, avatarId, false), avatarId, providerType, result, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((result.IsError || result.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonForListOfProviders(holon, avatarId, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            
            else if (ProviderManager.Instance.IsAutoReplicationEnabled)
            { 
                result = SaveHolonForListOfProviders(holon, avatarId, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                if (result.InnerMessages.Count > 0)
                    HandleSaveHolonErrorForAutoReplicateList(ref result);
            }

            // Auto-Load Balancing: Distribute load across multiple providers
            if (ProviderManager.Instance.IsAutoLoadBalanceEnabled && !result.IsError && result.Result != null)
            {
                var loadBalancedProvider = ProviderManager.Instance.SelectOptimalProviderForLoadBalancing();
                if (loadBalancedProvider.Value != currentProviderType)
                {
                    // Save to load-balanced provider for better performance distribution
                    var loadBalanceResult = SaveHolonForProviderType(holon, avatarId, loadBalancedProvider.Value, new OASISResult<IHolon>(), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    
                    if (!loadBalanceResult.IsError && loadBalanceResult.Result != null)
                    {
                        result.InnerMessages.Add($"Auto-load balanced to {loadBalancedProvider.Name} provider for optimal performance");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            result.Result = RestoreCelesialBodies(result.Result);
            return result;
        }

        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            return SaveHolon(holon, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        }

        public OASISResult<T> SaveHolon<T>(IHolon holon, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>((T)holon);
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();

            holonSaveResult = SaveHolonForProviderType(PrepareHolonForSaving(holon, avatarId, true), avatarId, providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonForListOfProviders(holon, avatarId, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            else
            {
                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonSaveResult.Result, result.Result);

                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                { 
                    result = SaveHolonForListOfProviders(holon, avatarId, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            result.Result = RestoreCelesialBodies(result.Result);
            return result;
        }

        public OASISResult<T> SaveHolon<T>(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return SaveHolon<T>(holon, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        }

        // Implemented OASISResult format for all Holon/Avatar Manager methods
        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) 
        {
            // HyperDrive v2 routing with safe fallback to legacy
            try
            {
                var dna = OASISDNAManager.OASISDNA;
                if (dna?.OASIS?.HyperDriveMode == "OASISHyperDrive2")
                {
                    var hyperDrive = new OASISHyperDrive();
                    var request = new StorageOperationRequest
                    {
                        Operation = "SaveHolon",
                        AvatarId = avatarId,
                        Payload = holon,
                        PreferredProvider = providerType
                    };

                    var hdResult = await hyperDrive.RouteRequestAsync<IHolon>(request, LoadBalancingStrategy.Auto);
                    if (hdResult != null && !hdResult.IsError && hdResult.Result != null)
                        return hdResult;
                }
            }
            catch { /* fallback to legacy */ }

            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await SaveHolonForProviderTypeAsync(PrepareHolonForSaving(holon, avatarId, false), avatarId, providerType, result, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((result.IsError || result.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonForListOfProvidersAsync(holon, avatarId, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);

            else if (ProviderManager.Instance.IsAutoReplicationEnabled)
            { 
                result = await SaveHolonForListOfProvidersAsync(holon, avatarId, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                if (result.InnerMessages.Count > 0)
                    HandleSaveHolonErrorForAutoReplicateList(ref result);
            }

            // Auto-Load Balancing: Distribute load across multiple providers
            if (ProviderManager.Instance.IsAutoLoadBalanceEnabled && !result.IsError && result.Result != null)
            {
                var loadBalancedProvider = ProviderManager.Instance.SelectOptimalProviderForLoadBalancing();
                if (loadBalancedProvider.Value != currentProviderType)
                {
                    // Save to load-balanced provider for better performance distribution
                    var loadBalanceResult = await SaveHolonForProviderTypeAsync(holon, avatarId, loadBalancedProvider.Value, new OASISResult<IHolon>(), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    
                    if (!loadBalanceResult.IsError && loadBalanceResult.Result != null)
                    {
                        result.InnerMessages.Add($"Auto-load balanced to {loadBalancedProvider.Name} provider for optimal performance");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            result.Result = RestoreCelesialBodies(result.Result);
            return result;
        }

        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            return await SaveHolonAsync(holon, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        }

        // Implemented OASISResult format for all Holon/Avatar Manager methods
        public async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon holon, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>((T)holon);
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();

            result = await SaveHolonForProviderTypeAsync(PrepareHolonForSaving(holon, avatarId, true), avatarId, providerType, result, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((result.IsError || result.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                //OASISErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonForListOfProvidersAsync(holon, avatarId, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            else
            {
                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonSaveResult.Result, result.Result);

                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                { 
                    result = await SaveHolonForListOfProvidersAsync(holon, avatarId, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            result.Result = RestoreCelesialBodies(result.Result);
            return result;
        }

        public async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return await SaveHolonAsync<T>(holon, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (holons.Count() == 0)
            {
                result.Message = "No holons found to save.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            result = SaveHolonsForProviderType(PrepareHolonsForSaving(holons, avatarId, false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((result.IsError || result.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonsForListOfProviders(holons, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonsErrorForAutoFailOverList(ref result);
            else
            {
                //Should already be false but just in case...
                foreach (IHolon holon in result.Result)
                    holon.IsChanged = false;

                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                { 
                    result = SaveHolonsForListOfProviders(holons, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonsErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.Result = RestoreCelesialBodies(result.Result);
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            return SaveHolons(holons, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        }

        public OASISResult<IEnumerable<T>> SaveHolons<T>(IEnumerable<T> holons, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            List<T> originalHolons = new List<T>();

            if (holons.Count() == 0)
            {
                result.Message = "No holons found to save.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            foreach (IHolon holon in holons)
                originalHolons.Add((T)holon);

            holonSaveResult = SaveHolonsForProviderType(PrepareHolonsForSaving(holons, avatarId, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(holonSaveResult.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonsForListOfProviders(holons, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonsErrorForAutoFailOverList(ref result);
            else
            {
                List<IHolon> savedHolons = holonSaveResult.Result.ToList();

                for (int i=0; i < savedHolons.Count; i++)
                {
                    savedHolons[i].IsChanged = false;  //Should already be false but just in case...
                    savedHolons[i].IsNewHolon = false;

                    //Update the base holon properties that have now been updated (id, createddate, modifieddata, etc)
                    originalHolons[i] = Mapper<IHolon, T>.MapBaseHolonProperties(savedHolons[i], originalHolons[i]);
                }

                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                { 
                    result = SaveHolonsForListOfProviders(holons, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonsErrorForAutoReplicateList(ref result);
                }
            }

            result.Result = originalHolons;
            SwitchBackToCurrentProvider(currentProviderType, ref result);

            result.Result = RestoreCelesialBodies(result.Result);
            return result;
        }

        public OASISResult<IEnumerable<T>> SaveHolons<T>(IEnumerable<T> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return SaveHolons<T>(holons, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        }

        // Implemented OASISResult format for all Holon/Avatar Manager methods
        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, bool childHolonsFlattened = false, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (holons.Count() == 0)
            {
                result.Message = "No holons found to save.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            result = await SaveHolonsForProviderTypeAsync(PrepareHolonsForSaving(holons, avatarId, false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, childHolonsFlattened);

            if ((result.IsError || result.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonsForListOfProvidersAsync(holons, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonsErrorForAutoFailOverList(ref result);
            else
            {
                //Should already be false but just in case...
                foreach (IHolon holon in result.Result)
                    holon.IsChanged = false;

                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                { 
                    result = await SaveHolonsForListOfProvidersAsync(holons, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonsErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.Result = RestoreCelesialBodies(result.Result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, bool childHolonsFlattened = false, ProviderType providerType = ProviderType.Default)
        {
            return await SaveHolonsAsync(holons, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, childHolonsFlattened, providerType);
        }

        // Implemented OASISResult format for all Holon/Avatar Manager methods
        public async Task<OASISResult<IEnumerable<T>>> SaveHolonsAsync<T>(IEnumerable<T> holons, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, bool childHolonsFlattened = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            List<T> originalHolons = new List<T>();

            if (holons.Count() == 0)
            {
                result.Message = "No holons found to save.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            foreach (IHolon holon in holons)
                originalHolons.Add((T)holon);

            holonSaveResult = await SaveHolonsForProviderTypeAsync(PrepareHolonsForSaving(holons, avatarId, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, childHolonsFlattened);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(holonSaveResult.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonsForListOfProvidersAsync(holons, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonsErrorForAutoFailOverList(ref result);
            else
            {
                List<IHolon> savedHolons = holonSaveResult.Result.ToList();

                for (int i = 0; i < savedHolons.Count; i++)
                {
                    savedHolons[i].IsChanged = false;  //Should already be false but just in case...
                    savedHolons[i].IsNewHolon = false;

                    //Update the base holon properties that have now been updated (id, createddate, modifieddata, etc)
                    originalHolons[i] = Mapper<IHolon, T>.MapBaseHolonProperties(savedHolons[i], originalHolons[i]);
                }

                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                {
                    result = await SaveHolonsForListOfProvidersAsync(holons, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonsErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.Result = RestoreCelesialBodies(result.Result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveHolonsAsync<T>(IEnumerable<T> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, bool childHolonsFlattened = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return await SaveHolonsAsync<T>(holons, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, childHolonsFlattened, providerType);
        }
    }
}