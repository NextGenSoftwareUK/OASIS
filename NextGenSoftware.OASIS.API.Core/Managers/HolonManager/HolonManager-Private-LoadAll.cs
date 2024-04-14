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
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?

                    OASISResult<IEnumerable<IHolon>> holonsResult = providerResult.Result.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider,version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonsResult.Result);
                        result.IsLoaded = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonsResult.Message}");
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
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IEnumerable<IHolon>> holonsResult = await providerResult.Result.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonsResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonsResult.Message}");
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
    }
}