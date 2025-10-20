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

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Holon : SemanticHolon, IHolon, INotifyPropertyChanged
    {
        private const string CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET = "Both Id and ProviderUniqueStorageKey are null, one of these need to be set before calling this method.";

        public Holon(Guid id)
        {
            Id = id;
        }

        public Holon(string providerKey, ProviderType providerType)
        {
            if (providerType == ProviderType.Default)
                providerType = ProviderManager.Instance.CurrentStorageProviderType.Value;

            this.ProviderUniqueStorageKey[providerType] = providerKey;
        }


        public Holon(HolonType holonType)
        {
            IsNewHolon = true;
            HolonType = holonType;
        }

        public Holon()
        {
            IsNewHolon = true;
        }

        public GlobalHolonData GlobalHolonData { get; set; } = new GlobalHolonData();

        public event EventDelegates.Initialized OnInitialized;
        public event EventDelegates.HolonError OnError;

        //Public method events for CRUD methods that apply to THIS holon.
        public event EventDelegates.HolonLoaded OnLoaded;
        public event EventDelegates.HolonSaved OnSaved;
        public event EventDelegates.HolonDeleted OnDeleted;
        public event EventDelegates.HolonAdded OnHolonAdded;
        public event EventDelegates.HolonRemoved OnHolonRemoved;
        public event EventDelegates.HolonsLoaded OnChildrenLoaded;
        public event EventDelegates.HolonsError OnChildrenLoadError;
        public IHolon Original { get; set; }

      

        //TODO: Finish converting all properties so are same as above...
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique key used by each provider (e.g. hashaddress in hc, accountname for Telos, id in MongoDB etc).        
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.
        public string CustomKey { get; set; } //A custom key that can be used to load the holon by (other than Id or ProviderKey).
        public bool IsNewHolon { get; set; } //TODO: Want to remove this ASAP!
        public bool IsSaving { get; set; }

        public Guid PreviousVersionId { get; set; }
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();

        public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        public EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        public EnumValue<OASISType> CreatedOASISType { get; set; }

        public virtual bool HasHolonChanged(bool checkChildren = true)
        {
            if (IsChanged)
                return true;

            if (Original != null)
            {
                if (Original.Id != Id)
                    return true;

                if (Original.Name != Name)
                    return true;

                if (Original.Description != Description)
                    return true;

                if (Original.CreatedByAvatar != CreatedByAvatar)
                    return true;

                if (Original.CreatedByAvatarId != CreatedByAvatarId)
                    return true;

                if (Original.CreatedDate != CreatedDate)
                    return true;

                if (Original.ModifiedByAvatar != ModifiedByAvatar)
                    return true;

                if (Original.ModifiedByAvatarId != ModifiedByAvatarId)
                    return true;

                if (Original.ModifiedDate != ModifiedDate)
                    return true;

                if (Original.CreatedProviderType != CreatedProviderType)
                    return true;

                if (Original.DeletedByAvatar != DeletedByAvatar)
                    return true;

                if (Original.DeletedByAvatarId != DeletedByAvatarId)
                    return true;

                if (Original.DeletedDate != DeletedDate)
                    return true;

                if (Original.HolonType != HolonType)
                    return true;

                if (Original.IsActive != IsActive)
                    return true;

                if (Original.CreatedOASISType != CreatedOASISType)
                    return true;

                //if (Original.CustomKey != CustomKey)
                //    return true;

                if (Original.InstanceSavedOnProviderType != InstanceSavedOnProviderType)
                    return true;

                if (Original.InstanceSavedOnProviderType != InstanceSavedOnProviderType)
                    return true;

                if (Original.PreviousVersionId != PreviousVersionId)
                    return true;

                if (Original.PreviousVersionProviderUniqueStorageKey != PreviousVersionProviderUniqueStorageKey)
                    return true;

                if (Original.ProviderMetaData != ProviderMetaData)
                    return true;

                if (Original.ProviderUniqueStorageKey != ProviderUniqueStorageKey)
                    return true;

                if (Original.Version != Version)
                    return true;

                if (Original.VersionId != VersionId)
                    return true;
            }

            return Id != Guid.Empty;
        }

        public async Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                if (this.HolonType == HolonType.GreatGrandSuperStar)
                    GetGreatGrandSuperStar(ref result, await HolonManager.Instance.LoadAllHolonsAsync(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType));

                else if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonAsync(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = await HolonManager.Instance.LoadHolonAsync(providerKeyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadAsync. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadAsync Calling HolonManager.LoadHolonAsync. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadAsync Calling HolonManager.LoadHolonAsync. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                if (this.HolonType == HolonType.GreatGrandSuperStar)
                    GetGreatGrandSuperStar(ref result, await HolonManager.Instance.LoadAllHolonsAsync(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType));

                else if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonAsync<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = await HolonManager.Instance.LoadHolonAsync<T>(providerKeyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadAsync<T>. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    //MapMetaData<T>(); //TODO: Don't think we need to do this? Because HolonManager does this for us...
                    OnLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadAsync<T> Calling HolonManager.LoadHolonAsync<T>. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadAsync<T> Calling HolonManager.LoadHolonAsync<T>. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                if (this.HolonType == HolonType.GreatGrandSuperStar)
                    GetGreatGrandSuperStar(ref result, HolonManager.Instance.LoadAllHolons(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType));

                else if (this.Id != Guid.Empty)
                    result = HolonManager.Instance.LoadHolon(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = HolonManager.Instance.LoadHolon(providerKeyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.Load. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.Load Calling HolonManager.LoadHolon. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Load Calling HolonManager.LoadHolon. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                if (this.HolonType == HolonType.GreatGrandSuperStar)
                    GetGreatGrandSuperStar(ref result, HolonManager.Instance.LoadAllHolons(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType));

                else if (this.Id != Guid.Empty)
                    result = HolonManager.Instance.LoadHolon<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = HolonManager.Instance.LoadHolon<T>(providerKeyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.Load<T>. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    //MapMetaData<T>(); //TODO: Don't think we need to do this? Because HolonManager does this for us...
                    OnLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.Load<T> Calling HolonManager.LoadHolon<T>. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Load<T> Calling HolonManager.LoadHolon<T>. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadChildHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonsForParentAsync(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = await HolonManager.Instance.LoadHolonsForParentAsync(providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadChildHolonsAsync. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    //this.Children = new ObservableCollection<IHolon>(result.Result);
                    this.Children = result.Result.ToList();
                    OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadChildHolonsAsync Calling HolonManager.LoadHolonsForParentAsync. Reason: {result.Message}");
                    OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadChildHolonsAsync Calling HolonManager.LoadHolonsForParentAsync. Reason: {ex}", ex);
                OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadChildHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            try
            {
                if (this.Id != Guid.Empty)
                    result = HolonManager.Instance.LoadHolonsForParent(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = HolonManager.Instance.LoadHolonsForParent(providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadChildHolons. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    //this.Children = new ObservableCollection<IHolon>(result.Result);
                    this.Children = result.Result.ToList();
                    OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadChildHolons Calling HolonManager.LoadHolonsForParent. Reason: {result.Message}");
                    OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadChildHolons Calling HolonManager.LoadHolonsForParent. Reason: {ex}", ex);
                OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadChildHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            try
            {
                if (this.Id != Guid.Empty)
                    result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadChildHolonsAsync<T>. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    //this.Children = new ObservableCollection<IHolon>(Mapper.Convert(result.Result));
                    this.Children = Mapper.Convert(result.Result).ToList();
                    OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadChildHolonsAsync<T> Calling HolonManager.LoadHolonsForParentAsync<T>. Reason: {result.Message}");
                    OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadChildHolonsAsync<T> Calling HolonManager.LoadHolonsForParentAsync<T>. Reason: {ex}", ex);
                OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IEnumerable<T>> LoadChildHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            try
            {
                if (this.Id != Guid.Empty)
                    result = HolonManager.Instance.LoadHolonsForParent<T>(this.Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);

                else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                {
                    OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                    if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                        result = HolonManager.Instance.LoadHolonsForParent<T>(providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, HolonType.All, version, providerType);
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in HolonBase.LoadChildHolons<T>. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
                }
                else
                {
                    result.IsError = true;
                    result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
                }

                if (result != null && !result.IsError && result.Result != null)
                {
                    //this.Children = new ObservableCollection<IHolon>(Mapper.Convert(result.Result));
                    this.Children = Mapper.Convert(result.Result).ToList();
                    OnChildrenLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.LoadChildHolons<T> Calling HolonManager.LoadHolonsForParent<T>. Reason: {result.Message}");
                    OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Reason = result.Message, Exception = result.Exception });
                }

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadChildHolons<T> Calling HolonManager.LoadHolonsForParent<T>. Reason: {ex}", ex);
                OnChildrenLoadError?.Invoke(this, new HolonsErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = await HolonManager.Instance.SaveHolonAsync((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.AvatarId : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.SaveAsync Calling HolonManager.SaveHolonAsync. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.SaveAsync Calling HolonManager.SaveHolonAsync. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result = await HolonManager.Instance.SaveHolonAsync<T>((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.AvatarId : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.SaveAsync<T> Calling HolonManager.SaveHolonAsync<T>. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.SaveAsync<T> Calling HolonManager.SaveHolonAsync<T>. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IHolon> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = HolonManager.Instance.SaveHolon((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.AvatarId : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.Save Calling HolonManager.SaveHolon. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Save Calling HolonManager.SaveHolon. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result = HolonManager.Instance.SaveHolon<T>((IHolon)this, AvatarManager.LoggedInAvatar != null ? AvatarManager.LoggedInAvatar.AvatarId : Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    OnSaved?.Invoke(this, new HolonSavedEventArgs() { Result = OASISResultHelper.CopyResult(result) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Error Occured in HolonBase.Save<T> Calling HolonManager.SaveHolon<T>. Reason: {result.Message}");
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Save<T> Calling HolonManager.SaveHolon<T>. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = OASISResultHelper.CopyResult(result), Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> DeleteAsync(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = await HolonManager.Instance.DeleteHolonAsync(this.Id, avatarId, softDelete, providerType);

                if (result != null && !result.IsError)
                    OnDeleted?.Invoke(this, new HolonDeletedEventArgs() { Result = result });
                else
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.DeleteAsync method calling DeleteHolonAsync attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging((IHolon)this), ". Error Details: ", result.Message));
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.DeleteAsync Calling HolonManager.DeleteHolonAsync. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<IHolon> Delete(Guid avatarId, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = HolonManager.Instance.DeleteHolon(this.Id, avatarId, softDelete, providerType);

                if (result != null && !result.IsError)
                    OnDeleted?.Invoke(this, new HolonDeletedEventArgs() { Result = result });
                else
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.Delete method calling DeleteHolon attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging((IHolon)this), ". Error Details: ", result.Message));
                    OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Delete Calling HolonManager.DeleteHolon. Reason: {ex}", ex);
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, Guid avatarId, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>(holon);

            try
            {
                holon.ParentHolonId = this.Id;
                //this.Children.Add(holon);
                ((List<IHolon>)this.Children).Add(holon);

                if (saveHolon)
                {
                    result = await HolonManager.Instance.SaveHolonAsync(holon, avatarId, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                    if (result.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error in HolonBase.AddHolonAsync method calling SaveHolonAsync attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message));
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
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured in HolonBase.AddHolonAsync method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", ex));
                OnError?.Invoke(this, new HolonErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

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
