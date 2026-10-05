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
        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderType(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
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
                    result = providerResult.Result.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
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
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderType<T>(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

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

                    OASISResult<IEnumerable<IHolon>> holonsResult = providerResult.Result.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper.MapBaseHolonPropertiesAndCreateT2IfNull<IHolon, T>(holonsResult.Result);
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
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeAsync(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            string errorMessage = string.Concat("An error occured in HolonManager.LoadHolonsForParentForProviderTypeAsync attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

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
                    //TODO: Need to apply this bug fix to ALL other methods for HolonManager and AvatarManager etc ASAP!
                    OASISResult<IEnumerable<IHolon>> providerLoadResult = await providerResult.Result.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (providerLoadResult != null && !providerLoadResult.IsError)
                    {
                        result = providerLoadResult;
                        result.IsLoaded = true;
                    }
                    else if (providerLoadResult != null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: An Error Was Returned From The Provider {Enum.GetName(typeof(ProviderType), providerType)} Calling LoadHolonsForParentAsync: {providerLoadResult.Message}");

                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: The Provider {Enum.GetName(typeof(ProviderType), providerType)} Returned Null For The LoadHolonsForParentAsync method.");
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

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeAsync<T>(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured in HolonManager.LoadHolonsForParentForProviderTypeAsync<T> attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IEnumerable<IHolon>> holonsResult = await providerResult.Result.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper.MapBaseHolonPropertiesAndCreateT2IfNull<IHolon, T>(holonsResult.Result);
                        result.IsLoaded = true;
                    }
                    else if (holonsResult != null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: An Error Was Returned From The Provider {Enum.GetName(typeof(ProviderType), providerType)} Calling LoadHolonsForParentAsync: {holonsResult.Message}");

                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: The Provider {Enum.GetName(typeof(ProviderType), providerType)} Returned Null For The LoadHolonsForParentAsync method.");
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

        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderType(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            string errorMessage = string.Concat("An error occured in HolonManager.LoadHolonsForParentForProviderType attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

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
                    //TODO: Need to apply this bug fix to ALL other methods for HolonManager and AvatarManager etc ASAP!
                    OASISResult<IEnumerable<IHolon>> providerLoadResult = providerResult.Result.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (providerLoadResult != null && !providerLoadResult.IsError)
                    {
                        result = providerLoadResult;
                        result.IsLoaded = true;
                    }
                    else if (providerLoadResult != null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: An Error Was Returned From The Provider {Enum.GetName(typeof(ProviderType), providerType)} Calling LoadHolonsForParent: {providerLoadResult.Message}");

                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: The Provider {Enum.GetName(typeof(ProviderType), providerType)} Returned Null For The LoadHolonsForParent method.");
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

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderType<T>(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

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
                    OASISResult<IEnumerable<IHolon>> holonResult = providerResult.Result.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Result = Mapper.MapBaseHolonPropertiesAndCreateT2IfNull<IHolon, T>(holonResult.Result, result.Result);
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

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeAsync(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            //string errorMessage = string.Concat("An error occured in HolonManager.LoadHolonsForParentForProviderTypeAsync attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

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
                    result = await providerResult.Result.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
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
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeAsync<T>(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
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
                    OASISResult<IEnumerable<IHolon>> loadHolonsForParentResult = await providerResult.Result.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

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
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        //private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderTypeByCustomKey(string customKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        //{
        //    try
        //    {
        //        OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

        //        if (providerResult.IsError)
        //        {
        //            LoggingManager.Log(providerResult.Message, LogType.Error);

        //            if (result != null)
        //            {
        //                result.IsError = true;
        //                result.Message = providerResult.Message;
        //            }
        //        }
        //        else if (result != null)
        //        {
        //            result = providerResult.Result.LoadHolonsByCustomKey(customKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
        //            result.IsLoaded = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = string.Concat("An error occured attempting to load the holons for parent with customKey ", customKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

        //        if (result != null)
        //        {
        //            result.Result = null;
        //            OASISErrorHandling.HandleError(ref result, errorMessage);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(errorMessage);
        //    }

        //    return result;
        //}

        //private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderTypeByCustomKey<T>(string customKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        //{
        //    string errorMessage = string.Concat("An error occured attempting to load the holons for parent with customKey ", customKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

        //    try
        //    {
        //        OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

        //        if (providerResult.IsError)
        //        {
        //            LoggingManager.Log(providerResult.Message, LogType.Error);

        //            if (result != null)
        //            {
        //                result.IsError = true;
        //                result.Message = providerResult.Message;
        //            }
        //        }
        //        else if (result != null)
        //        {
        //            T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
        //            OASISResult<IEnumerable<IHolon>> holonResult = providerResult.Result.LoadHolonsByCustomKey(customKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

        //            if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
        //            {
        //                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
        //                result.IsLoaded = true;
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonResult.Message}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        errorMessage = $"{errorMessage} Reason: {ex}";

        //        if (result != null)
        //        {
        //            result.Result = null;
        //            OASISErrorHandling.HandleError(ref result, errorMessage);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(errorMessage);
        //    }

        //    return result;
        //}

        //private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeByCustomKeyAsync(string customKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        //{
        //    try
        //    {
        //        OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

        //        if (providerResult.IsError)
        //        {
        //            LoggingManager.Log(providerResult.Message, LogType.Error);

        //            if (result != null)
        //            {
        //                result.IsError = true;
        //                result.Message = providerResult.Message;
        //            }
        //        }
        //        else if (result != null)
        //        {
        //            result = await providerResult.Result.LoadHolonsByCustomKeyAsync(customKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
        //            result.IsLoaded = true;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = string.Concat("An error occured attempting to load the holons for parent with customKey ", customKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

        //        if (result != null)
        //        {
        //            result.Result = null;
        //            OASISErrorHandling.HandleError(ref result, errorMessage);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(errorMessage);
        //    }

        //    return result;
        //}

        //private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeByCustomKeyAsync<T>(string customKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        //{
        //    try
        //    {
        //        OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

        //        if (providerResult.IsError)
        //        {
        //            LoggingManager.Log(providerResult.Message, LogType.Error);

        //            if (result != null)
        //            {
        //                result.IsError = true;
        //                result.Message = providerResult.Message;
        //            }
        //        }
        //        else if (result != null)
        //        {
        //            //T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
        //            OASISResult<IEnumerable<IHolon>> loadHolonsForParentResult = await providerResult.Result.LoadHolonsByCustomKeyAsync(customKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

        //            if (!loadHolonsForParentResult.IsError && loadHolonsForParentResult.Result != null)
        //            {
        //                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(loadHolonsForParentResult.Result);
        //                result.IsLoaded = true;
        //            }
        //            else
        //            {
        //                result.IsError = true;
        //                result.Message = loadHolonsForParentResult.Message;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMessage = string.Concat("An error occured attempting to load the holons for parent with customKey ", customKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

        //        if (result != null)
        //        {
        //            result.Result = null;
        //            OASISErrorHandling.HandleError(ref result, errorMessage);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(errorMessage);
        //    }

        //    return result;
        //}

        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderTypeByMetaData(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
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

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderTypeByMetaData<T>(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
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

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeByMetaDataAsync(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
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

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeByMetaDataAsync<T>(string metaKey, string metaValue, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
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
                    OASISResult<IEnumerable<IHolon>> loadHolonsForParentResult = await providerResult.Result.LoadHolonsByMetaDataAsync(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

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
                    OASISErrorHandling.HandleError(errorMessage);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadChildHolonsRecursive<T>(OASISResult<IEnumerable<T>> result, string errorMessage, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            currentChildDepth++;

            if ((recursive && currentChildDepth >= maxChildDepth && maxChildDepth > 0) || (!recursive && currentChildDepth > 1))
                return result;

            foreach (IHolon childHolon in result.Result)
            {
                OASISResult<IEnumerable<T>> holonsResult = LoadHolonsForParent<T>(childHolon.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    childHolon.Children = [.. holonsResult.Result];
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"The child holon with id {childHolon.Id} failed to load. Reason: {holonsResult.Message}", true);

                    if (!continueOnError)
                        break;
                }
            }

            if (result.InnerMessages.Count > 0)
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadChildHolonsRecursiveAsync<T>(OASISResult<IEnumerable<T>> result, string errorMessage, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            currentChildDepth++;

            if ((recursive && currentChildDepth >= maxChildDepth && maxChildDepth > 0) || (!recursive && currentChildDepth > 1))
                return result;

            foreach (IHolon childHolon in result.Result)
            {
                OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsForParentAsync<T>(childHolon.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    childHolon.Children = [.. holonsResult.Result];
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"The child holon with id {childHolon.Id} failed to load. Reason: {holonsResult.Message}", true);

                    if (!continueOnError)
                        break;
                }
            }

            if (result.InnerMessages.Count > 0)
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> LoadChildHolonsRecursive(OASISResult<IEnumerable<IHolon>> result, string errorMessage, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default)
        {
            currentChildDepth++;

            if ((recursive && currentChildDepth >= maxChildDepth && maxChildDepth > 0) || (!recursive && currentChildDepth > 1))
                return result;

            foreach (IHolon childHolon in result.Result)
            {
                OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsForParent(childHolon.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    childHolon.Children = [.. holonsResult.Result];
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"The child holon with id {childHolon.Id} failed to load. Reason: {holonsResult.Message}", true);

                    if (!continueOnError)
                        break;
                }
            }

            if (result.InnerMessages.Count > 0)
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsRecursiveAsync(OASISResult<IEnumerable<IHolon>> result, string errorMessage, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default)
        {
            currentChildDepth++;

            if ((recursive && currentChildDepth >= maxChildDepth && maxChildDepth > 0) || (!recursive && currentChildDepth > 1))
                return result;

            foreach (IHolon childHolon in result.Result)
            {
                OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsForParentAsync(childHolon.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    childHolon.Children = [.. holonsResult.Result];
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"The child holon with id {childHolon.Id} failed to load. Reason: {holonsResult.Message}", true);

                    if (!continueOnError)
                        break;
                }
            }

            if (result.InnerMessages.Count > 0)
                OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadChildHolonsRecursiveForParentHolon<T>(OASISResult<IEnumerable<T>> result, string parentHolonIdMessage, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return LoadChildHolonsRecursive(result, $"The holon with {parentHolonIdMessage} loaded fine but one or more of it's children failed to load.", holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadChildHolonsRecursiveForParentHolonAsync<T>(OASISResult<IEnumerable<T>> result, string parentHolonIdMessage, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return await LoadChildHolonsRecursiveAsync(result, $"The holon with {parentHolonIdMessage} loaded fine but one or more of it's children failed to load.", holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        }

        private OASISResult<IEnumerable<IHolon>> LoadChildHolonsRecursiveForParentHolon(OASISResult<IEnumerable<IHolon>> result, string parentHolonIdMessage, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default)
        {
            return LoadChildHolonsRecursive(result, $"The holon with {parentHolonIdMessage} loaded fine but one or more of it's children failed to load.", holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsRecursiveForParentHolonAsync(OASISResult<IEnumerable<IHolon>> result, string parentHolonIdMessage, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, int currentChildDepth = 0, ProviderType providerType = ProviderType.Default)
        {
            return await LoadChildHolonsRecursiveAsync(result, $"The holon with {parentHolonIdMessage} loaded fine but one or more of it's children failed to load.", holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        }
    }
}