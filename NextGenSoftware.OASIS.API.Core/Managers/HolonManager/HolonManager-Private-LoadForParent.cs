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
        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderType(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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
                    result = providerResult.Result.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderType<T>(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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

                    OASISResult<IEnumerable<IHolon>> holonsResult = providerResult.Result.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

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

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeAsync(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    result = await providerResult.Result.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeAsync<T>(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    OASISResult<IEnumerable<IHolon>> holonsResult = await providerResult.Result.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

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

        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderType(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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
                    result = providerResult.Result.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderType<T>(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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
                    OASISResult<IEnumerable<IHolon>> holonResult = providerResult.Result.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

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
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeAsync(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    result = await providerResult.Result.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeAsync<T>(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    OASISResult<IEnumerable<IHolon>> loadHolonsForParentResult = await providerResult.Result.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (!loadHolonsForParentResult.IsError && loadHolonsForParentResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(loadHolonsForParentResult.Result);
                        result.IsLoaded = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = loadHolonsForParentResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderTypeByCustomKey(string customKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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
                    result = providerResult.Result.LoadHolonsForParentByCustomKey(customKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with customKey ", customKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderTypeByCustomKey<T>(string customKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with customKey ", customKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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
                    OASISResult<IEnumerable<IHolon>> holonResult = providerResult.Result.LoadHolonsForParentByCustomKey(customKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

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
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeByCustomKeyAsync(string customKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    result = await providerResult.Result.LoadHolonsForParentByCustomKeyAsync(customKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with customKey ", customKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeByCustomKeyAsync<T>(string customKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    OASISResult<IEnumerable<IHolon>> loadHolonsForParentResult = await providerResult.Result.LoadHolonsForParentByCustomKeyAsync(customKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (!loadHolonsForParentResult.IsError && loadHolonsForParentResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(loadHolonsForParentResult.Result);
                        result.IsLoaded = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = loadHolonsForParentResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with customKey ", customKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderTypeByMetaData(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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
                    result = providerResult.Result.LoadHolonsForParentByMetaData(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
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
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderTypeByMetaData<T>(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with metaKey ", metaKey, " and metaValue, ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

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
                    OASISResult<IEnumerable<IHolon>> holonResult = providerResult.Result.LoadHolonsForParentByMetaData(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

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
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeByMetaDataAsync(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    result = await providerResult.Result.LoadHolonsForParentByMetaDataAsync(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
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
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeByMetaDataAsync<T>(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.SetAndActivateCurrentStorageProviderAsync(providerType);

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
                    OASISResult<IEnumerable<IHolon>> loadHolonsForParentResult = await providerResult.Result.LoadHolonsForParentByMetaDataAsync(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (!loadHolonsForParentResult.IsError && loadHolonsForParentResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(loadHolonsForParentResult.Result);
                        result.IsLoaded = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = loadHolonsForParentResult.Message;
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
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }
    }
}