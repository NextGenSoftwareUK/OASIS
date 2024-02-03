using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        //TODO: Why do we pass in result?! Need to look into this tomorrow! ;-) lol
        private OASISResult<IHolon> SaveHolonForProviderType(IHolon holon, ProviderType providerType, OASISResult<IHolon> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                    OASISResult<IHolon> saveHolonResult = providerResult.Result.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (!saveHolonResult.IsError && saveHolonResult != null)
                    {
                        result.Result = saveHolonResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex.ToString()}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> SaveHolonForProviderTypeAsync(IHolon holon, ProviderType providerType, OASISResult<IHolon> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) //TODO: Dont think this should be an optional param?!
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                    OASISResult<IHolon> saveHolonResult = await providerResult.Result.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (!saveHolonResult.IsError && saveHolonResult != null)
                    {
                        result.Result = saveHolonResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex.ToString()}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> SaveHolonsForProviderType(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    foreach (IHolon holon in holons)
                        holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = providerResult.Result.SaveHolons(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        result.Result = saveHolonsResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to save the holons in the SaveHolons method using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsForProviderTypeAsync(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    foreach (IHolon holon in holons) 
                        holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = await providerResult.Result.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        result.Result = saveHolonsResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occured attempting to save the holons in the SaveHolonsAsync method for the {Enum.GetName(providerType)} provider. Reason: {ex.ToString()}";

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> SaveHolonsForListOfProviders<T>(IEnumerable<IHolon> holons, OASISResult<IEnumerable<T>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon
        {
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            ProviderType originalCurrentProvider = ProviderManager.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = SaveHolonsForProviderType(holons, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> SaveHolonsForListOfProvidersAsync<T>(IEnumerable<IHolon> holons, OASISResult<IEnumerable<T>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon
        {
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            ProviderType originalCurrentProvider = ProviderManager.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonsForProviderTypeAsync(holons, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private OASISResult<T> SaveHolonForListOfProviders<T>(IHolon holon, OASISResult<T> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon
        {
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();
            ProviderType originalCurrentProvider = ProviderManager.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                { 
                    holonSaveResult = SaveHolonForProviderType(holon, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private async Task<OASISResult<T>> SaveHolonForListOfProvidersAsync<T>(IHolon holon, OASISResult<T> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon
        {
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();
            ProviderType originalCurrentProvider = ProviderManager.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonForProviderTypeAsync(holon, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }
    }
}