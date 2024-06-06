using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        private OASISResult<IHolon> SaveHolonForProviderType(IHolon holon, ProviderType providerType, OASISResult<IHolon> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

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
                    OASISResult<IHolon> saveHolonResult = providerResult.Result.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

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

        private OASISResult<T> SaveHolonForProviderType<T>(IHolon holon, ProviderType providerType, OASISResult<T> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

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
                    OASISResult<IHolon> saveHolonResult = providerResult.Result.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonResult.IsError && saveHolonResult != null)
                    {
                        result.Result = (T)saveHolonResult.Result;
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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex.ToString()}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> SaveSaveHolonForProviderTypeHolonForProviderTypeAsync(IHolon holon, ProviderType providerType, OASISResult<IHolon> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) //TODO: Dont think this should be an optional param?!
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    OASISResult<IHolon> saveHolonResult = await providerResult.Result.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

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

        private async Task<OASISResult<T>> SaveHolonForProviderTypeAsync<T>(IHolon holon, Guid avatarId, ProviderType providerType, OASISResult<T> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon, new()
        {
            string errorMessage = $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the HolonManager.SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason:";
            List<string> childSaveErrors = new List<string>();

            try
            {
                HasHolonChanged((IHolon)holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                    OASISResult<IHolon> saveHolonResult = await providerResult.Result.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonResult.IsError && saveHolonResult != null)
                    {
                        if (saveChildren && !saveChildrenOnProvider)
                        {
                            currentChildDepth++;

                            if ((recursive && currentChildDepth >= maxChildDepth) || (!recursive && currentChildDepth > 1))
                                return result;

                            //TODO:Need to recursively save all children here...
                            foreach (IHolon childHolon in holon.Children)
                            {
                                OASISResult<T> saveChildResult = await SaveHolonAsync<T>(childHolon, avatarId, saveChildren, recursive, maxChildDepth, currentChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                                if (saveChildResult != null && saveChildResult.Result != null && !saveChildResult.IsError)
                                {
                                    result.SavedCount++;                                    
                                }
                                else
                                {
                                    //Make sure it DOESN'T add to the innerMessages because we need to keep these reserved for the top level holon that is saving and any auto-failover save messaged used in SaveHolon method.
                                    childSaveErrors.Add($"An error occured saving the child holon {LoggingHelper.GetHolonInfoForLogging(childHolon)} in the HolonManager.SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveChildResult.Message}");
                                    result.WarningCount++;
                                    result.IsWarning = true;
                                    //OASISErrorHandling.HandleWarning(ref result, $"An error occured saving the child holon {LoggingHelper.GetHolonInfoForLogging(childHolon)} in the HolonManager.SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {saveChildResult.Message}", false, false, false, false, true) ;
                                }
                            }

                            if (childSaveErrors.Count > 0)
                                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage} The holon {LoggingHelper.GetHolonInfoForLogging(holon)} saved fine but errors occured saving some of it's children:\n\n{OASISResultHelper.BuildInnerMessageError(childSaveErrors)}");
                        }

                        result.Result = (T)saveHolonResult.Result;
                        result.IsSaved = true;
                        result.SavedCount++;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {saveHolonResult.Message}");
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> SaveHolonsForProviderType(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

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

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = providerResult.Result.SaveHolons(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

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

        private OASISResult<IEnumerable<T>> SaveHolonsForProviderType<T>(IEnumerable<T> holons, ProviderType providerType, OASISResult<IEnumerable<T>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

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
                    List<IHolon> holonsToSave = new List<IHolon>();

                    foreach (IHolon holon in holons)
                    {
                        holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                        holonsToSave.Add(holon);
                    }

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = providerResult.Result.SaveHolons(holonsToSave, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        List<T> savedHolons = new List<T>();

                        foreach (IHolon holon in saveHolonsResult.Result)
                            savedHolons.Add((T)holon);

                        result.Result = savedHolons;
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

        private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsForProviderTypeAsync(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

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

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = await providerResult.Result.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

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

        private async Task<OASISResult<IEnumerable<T>>> SaveHolonsForProviderTypeAsync<T>(IEnumerable<T> holons, ProviderType providerType, OASISResult<IEnumerable<T>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    List<IHolon> holonsToSave = new List<IHolon>();
                    
                    foreach (IHolon holon in holons)
                    {
                        holon.InstanceSavedOnProviderType = new EnumValue<ProviderType>(providerType);
                        holonsToSave.Add(holon);
                    }

                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = await providerResult.Result.SaveHolonsAsync(holonsToSave, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        List<T> savedHolons = new List<T>();

                        foreach (IHolon holon in saveHolonsResult.Result)
                            savedHolons.Add((T)holon);

                        result.Result = savedHolons;
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

        private OASISResult<IEnumerable<T>> SaveHolonsForListOfProviders<T>(IEnumerable<T> holons, OASISResult<IEnumerable<T>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon
        {
            OASISResult<IEnumerable<T>> holonSaveResult = new OASISResult<IEnumerable<T>>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
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

        private async Task<OASISResult<IEnumerable<T>>> SaveHolonsForListOfProvidersAsync<T>(IEnumerable<T> holons, OASISResult<IEnumerable<T>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon
        {
            OASISResult<IEnumerable<T>> holonSaveResult = new OASISResult<IEnumerable<T>>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
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

        private OASISResult<T> SaveHolonForListOfProviders<T>(IHolon holon, OASISResult<T> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon
        {
            OASISResult<T> holonSaveResult = new OASISResult<T>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
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

        private async Task<OASISResult<T>> SaveHolonForListOfProvidersAsync<T>(IHolon holon, Guid avatarId, OASISResult<T> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) where T : IHolon, new()
        {
            OASISResult<T> holonSaveResult = new OASISResult<T>();
            ProviderType originalCurrentProvider = ProviderManager.Instance.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonForProviderTypeAsync<T>(holon, avatarId, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, currentChildDepth, continueOnError, saveChildrenOnProvider);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError<T>(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }
    }
}