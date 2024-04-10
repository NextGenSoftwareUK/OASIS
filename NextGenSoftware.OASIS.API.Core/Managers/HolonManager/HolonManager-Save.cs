using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        public OASISResult<IHolon> SaveHolon(IHolon holon, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = SaveHolonForProviderType(PrepareHolonForSaving(holon, avatarId, false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((result.IsError || result.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonForListOfProviders(holon, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            
            else if (ProviderManager.Instance.IsAutoReplicationEnabled)
            { 
                result = SaveHolonForListOfProviders(holon, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                if (result.InnerMessages.Count > 0)
                    HandleSaveHolonErrorForAutoReplicateList(ref result);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            return result;
        }

        public OASISResult<T> SaveHolon<T>(IHolon holon, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>((T)holon);
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();

            holonSaveResult = SaveHolonForProviderType(PrepareHolonForSaving(holon, avatarId, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonForListOfProviders(holon, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            else
            {
                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonSaveResult.Result, result.Result);

                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                { 
                    result = SaveHolonForListOfProviders(holon, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            return result;
        }

        
        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) 
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await SaveHolonForProviderTypeAsync(PrepareHolonForSaving(holon, avatarId,false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((result.IsError || result.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonForListOfProvidersAsync(holon, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);

            else if (ProviderManager.Instance.IsAutoReplicationEnabled)
            { 
                result = await SaveHolonForListOfProvidersAsync(holon, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                if (result.InnerMessages.Count > 0)
                    HandleSaveHolonErrorForAutoReplicateList(ref result);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            return result;
        }


        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon holon, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>((T)holon);
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();

            holonSaveResult = await SaveHolonForProviderTypeAsync(PrepareHolonForSaving(holon, avatarId, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                OASISErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(holonSaveResult.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonForListOfProvidersAsync(holon, result, providerType, ProviderManager.Instance.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            else
            {
                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonSaveResult.Result, result.Result);

                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                { 
                    result = await SaveHolonForListOfProvidersAsync(holon, result, providerType, ProviderManager.Instance.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            return result;
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
            return result;
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

            return result;
        }

        
        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
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

            result = await SaveHolonsForProviderTypeAsync(PrepareHolonsForSaving(holons, avatarId, false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

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
            return result;
        }

        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<IEnumerable<T>>> SaveHolonsAsync<T>(IEnumerable<T> holons, Guid avatarId, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
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

            holonSaveResult = await SaveHolonsForProviderTypeAsync(PrepareHolonsForSaving(holons, avatarId, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

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
            return result;
        }
    }
}