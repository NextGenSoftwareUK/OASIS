using System;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        private OASISResult<IEnumerable<IHolon>> LoadHolonsForProviderTypeByMetaData(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
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
                    result = providerResult.Result.LoadHolonsByMetaData(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadHolonsForProviderTypeByMetaData<T>(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with metaKey ", metaKey, " and metaValue, ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IEnumerable<IHolon>> holonResult = providerResult.Result.LoadHolonsByMetaData(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
                        result.IsLoaded = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonResult.Message}");
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
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForProviderTypeByMetaDataAsync(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
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
                    result = await providerResult.Result.LoadHolonsByMetaDataAsync(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForProviderTypeByMetaDataAsync<T>(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
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
                    //T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IEnumerable<IHolon>> loadHolonsResult = await providerResult.Result.LoadHolonsByMetaDataAsync(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (!loadHolonsResult.IsError && loadHolonsResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(loadHolonsResult.Result);
                        result.IsLoaded = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = loadHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> LoadHolonsForProviderTypeByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
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
                    result = providerResult.Result.LoadHolonsByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons with metaKeyValuePairs and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadHolonsForProviderTypeByMetaData<T>(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons with metaKeyValuePairs and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IEnumerable<IHolon>> holonResult = providerResult.Result.LoadHolonsByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
                        result.IsLoaded = true;
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonResult.Message}");
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
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForProviderTypeByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
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
                    result = await providerResult.Result.LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons with metaKeyValuePairs and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForProviderTypeByMetaDataAsync<T>(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
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
                    //T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IEnumerable<IHolon>> loadHolonsResult = await providerResult.Result.LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (!loadHolonsResult.IsError && loadHolonsResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(loadHolonsResult.Result);
                        result.IsLoaded = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = loadHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons with metaKeyValuePairs and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }
    }
}
