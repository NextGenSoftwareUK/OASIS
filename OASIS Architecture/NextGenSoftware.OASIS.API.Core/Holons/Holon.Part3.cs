using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public partial class Holon
    {

        public OASISResult<IHolon> AddHolon(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);

            try
            {
                holon.ParentHolonId = this.Id;
                //this.Children.Add(holon);
                ((List<IHolon>)this.Children).Add(holon);

                if (saveHolon)
                {
                    result = HolonManager.Instance.SaveHolon(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolon method calling SaveHolon attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message));
                        OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                    }
                    else
                        OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
                }
                else
                    OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in HolonBase.AddHolon method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", ex));
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<T>> AddHolonAsync<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>(holon);

            try
            {
                holon.ParentHolonId = this.Id;
                ((List<IHolon>)this.Children).Add(holon);
                //this.Children.Add(holon);

                if (saveHolon)
                {
                    result = await HolonManager.Instance.SaveHolonAsync<T>(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolonAsync<T> method calling SaveHolonAsync<T> attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message));
                        OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception });
                    }
                    else
                        OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                    OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in HolonBase.AddHolonAsync<T> method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", ex));
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<T> AddHolon<T>(T holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>(holon);

            try
            {
                holon.ParentHolonId = this.Id;
                ((List<IHolon>)this.Children).Add(holon);
                //this.Children.Add(holon);

                if (saveHolon)
                {
                    result = HolonManager.Instance.SaveHolon<T>(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolon<T> method calling SaveHolon<T> attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message));
                        OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception });
                    }
                    else
                        OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                    OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in HolonBase.AddHolon<T> method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", ex));
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, Guid avatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);

            try
            {
                holon.ParentHolonId = Guid.Empty;
                ((List<IHolon>)this.Children).Remove(holon);
                //this.Children.Add(holon);

                if (deleteHolon)
                {
                    result = await HolonManager.Instance.DeleteHolonAsync(holon.Id, avatarId, softDelete, providerType);

                    if (result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.RemoveHolonAsync method calling DeleteHolonAsync attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message));
                        OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                    }
                    else
                        OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
                }
                else
                    OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in HolonBase.RemoveHolonAsync method attempting to remove the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", ex));
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IHolon> RemoveHolon(IHolon holon, Guid avatarId, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);

            try
            {
                holon.ParentHolonId = Guid.Empty;
                ((List<IHolon>)this.Children).Remove(holon);
                //this.Children.Add(holon);

                if (deleteHolon)
                {
                    result = HolonManager.Instance.DeleteHolon(holon.Id, avatarId, softDelete, providerType);

                    if (result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.RemoveHolon method calling DeleteHolon attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message));
                        OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                    }
                    else
                        OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
                }
                else
                    OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in HolonBase.RemoveHolon method attempting to remove the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", ex));
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        private void SetProperties(IHolon holon)
        {
            this.Name = holon.Name;
            this.Description = holon.Description;
            this.CreatedByAvatar = holon.CreatedByAvatar;
            this.CreatedByAvatarId = holon.CreatedByAvatarId;
            this.CreatedDate = holon.CreatedDate;
            this.CreatedOASISType = holon.CreatedOASISType;
            this.CreatedProviderType = holon.CreatedProviderType;
            //this.CustomKey = holon.CustomKey;
            this.DeletedByAvatar = holon.DeletedByAvatar;
            this.DeletedByAvatarId = holon.DeletedByAvatarId;
            this.DeletedDate = holon.DeletedDate;
            this.HolonType = holon.HolonType;
            this.Id = holon.Id;
            this.InstanceSavedOnProviderType = holon.InstanceSavedOnProviderType;
            this.IsActive = holon.IsActive;
            this.IsChanged = holon.IsChanged; //these appear to be the only properties that need updating, the rest are done automatically due to the objects being byref! ;-)
            this.IsNewHolon = holon.IsNewHolon; //these appear to be the only properties that need updating, the rest are done automatically due to the objects being byref! ;-)
            this.IsSaving = holon.IsSaving;
            this.MetaData = holon.MetaData;
            this.ModifiedByAvatar = holon.ModifiedByAvatar;
            this.ModifiedByAvatarId = holon.ModifiedByAvatarId;
            this.ModifiedDate = holon.ModifiedDate;
            this.Original = holon.Original;
            this.PreviousVersionId = holon.PreviousVersionId;
            this.PreviousVersionProviderUniqueStorageKey = holon.PreviousVersionProviderUniqueStorageKey;
            this.ProviderMetaData = holon.ProviderMetaData;
            this.ProviderUniqueStorageKey = holon?.ProviderUniqueStorageKey;
            this.Version = holon.Version;
            this.VersionId = holon.VersionId;
        }

        private void MapMetaData<T>()
        {
            if (this.MetaData != null && this.MetaData.Count > 0)
            {
                foreach (string key in this.MetaData.Keys)
                {
                    PropertyInfo propInfo = typeof(T).GetProperty(key);

                    if (propInfo != null)
                    {
                        if (propInfo.PropertyType == typeof(Guid))
                            propInfo.SetValue(this, new Guid(this.MetaData[key].ToString()));

                        else if (propInfo.PropertyType == typeof(bool))
                            propInfo.SetValue(this, Convert.ToBoolean(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(DateTime))
                            propInfo.SetValue(this, Convert.ToDateTime(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(int))
                            propInfo.SetValue(this, Convert.ToInt32(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(long))
                            propInfo.SetValue(this, Convert.ToInt64(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(float))
                            propInfo.SetValue(this, Convert.ToDouble(this.MetaData[key])); //TODO: Check if this is right?! :)

                        else if (propInfo.PropertyType == typeof(double))
                            propInfo.SetValue(this, Convert.ToDouble(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(decimal))
                            propInfo.SetValue(this, Convert.ToDecimal(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(UInt16))
                            propInfo.SetValue(this, Convert.ToUInt16(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(UInt32))
                            propInfo.SetValue(this, Convert.ToUInt32(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(UInt64))
                            propInfo.SetValue(this, Convert.ToUInt64(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(Single))
                            propInfo.SetValue(this, Convert.ToSingle(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(char))
                            propInfo.SetValue(this, Convert.ToChar(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(byte))
                            propInfo.SetValue(this, Convert.ToByte(this.MetaData[key]));

                        else if (propInfo.PropertyType == typeof(sbyte))
                            propInfo.SetValue(this, Convert.ToSByte(this.MetaData[key]));

                        else
                            propInfo.SetValue(this, this.MetaData[key]);
                    }

                    //TODO: Add any other missing types...
                }

                //this(IHolonBase) = HolonManager.Instance.MapMetaData<T>((IHolon)this);
            }
        }

        private OASISResult<string> GetCurrentProviderKey(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            if (providerType == ProviderType.Default || providerType == ProviderType.All || providerType == ProviderType.None)
                providerType = ProviderManager.Instance.CurrentStorageProviderType.Value;

            if (ProviderUniqueStorageKey.ContainsKey(providerType) && !string.IsNullOrEmpty(ProviderUniqueStorageKey[providerType]))
                result.Result = ProviderUniqueStorageKey[providerType];
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("ProviderUniqueStorageKey not found for CurrentStorageProviderType ", Enum.GetName(typeof(ProviderType), providerType)));

            return result;
        }

        private void GetGreatGrandSuperStar(ref OASISResult<IHolon> result, OASISResult<IEnumerable<IHolon>> holonsResult)
        {
            if (!holonsResult.IsError && holonsResult.Result != null)
            {
                List<IHolon> holons = (List<IHolon>)holonsResult.Result;

                if (holons.Count == 1)
                    result.Result = holons[0];
                else
                {
                    result.IsError = true;
                    result.Message = "ERROR, there should only be one GreatGrandSuperStar!";
                }
            }
        }

        private void GetGreatGrandSuperStar<T>(ref OASISResult<T> result, OASISResult<IEnumerable<IHolon>> holonsResult)
        {
            if (!holonsResult.IsError && holonsResult.Result != null)
            {
                List<T> holons = (List<T>)holonsResult.Result;

                if (holons.Count == 1)
                    result.Result = holons[0];
                else
                {
                    result.IsError = true;
                    result.Message = "ERROR, there should only be one GreatGrandSuperStar!";
                }
            }
        }
    }
}
