using System;
using System.Collections.Generic;
using System.Reflection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public partial class Holon
    {
        public virtual bool HasHolonChanged(bool checkChildren = true)
        {
            if (IsChanged)
                return true;

            if (Original != null)
            {
                if (Original.Id != Id) return true;
                if (Original.Name != Name) return true;
                if (Original.Description != Description) return true;
                if (Original.CreatedByAvatar != CreatedByAvatar) return true;
                if (Original.CreatedByAvatarId != CreatedByAvatarId) return true;
                if (Original.CreatedDate != CreatedDate) return true;
                if (Original.ModifiedByAvatar != ModifiedByAvatar) return true;
                if (Original.ModifiedByAvatarId != ModifiedByAvatarId) return true;
                if (Original.ModifiedDate != ModifiedDate) return true;
                if (Original.CreatedProviderType != CreatedProviderType) return true;
                if (Original.DeletedByAvatar != DeletedByAvatar) return true;
                if (Original.DeletedByAvatarId != DeletedByAvatarId) return true;
                if (Original.DeletedDate != DeletedDate) return true;
                if (Original.HolonType != HolonType) return true;
                if (Original.IsActive != IsActive) return true;
                if (Original.CreatedOASISType != CreatedOASISType) return true;
                if (Original.InstanceSavedOnProviderType != InstanceSavedOnProviderType) return true;
                if (Original.PreviousVersionId != PreviousVersionId) return true;
                if (Original.PreviousVersionProviderUniqueStorageKey != PreviousVersionProviderUniqueStorageKey) return true;
                if (Original.ProviderMetaData != ProviderMetaData) return true;
                if (Original.ProviderUniqueStorageKey != ProviderUniqueStorageKey) return true;
                if (Original.Version != Version) return true;
                if (Original.VersionId != VersionId) return true;
            }

            return Id != Guid.Empty;
        }

        private void MapMetaData<T>()
        {
            if (this.MetaData != null && this.MetaData.Count > 0)
            {
                foreach (string key in this.MetaData.Keys)
                {
                    PropertyInfo propInfo = typeof(T).GetProperty(key);
                    if (propInfo == null) continue;

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
                        propInfo.SetValue(this, Convert.ToDouble(this.MetaData[key]));
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
                List<IHolon> holons = new List<IHolon>(holonsResult.Result);
                if (holons.Count == 1)
                    result.Result = holons[0];
                else
                { result.IsError = true; result.Message = "ERROR, there should only be one GreatGrandSuperStar!"; }
            }
        }

        private void GetGreatGrandSuperStar<T>(ref OASISResult<T> result, OASISResult<IEnumerable<IHolon>> holonsResult)
        {
            if (!holonsResult.IsError && holonsResult.Result != null)
            {
                List<T> holons = new List<T>((IEnumerable<T>)holonsResult.Result);
                if (holons.Count == 1)
                    result.Result = holons[0];
                else
                { result.IsError = true; result.Message = "ERROR, there should only be one GreatGrandSuperStar!"; }
            }
        }
    }
}
