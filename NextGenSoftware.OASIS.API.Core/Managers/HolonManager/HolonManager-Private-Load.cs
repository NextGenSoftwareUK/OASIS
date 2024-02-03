using System;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        private OASISResult<IHolon> LoadHolonForProviderType(Guid id, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
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
                    result = providerResult.Result.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for id ", id, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<T> LoadHolonForProviderType<T>(Guid id, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for id ", id, " using the ", Enum.GetName(providerType), " provider.");

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
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T));
                    OASISResult<IHolon> holonResult = providerResult.Result.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version);

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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        //TODO: Finish Upgrading rest of HolonManager to work with this improved code (from AvatarManager):
        private async Task<OASISResult<IHolon>> LoadHolonForProviderTypeAsync(Guid id, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
        {
            string errorMessageTemplate = "Error in LoadHolonForProviderTypeAsync method in HolonManager loading holon with id {0} for provider {1}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, id, providerType);

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, id, ProviderManager.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    var task = providerResult.Result.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == task)
                    {
                        if (task.Result.IsError || task.Result.Result == null)
                        {
                            if (string.IsNullOrEmpty(task.Result.Message))
                                task.Result.Message = "No Holon Found.";

                            OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, task.Result.Message), task.Result.DetailedMessage);
                        }
                        else
                        {
                            result.IsLoaded = true;
                            result.Result = task.Result.Result;
                        }
                    }
                    else
                        OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, "timeout occured."));
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleWarning(ref result, string.Concat(errorMessage, ex.Message));
            }

            return result;
        }

        private async Task<OASISResult<T>> LoadHolonForProviderTypeAsync<T>(Guid id, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for id ", id, " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IHolon> holonResult = await providerResult.Result.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);

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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IHolon> LoadHolonForProviderType(string providerKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
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
                    result = providerResult.Result.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for providerKey ", providerKey, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<T> LoadHolonForProviderType<T>(string providerKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for providerKey ", providerKey, " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IHolon> holonResult = providerResult.Result.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);

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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> LoadHolonForProviderTypeAsync(string providerKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
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
                    result = await providerResult.Result.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for providerKey ", providerKey, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private async Task<OASISResult<T>> LoadHolonForProviderTypeAsync<T>(string providerKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for providerKey ", providerKey, " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IHolon> holonResult = await providerResult.Result.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);

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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IHolon> LoadHolonForProviderTypeByCustomKey(string customKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
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
                    result = providerResult.Result.LoadHolonByCustomKey(customKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for customKey ", customKey, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<T> LoadHolonForProviderTypeByCustomKey<T>(string customKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for customKey ", customKey, " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IHolon> holonResult = providerResult.Result.LoadHolonByCustomKey(customKey, loadChildren, recursive, maxChildDepth, continueOnError, version);

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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> LoadHolonForProviderTypeByCustomKeyAsync(string customKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
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
                    result = await providerResult.Result.LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for customKey ", customKey, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private async Task<OASISResult<T>> LoadHolonForProviderTypeByCustomKeyAsync<T>(string customKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for customKey ", customKey, " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IHolon> holonResult = await providerResult.Result.LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, version);

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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IHolon> LoadHolonForProviderTypeByMetaData(string metaKey, string metaValue, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
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
                    result = providerResult.Result.LoadHolonByMetaData(metaKey, metaValue, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for metaKey ", metaKey, " and ", metaValue, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private OASISResult<T> LoadHolonForProviderTypeByMetaData<T>(string metaKey, string metaValue, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for metaKey ", metaKey, " and metaValue ", metaValue, " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IHolon> holonResult = providerResult.Result.LoadHolonByMetaData(metaKey, metaValue, loadChildren, recursive, maxChildDepth, continueOnError, version);

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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> LoadHolonForProviderTypeByMetaDataAsync(string metaKey, string metaValue, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
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
                    result = await providerResult.Result.LoadHolonByMetaDataAsync(metaKey, metaValue, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsLoaded = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for metaKey ", metaKey, " and metaValue ", metaValue, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

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

        private async Task<OASISResult<T>> LoadHolonForProviderTypeByMetaDataAsync<T>(string metaKey, string metaValue, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for metaKey ", metaKey, " and metaValue ", metaValue, " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IHolon> holonResult = await providerResult.Result.LoadHolonByMetaDataAsync(metaKey, metaValue, loadChildren, recursive, maxChildDepth, continueOnError, version);

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
                    result.Result = default(T);
                    OASISErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }
    }
}