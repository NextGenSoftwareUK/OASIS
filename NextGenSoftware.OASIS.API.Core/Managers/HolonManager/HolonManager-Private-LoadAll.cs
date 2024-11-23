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
        private OASISResult<IEnumerable<IHolon>> LoadAllHolonsForProviderType(HolonType holonType, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

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
                    result = providerResult.Result.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<IEnumerable<T>> LoadAllHolonsForProviderType<T>(HolonType holonType, ProviderType providerType, OASISResult<IEnumerable<T>> result, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

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
                    //T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?

                    OASISResult<IEnumerable<IHolon>> holonsResult = providerResult.Result.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider,version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper.MapBaseHolonPropertiesAndCreateT2IfNull<IHolon, T>(holonsResult.Result, result.Result);
                        result.IsLoaded = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Reason: ", holonsResult != null ? holonsResult.Message : ""));
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

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

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsForProviderTypeAsync(HolonType holonType, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            try
            {
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
                    result = await providerResult.Result.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsForProviderTypeAsync<T>(HolonType holonType, ProviderType providerType, OASISResult<IEnumerable<T>> result, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
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
                    //T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IEnumerable<IHolon>> holonsResult = await providerResult.Result.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper.MapBaseHolonPropertiesAndCreateT2IfNull<IHolon, T>(holonsResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        //TODO: Need to make this bug fix everywhere! ;-)
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Reason: ", holonsResult != null ? holonsResult.Message :""));
                        //OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonsResult != null ? holonsResult.Message :}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

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

        private OASISResult<IEnumerable<T>> LoadChildHolonsRecursiveForAllHolons<T>(OASISResult<IEnumerable<T>> result, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return LoadChildHolonsRecursive(result, $"All holons with type {Enum.GetName(typeof(HolonType), holonType)} loaded fine but one or more of their children failed to load.", holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadChildHolonsRecursiveForAllHolonsAsync<T>(OASISResult<IEnumerable<T>> result, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return await LoadChildHolonsRecursiveAsync(result, $"All holons with type {Enum.GetName(typeof(HolonType), holonType)} loaded fine but one or more of their children failed to load.", holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        }

        private OASISResult<IEnumerable<IHolon>> LoadChildHolonsRecursiveForAllHolons(OASISResult<IEnumerable<IHolon>> result, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default)
        {
            return LoadChildHolonsRecursive(result, $"All holons with type {Enum.GetName(typeof(HolonType), holonType)} loaded fine but one or more of their children failed to load.", holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsRecursiveForAllHolonsAsync(OASISResult<IEnumerable<IHolon>> result, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default)
        {
            return await LoadChildHolonsRecursiveAsync(result, $"All holons with type {Enum.GetName(typeof(HolonType), holonType)} loaded fine but one or more of their children failed to load.", holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        }
    }
}