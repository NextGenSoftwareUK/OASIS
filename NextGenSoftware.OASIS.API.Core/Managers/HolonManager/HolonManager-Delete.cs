using System;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        public OASISResult<IHolon> DeleteHolon(Guid id, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    if (softDelete)
                    {
                        OASISResult<IHolon> loadHolonResult = LoadHolon(id, providerType: providerType);

                        if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                        {
                            loadHolonResult.Result.DeletedByAvatarId = avatarId;
                            loadHolonResult.Result.DeletedDate = DateTime.UtcNow;

                            OASISResult<IHolon> saveHolonResult = SaveHolon(loadHolonResult.Result, providerType: providerType);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                result.Result = saveHolonResult.Result;
                                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured deleting holon with id {id} calling SaveHolon. Reason: {saveHolonResult.Message}");
                        }
                    }
                    else
                    {
                        result = providerResult.Result.DeleteHolon(id);

                        if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                            {
                                if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    try
                                    {
                                        OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                                        if (autoReplicateProviderResult.IsError)
                                        {
                                            result.IsError = true;
                                            result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                        }
                                        else
                                        {
                                            result = autoReplicateProviderResult.Result.DeleteHolon(id);

                                            if (result != null && !result.IsError && result.Result != null)
                                                break;
                                            else
                                                result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Reason: ", result.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                        result.IsError = true;
                                        result.InnerMessages.Add(errorMessage);
                                        LoggingManager.Log(errorMessage, LogType.Error);
                                        result.Exception = ex;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolon attempting to auto-replicate the deletion of the holon with id: ", id, ", softDelete = ", softDelete);
            }

            return result;
        }


        public async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    if (softDelete)
                    {
                        OASISResult<IHolon> loadHolonResult = await LoadHolonAsync(id, providerType: providerType);

                        if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                        {
                            loadHolonResult.Result.DeletedByAvatarId = avatarId;
                            loadHolonResult.Result.DeletedDate = DateTime.UtcNow;

                            OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(loadHolonResult.Result, providerType: providerType);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                result.Result = saveHolonResult.Result;
                                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured deleting holon with id {id} calling SaveHolon. Reason: {saveHolonResult.Message}");
                        }
                    }
                    else
                    {
                        result = providerResult.Result.DeleteHolon(id);

                        if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                            {
                                if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    try
                                    {
                                        OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                                        if (autoReplicateProviderResult.IsError)
                                        {
                                            result.IsError = true;
                                            result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                        }
                                        else
                                        {
                                            result = autoReplicateProviderResult.Result.DeleteHolon(id);

                                            if (result != null && !result.IsError && result.Result != null)
                                                break;
                                            else
                                                result.InnerMessages.Add(string.Concat("An error occured in DeleteHolonAsync method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Reason: ", result.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                        result.IsError = true;
                                        result.InnerMessages.Add(errorMessage);
                                        LoggingManager.Log(errorMessage, LogType.Error);
                                        result.Exception = ex;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolonAsync method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolonAsync attempting to auto-replicate the deletion of the holon with id: ", id, ", softDelete = ", softDelete);
            }

            return result;
        }


        public OASISResult<T> DeleteHolon<T>(Guid id, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T: IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    if (softDelete)
                    {
                        OASISResult<T> loadHolonResult = LoadHolon<T>(id, providerType: providerType);

                        if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                        {
                            loadHolonResult.Result.DeletedByAvatarId = avatarId;
                            loadHolonResult.Result.DeletedDate = DateTime.UtcNow;

                            OASISResult<T> saveHolonResult = SaveHolon<T>(loadHolonResult.Result, providerType: providerType);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                result.Result = saveHolonResult.Result;
                                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured deleting holon with id {id} calling SaveHolon. Reason: {saveHolonResult.Message}");
                        }
                    }
                    else
                    {
                        OASISResult<IHolon> deleteHolonResult = providerResult.Result.DeleteHolon(id);
                        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteHolonResult, result);

                        if (deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError)
                            result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(deleteHolonResult.Result);

                        if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                            {
                                if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    try
                                    {
                                        OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                                        if (autoReplicateProviderResult.IsError)
                                        {
                                            result.IsError = true;
                                            result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                        }
                                        else
                                        {
                                            //result = autoReplicateProviderResult.Result.DeleteHolon(id);

                                            deleteHolonResult = autoReplicateProviderResult.Result.DeleteHolon(id);
                                            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteHolonResult, result);

                                            if (deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError)
                                            {
                                                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(deleteHolonResult.Result);
                                                break;
                                            }
                                            else
                                                result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Reason: ", result.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                        result.IsError = true;
                                        result.InnerMessages.Add(errorMessage);
                                        LoggingManager.Log(errorMessage, LogType.Error);
                                        result.Exception = ex;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolon attempting to auto-replicate the deletion of the holon with id: ", id, ", softDelete = ", softDelete);
            }

            return result;
        }


        public async Task<OASISResult<T>> DeleteHolonAsync<T>(Guid id, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    if (softDelete)
                    {
                        OASISResult<T> loadHolonResult = LoadHolon<T>(id, providerType: providerType);

                        if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                        {
                            loadHolonResult.Result.DeletedByAvatarId = avatarId;
                            loadHolonResult.Result.DeletedDate = DateTime.UtcNow;

                            OASISResult<T> saveHolonResult = SaveHolon<T>(loadHolonResult.Result, providerType: providerType);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                result.Result = saveHolonResult.Result;
                                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured deleting holon with id {id} calling SaveHolon. Reason: {saveHolonResult.Message}");
                        }
                    }
                    else
                    {
                        OASISResult<IHolon> deleteHolonResult = providerResult.Result.DeleteHolon(id);
                        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteHolonResult, result);

                        if (deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError)
                            result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(deleteHolonResult.Result);

                        if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                            {
                                if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    try
                                    {
                                        OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                                        if (autoReplicateProviderResult.IsError)
                                        {
                                            result.IsError = true;
                                            result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                        }
                                        else
                                        {
                                            //result = autoReplicateProviderResult.Result.DeleteHolon(id);

                                            deleteHolonResult = autoReplicateProviderResult.Result.DeleteHolon(id);
                                            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteHolonResult, result);

                                            if (deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError)
                                            {
                                                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(deleteHolonResult.Result);
                                                break;
                                            }
                                            else
                                                result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Reason: ", result.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                        result.IsError = true;
                                        result.InnerMessages.Add(errorMessage);
                                        LoggingManager.Log(errorMessage, LogType.Error);
                                        result.Exception = ex;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolon attempting to auto-replicate the deletion of the holon with id: ", id, ", softDelete = ", softDelete);
            }

            return result;
        }

        public OASISResult<IHolon> DeleteHolon(string providerKey, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    if (softDelete)
                    {
                        OASISResult<IHolon> loadHolonResult = LoadHolon(providerKey, providerType: providerType);

                        if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                        {
                            loadHolonResult.Result.DeletedByAvatarId = avatarId;
                            loadHolonResult.Result.DeletedDate = DateTime.UtcNow;

                            OASISResult<IHolon> saveHolonResult = SaveHolon(loadHolonResult.Result, providerType: providerType);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                result.Result = saveHolonResult.Result;
                                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured deleting holon with providerKey {providerKey} calling SaveHolon. Reason: {saveHolonResult.Message}");
                        }
                    }
                    else
                    {
                        result = providerResult.Result.DeleteHolon(providerKey);

                        if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                            {
                                if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    try
                                    {
                                        OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                                        if (autoReplicateProviderResult.IsError)
                                        {
                                            result.IsError = true;
                                            result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                        }
                                        else
                                        {
                                            result = autoReplicateProviderResult.Result.DeleteHolon(providerKey);

                                            if (result != null && !result.IsError && result.Result != null)
                                                break;
                                            else
                                                result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Reason: ", result.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                        result.IsError = true;
                                        result.InnerMessages.Add(errorMessage);
                                        LoggingManager.Log(errorMessage, LogType.Error);
                                        result.Exception = ex;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolon attempting to auto-replicate the deletion of the holon with providerKey: ", providerKey, ", softDelete = ", softDelete);
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    if (softDelete)
                    {
                        OASISResult<IHolon> loadHolonResult = await LoadHolonAsync(providerKey, providerType: providerType);

                        if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                        {
                            loadHolonResult.Result.DeletedByAvatarId = avatarId;
                            loadHolonResult.Result.DeletedDate = DateTime.UtcNow;

                            OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(loadHolonResult.Result, providerType: providerType);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                result.Result = saveHolonResult.Result;
                                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured deleting holon with providerKey {providerKey} calling SaveHolon. Reason: {saveHolonResult.Message}");
                        }
                    }
                    else
                    {
                        result = await providerResult.Result.DeleteHolonAsync(providerKey);

                        if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                            {
                                if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    try
                                    {
                                        OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                                        if (autoReplicateProviderResult.IsError)
                                        {
                                            result.IsError = true;
                                            result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                        }
                                        else
                                        {
                                            result = autoReplicateProviderResult.Result.DeleteHolon(providerKey);

                                            if (result != null && !result.IsError && result.Result != null)
                                                break;
                                            else
                                                result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Reason: ", result.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                        result.IsError = true;
                                        result.InnerMessages.Add(errorMessage);
                                        LoggingManager.Log(errorMessage, LogType.Error);
                                        result.Exception = ex;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolonAsync method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolonAsync attempting to auto-replicate the deletion of the holon with providerKey: ", providerKey, ", softDelete = ", softDelete);
            }

            return result;
        }

        public OASISResult<T> DeleteHolon<T>(string providerKey, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    if (softDelete)
                    {
                        OASISResult<T> loadHolonResult = LoadHolon<T>(providerKey, providerType: providerType);

                        if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                        {
                            loadHolonResult.Result.DeletedByAvatarId = avatarId;
                            loadHolonResult.Result.DeletedDate = DateTime.UtcNow;

                            OASISResult<T> saveHolonResult = SaveHolon<T>(loadHolonResult.Result, providerType: providerType);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                result.Result = saveHolonResult.Result;
                                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured deleting holon with providerKey {providerKey} calling SaveHolon. Reason: {saveHolonResult.Message}");
                        }
                    }
                    else
                    {
                        OASISResult<IHolon> deleteHolonResult = providerResult.Result.DeleteHolon(providerKey);
                        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteHolonResult, result);

                        if (deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError)
                            result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(deleteHolonResult.Result);

                        if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                            {
                                if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    try
                                    {
                                        OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                                        if (autoReplicateProviderResult.IsError)
                                        {
                                            result.IsError = true;
                                            result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                        }
                                        else
                                        {
                                            //result = autoReplicateProviderResult.Result.DeleteHolon(id);

                                            deleteHolonResult = autoReplicateProviderResult.Result.DeleteHolon(providerKey);
                                            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteHolonResult, result);

                                            if (deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError)
                                            {
                                                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(deleteHolonResult.Result);
                                                break;
                                            }
                                            else
                                                result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Reason: ", result.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                        result.IsError = true;
                                        result.InnerMessages.Add(errorMessage);
                                        LoggingManager.Log(errorMessage, LogType.Error);
                                        result.Exception = ex;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolon attempting to auto-replicate the deletion of the holon with providerKey: ", providerKey, ", softDelete = ", softDelete);
            }

            return result;
        }

        public async Task<OASISResult<T>> DeleteHolonAsync<T>(string providerKey, Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    if (softDelete)
                    {
                        OASISResult<T> loadHolonResult = await LoadHolonAsync<T>(providerKey, providerType: providerType);

                        if (loadHolonResult != null && loadHolonResult.Result != null && !loadHolonResult.IsError)
                        {
                            loadHolonResult.Result.DeletedByAvatarId = avatarId;
                            loadHolonResult.Result.DeletedDate = DateTime.UtcNow;

                            OASISResult<T> saveHolonResult = await SaveHolonAsync<T>(loadHolonResult.Result, providerType: providerType);

                            if (saveHolonResult != null && saveHolonResult.Result != null && !saveHolonResult.IsError)
                            {
                                result.Result = saveHolonResult.Result;
                                result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveHolonResult, result);
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"Error occured deleting holon with providerKey {providerKey} calling SaveHolon. Reason: {saveHolonResult.Message}");
                        }
                    }
                    else
                    {
                        OASISResult<IHolon> deleteHolonResult = await providerResult.Result.DeleteHolonAsync(providerKey);
                        result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteHolonResult, result);

                        if (deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError)
                            result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(deleteHolonResult.Result);

                        if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                            {
                                if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    try
                                    {
                                        OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);

                                        if (autoReplicateProviderResult.IsError)
                                        {
                                            result.IsError = true;
                                            result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                        }
                                        else
                                        {
                                            //result = autoReplicateProviderResult.Result.DeleteHolon(id);

                                            deleteHolonResult = await autoReplicateProviderResult.Result.DeleteHolonAsync(providerKey);
                                            result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(deleteHolonResult, result);

                                            if (deleteHolonResult != null && deleteHolonResult.Result != null && !deleteHolonResult.IsError)
                                            {
                                                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(deleteHolonResult.Result);
                                                break;
                                            }
                                            else
                                                result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Reason: ", result.Message));
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                        result.IsError = true;
                                        result.InnerMessages.Add(errorMessage);
                                        LoggingManager.Log(errorMessage, LogType.Error);
                                        result.Exception = ex;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolon attempting to auto-replicate the deletion of the holon with providerKey: ", providerKey, ", softDelete = ", softDelete);
            }

            return result;
        }

    }
} 