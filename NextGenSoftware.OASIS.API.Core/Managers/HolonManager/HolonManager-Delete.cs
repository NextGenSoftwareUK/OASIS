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
        public OASISResult<IHolon> DeleteHolon(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
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
                    result = providerResult.Result.DeleteHolon(id, softDelete);

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
                                        result = autoReplicateProviderResult.Result.DeleteHolon(id, softDelete);

                                        if (result.IsError)
                                            result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value)));
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

        public async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
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
                    result = await providerResult.Result.DeleteHolonAsync(id, softDelete);

                    if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                            {
                                try
                                {
                                    OASISResult<IOASISStorageProvider> autoReplicateProviderResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                                    if (autoReplicateProviderResult.IsError)
                                    {
                                        result.IsError = true;
                                        result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                    }
                                    else
                                    {
                                        result = await autoReplicateProviderResult.Result.DeleteHolonAsync(id, softDelete);

                                        if (result.IsError)
                                            result.InnerMessages.Add(string.Concat("An error occured in DeleteHolonAsync method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value)));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = string.Concat("An unknown error occured in DeleteHolonAsync method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
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

        public OASISResult<IHolon> DeleteHolon(string providerKey, bool softDelete = true, ProviderType providerType = ProviderType.Default)
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
                    result = providerResult.Result.DeleteHolon(providerKey, softDelete);

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
                                        result = autoReplicateProviderResult.Result.DeleteHolon(providerKey, softDelete);

                                        if (result.IsError)
                                            result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value)));
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

        public async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, bool softDelete = true, ProviderType providerType = ProviderType.Default)
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
                    result = await providerResult.Result.DeleteHolonAsync(providerKey, softDelete);

                    if (!result.IsError && result.Result != null && ProviderManager.Instance.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                            {
                                try
                                {
                                    OASISResult<IOASISStorageProvider> autoReplicateProviderResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);

                                    if (autoReplicateProviderResult.IsError)
                                    {
                                        result.IsError = true;
                                        result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                    }
                                    else
                                    {
                                        result = await autoReplicateProviderResult.Result.DeleteHolonAsync(providerKey, softDelete);

                                        if (result.IsError)
                                            result.InnerMessages.Add(string.Concat("An error occured in DeleteHolonAsync method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value)));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = string.Concat("An unknown error occured in DeleteHolonAsync method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
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
    }
} 