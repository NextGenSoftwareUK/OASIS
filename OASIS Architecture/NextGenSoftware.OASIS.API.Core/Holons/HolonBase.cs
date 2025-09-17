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
    public abstract class HolonBase : IHolonBase, INotifyPropertyChanged
    {
        private const string CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET = "Both Id and ProviderUniqueStorageKey are null, one of these need to be set before calling this method.";
        private string _name;
        private string _description;
        private IAvatar _createdByAvatar = null;
        private IAvatar _modifiedByAvatar = null;
        private IAvatar _deletedByAvatar = null;

        public HolonBase(Guid id)
        {
            Id = id;
        }

        public HolonBase(string providerKey, ProviderType providerType)
        {
            if (providerType == ProviderType.Default)
                providerType = ProviderManager.Instance.CurrentStorageProviderType.Value;

            this.ProviderUniqueStorageKey[providerType] = providerKey;
        }


        public HolonBase(HolonType holonType)
        {
            IsNewHolon = true;
            HolonType = holonType;
        }

        public HolonBase()
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

        public Guid Id { get; set; } //Unique id within the OASIS.
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                if (value != _name)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("Name");
                }

                _name = value;
            }
        }
        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                if (value != _description)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("Description");
                }

                _description = value;
            }
        }

        //TODO: Finish converting all properties so are same as above...
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique key used by each provider (e.g. hashaddress in hc, accountname for Telos, id in MongoDB etc).        
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.
        public Dictionary<string, object> MetaData { get; set; } = new Dictionary<string, object>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.
        public string CustomKey { get; set; } //A custom key that can be used to load the holon by (other than Id or ProviderKey).
        public bool IsNewHolon { get; set; } //TODO: Want to remove this ASAP!
        public bool IsChanged { get; set; }
        public bool IsSaving { get; set; }
        public HolonType HolonType { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        
        public IAvatar CreatedByAvatar 
        { 
            get
            {
                if (_createdByAvatar == null && CreatedByAvatarId != Guid.Empty)
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(CreatedByAvatarId);

                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                        _createdByAvatar = avatarResult.Result;
                }

                return _createdByAvatar;
            }
            set
            {
                if (value != _createdByAvatar)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("CreatedByAvatar");
                }

                _createdByAvatar = value;
            }
        }
        
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByAvatarId { get; set; }

        public IAvatar ModifiedByAvatar
        {
            get
            {
                if (_modifiedByAvatar == null && ModifiedByAvatarId != Guid.Empty)
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(ModifiedByAvatarId);

                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                        _modifiedByAvatar = avatarResult.Result;
                }

                return _modifiedByAvatar;
            }
            set
            {
                if (value != _modifiedByAvatar)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("ModifiedByAvatar");
                }

                _modifiedByAvatar = value;
            }
        }

        public DateTime ModifiedDate { get; set; }
        public Guid DeletedByAvatarId { get; set; }

        public IAvatar DeletedByAvatar
        {
            get
            {
                if (_deletedByAvatar == null && DeletedByAvatarId != Guid.Empty)
                {
                    OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(DeletedByAvatarId);

                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                        _deletedByAvatar = avatarResult.Result;
                }

                return _deletedByAvatar;
            }
            set
            {
                if (value != _deletedByAvatar)
                {
                    IsChanged = true;
                    NotifyPropertyChanged("DeletedByAvatar");
                }

                _deletedByAvatar = value;
            }
        }

        public DateTime DeletedDate { get; set; }
        public int Version { get; set; }
        public Guid VersionId { get; set; }
        public Guid PreviousVersionId { get; set; }
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();
        public bool IsActive { get; set; }
        public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        public EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        public EnumValue<OASISType> CreatedOASISType { get; set; }
        public Guid ParentHolonId { get; set; }
        public IHolon ParentHolon { get; set; }
        public string ChildIdListCache { get; set; } //This will store the list of id's for the direct childen of this holon.
        public string AllChildIdListCache { get; set; } //This will store the list of id's for the ALL the childen of this holon (including all sub-childen).

        public IList<IHolon> Children { get; set; } = new List<IHolon>();

        public virtual IReadOnlyCollection<IHolon> AllChildren 
        { 
            get
            {
                return Children.AsReadOnly();
            }
        } 

        //public ObservableCollection<IHolon> Children { get; set; }

            /// <summary>
            /// Fired when a property in this class changes.
            /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /*
       public Holon()
       {
           //TODO: Need to check if these are fired when an item in the collection is changed (not just added/removed).
           if (ChildrenTest != null)
               ChildrenTest.CollectionChanged += Children_CollectionChanged;

           if (Nodes != null)
               Nodes.CollectionChanged += Nodes_CollectionChanged;
       }

       private void Nodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
       {
           IsChanged = true;

           //TOOD: Not sure if we need this? Because ObservableCollection is supposed to raise PropertyChanged events itself.
           NotifyPropertyChanged("Nodes");
       }

       private void Children_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
       {
           IsChanged = true;

           //TOOD: Not sure if we need this? Because ObservableCollection is supposed to raise PropertyChanged events itself.
           NotifyPropertyChanged("Children");
       }
          */


        /// <summary>
        /// Triggers the property changed event for a specific property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        public void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        //public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IHolon> result = await HolonManager.Instance.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //public virtual OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IHolon> result = HolonManager.Instance.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //public virtual async Task<OASISResult<T>> LoadHolonAsync<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    OASISResult<T> result = await HolonManager.Instance.LoadHolonAsync<T>(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in ZomeBase.LoadHolonAsync method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //public virtual OASISResult<T> LoadHolon<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    OASISResult<T> result = HolonManager.Instance.LoadHolon<T>(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in ZomeBase.LoadHolon method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, bool loadChildrenFromProvider = false, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        //{
        //    OASISResult<IHolon> result = await HolonManager.Instance.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //public virtual OASISResult<IHolon> LoadHolon(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        //{
        //    OASISResult<IHolon> result = HolonManager.Instance.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //public virtual async Task<OASISResult<T>> LoadHolonAsync<T>(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        //{
        //    OASISResult<T> result = await HolonManager.Instance.LoadHolonAsync<T>(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //public virtual OASISResult<T> LoadHolon<T>(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        //{
        //    OASISResult<T> result = HolonManager.Instance.LoadHolon<T>(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //protected virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = await HolonManager.Instance.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //protected virtual OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = HolonManager.Instance.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadAllHolons method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //protected virtual async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    OASISResult<IEnumerable<T>> result = await HolonManager.Instance.LoadAllHolonsAsync<T>(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) }); //OASISResultHelper<IEnumerable<T>, IEnumerable<IHolon>>.CopyResult(result) });

        //    return result;
        //}

        //protected virtual OASISResult<IEnumerable<T>> LoadAllHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    OASISResult<IEnumerable<T>> result = HolonManager.Instance.LoadAllHolons<T>(holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //protected virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = await HolonManager.Instance.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParentAsync method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //protected virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = HolonManager.Instance.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParent method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //protected virtual async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    OASISResult<IEnumerable<T>> result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParentAsync method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //protected virtual OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    OASISResult<IEnumerable<T>> result = HolonManager.Instance.LoadHolonsForParent<T>(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParent method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //protected virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = await HolonManager.Instance.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParentAsync method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //protected virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    OASISResult<IEnumerable<IHolon>> result = HolonManager.Instance.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParent method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });

        //    return result;
        //}

        //protected virtual async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        //{
        //    OASISResult<IEnumerable<T>> result = await HolonManager.Instance.LoadHolonsForParentAsync<T>(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in HolonBase.LoadHolonsForParent method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //protected virtual OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) where T : IHolon, new()
        //{
        //    OASISResult<IEnumerable<T>> result = HolonManager.Instance.LoadHolonsForParent<T>(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

        //    if (result.IsError)
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParent method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //    else
        //        OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });

        //    return result;
        //}

        //protected virtual async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    savingHolon = RemoveCelesialBodies(savingHolon);
        //    OASISResult<IHolon> result = await HolonManager.Instance.SaveHolonAsync(savingHolon, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //    HandleSaveHolonResult(savingHolon, "SaveHolonAsync", ref result);
        //    return result;
        //}

        //protected virtual OASISResult<IHolon> SaveHolon(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    savingHolon = RemoveCelesialBodies(savingHolon);
        //    OASISResult<IHolon> result = HolonManager.Instance.SaveHolon(savingHolon, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //    HandleSaveHolonResult(savingHolon, "SaveHolon", ref result);
        //    return result;
        //}

        //protected virtual async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    savingHolon = RemoveCelesialBodies(savingHolon);
        //    OASISResult<T> result = await HolonManager.Instance.SaveHolonAsync<T>(savingHolon, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //    HandleSaveHolonResult(savingHolon, "SaveHolonAsync<T>", ref result);
        //    return result;
        //}

        //protected virtual OASISResult<T> SaveHolon<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    savingHolon = RemoveCelesialBodies(savingHolon);
        //    OASISResult<T> result = HolonManager.Instance.SaveHolon<T>(savingHolon, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //    HandleSaveHolonResult(savingHolon, "SaveHolon<T>", ref result);
        //    return result;
        //}

        //protected virtual async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    if (savingHolons == null)
        //        return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

        //    if (savingHolons.Count() == 0)
        //        return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

        //    savingHolons = RemoveCelesialBodies(savingHolons);
        //    OASISResult<IEnumerable<IHolon>> result = await HolonManager.Instance.SaveHolonsAsync(savingHolons, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //    HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result);
        //    return result;
        //}

        //protected virtual OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    if (savingHolons == null)
        //        return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

        //    if (savingHolons.Count() == 0)
        //        return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

        //    savingHolons = RemoveCelesialBodies(savingHolons);
        //    OASISResult<IEnumerable<IHolon>> result = HolonManager.Instance.SaveHolons(savingHolons, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //    HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result);
        //    return result;
        //}

        //protected virtual async Task<OASISResult<IEnumerable<T>>> SaveHolonsAsync<T>(IEnumerable<T> savingHolons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

        //    if (savingHolons == null)
        //        return new OASISResult<IEnumerable<T>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

        //    if (savingHolons.Count() == 0)
        //        return new OASISResult<IEnumerable<T>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

        //    savingHolons = RemoveCelesialBodies(savingHolons);
        //    OASISResult<IEnumerable<T>> saveHolonResult = await HolonManager.Instance.SaveHolonsAsync<T>(savingHolons, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);
        //    OASISResult<IEnumerable<IHolon>> holonsResult = OASISResultHelper.CopyResult<T, IHolon>(saveHolonResult);
        //    HandleSaveHolonsResult(OASISResultHelper.CopyResult<T, IHolon>(result).Result, "SaveHolonsAsync<T>", ref holonsResult);
        //    return result;
        //}

        //protected virtual OASISResult<IEnumerable<T>> SaveHolons<T>(IEnumerable<T> savingHolons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

        //    if (savingHolons == null)
        //        return new OASISResult<IEnumerable<T>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

        //    if (savingHolons.Count() == 0)
        //        return new OASISResult<IEnumerable<T>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

        //    savingHolons = RemoveCelesialBodies(savingHolons);
        //    OASISResult<IEnumerable<T>> saveHolonResult = HolonManager.Instance.SaveHolons(savingHolons, AvatarManager.LoggedInAvatar.Id, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

        //    OASISResult<IEnumerable<IHolon>> holonsResult = OASISResultHelper.CopyResult<T, IHolon>(saveHolonResult);
        //    HandleSaveHolonsResult(OASISResultHelper.CopyResult<T, IHolon>(result).Result, "SaveHolonsAsync<T>", ref holonsResult);
        //    return result;
        //}

        //protected virtual async Task<OASISResult<IHolon>> AddHolonToCollectionAsync(IHolon parentCelestialBody, IHolon holon, List<IHolon> holons, bool saveHolon = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    if (holons == null)
        //        holons = new List<IHolon>();

        //    else if (holons.Any(x => x.Name == holon.Name))
        //    {
        //        result.IsError = true;
        //        result.Message = string.Concat("The name ", holon.Name, " is already taken, please choose another.");
        //        return result;
        //    }

        //    holon.IsNewHolon = true; //TODO: I am pretty sure every holon being added to a collection using this method will be a new one?

        //    if (holon.ParentOmniverseId == Guid.Empty)
        //    {
        //        holon.ParentOmniverseId = parentCelestialBody.ParentOmniverseId;
        //        holon.ParentOmniverse = parentCelestialBody.ParentOmniverse;
        //    }

        //    if (holon.ParentMultiverseId == Guid.Empty)
        //    {
        //        holon.ParentMultiverseId = parentCelestialBody.ParentMultiverseId;
        //        holon.ParentMultiverse = parentCelestialBody.ParentMultiverse;
        //    }

        //    if (holon.ParentUniverseId == Guid.Empty)
        //    {
        //        holon.ParentUniverseId = parentCelestialBody.ParentUniverseId;
        //        holon.ParentUniverse = parentCelestialBody.ParentUniverse;
        //    }

        //    if (holon.ParentDimensionId == Guid.Empty)
        //    {
        //        holon.ParentDimensionId = parentCelestialBody.ParentDimensionId;
        //        holon.ParentDimension = parentCelestialBody.ParentDimension;
        //    }

        //    if (holon.ParentGalaxyClusterId == Guid.Empty)
        //    {
        //        holon.ParentGalaxyClusterId = parentCelestialBody.ParentGalaxyClusterId;
        //        holon.ParentGalaxyCluster = parentCelestialBody.ParentGalaxyCluster;
        //    }

        //    if (holon.ParentGalaxyId == Guid.Empty)
        //    {
        //        holon.ParentGalaxyId = parentCelestialBody.ParentGalaxyId;
        //        holon.ParentGalaxy = parentCelestialBody.ParentGalaxy;
        //    }

        //    if (holon.ParentSolarSystemId == Guid.Empty)
        //    {
        //        holon.ParentSolarSystemId = parentCelestialBody.ParentSolarSystemId;
        //        holon.ParentSolarSystem = parentCelestialBody.ParentSolarSystem;
        //    }

        //    if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentGreatGrandSuperStarId = parentCelestialBody.ParentGreatGrandSuperStarId;
        //        holon.ParentGreatGrandSuperStar = parentCelestialBody.ParentGreatGrandSuperStar;
        //    }

        //    if (holon.ParentGrandSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentGrandSuperStarId = parentCelestialBody.ParentGrandSuperStarId;
        //        holon.ParentGrandSuperStar = parentCelestialBody.ParentGrandSuperStar;
        //    }

        //    if (holon.ParentSuperStarId == Guid.Empty)
        //    {
        //        holon.ParentSuperStarId = parentCelestialBody.ParentSuperStarId;
        //        holon.ParentSuperStar = parentCelestialBody.ParentSuperStar;
        //    }

        //    if (holon.ParentStarId == Guid.Empty)
        //    {
        //        holon.ParentStarId = parentCelestialBody.ParentStarId;
        //        holon.ParentStar = parentCelestialBody.ParentStar;
        //    }

        //    if (holon.ParentPlanetId == Guid.Empty)
        //    {
        //        holon.ParentPlanetId = parentCelestialBody.ParentPlanetId;
        //        holon.ParentPlanet = parentCelestialBody.ParentPlanet;
        //    }

        //    if (holon.ParentMoonId == Guid.Empty)
        //    {
        //        holon.ParentMoonId = parentCelestialBody.ParentMoonId;
        //        holon.ParentMoon = parentCelestialBody.ParentMoon;
        //    }

        //    if (holon.ParentCelestialSpaceId == Guid.Empty)
        //    {
        //        holon.ParentCelestialSpaceId = parentCelestialBody.ParentCelestialSpaceId;
        //        holon.ParentCelestialSpace = parentCelestialBody.ParentCelestialSpace;
        //    }

        //    if (holon.ParentCelestialBodyId == Guid.Empty)
        //    {
        //        holon.ParentCelestialBodyId = parentCelestialBody.ParentCelestialBodyId;
        //        holon.ParentCelestialBody = parentCelestialBody.ParentCelestialBody;
        //    }

        //    if (holon.ParentZomeId == Guid.Empty)
        //    {
        //        holon.ParentZomeId = parentCelestialBody.ParentZomeId;
        //        holon.ParentZome = parentCelestialBody.ParentZome;
        //    }

        //    if (holon.ParentHolonId == Guid.Empty)
        //    {
        //        holon.ParentHolonId = parentCelestialBody.ParentHolonId;
        //        holon.ParentHolon = parentCelestialBody.ParentHolon;
        //    }

        //    switch (parentCelestialBody.HolonType)
        //    {
        //        case HolonType.GreatGrandSuperStar:
        //            holon.ParentGreatGrandSuperStarId = parentCelestialBody.Id;
        //            holon.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.GrandSuperStar:
        //            holon.ParentGrandSuperStarId = parentCelestialBody.Id;
        //            holon.ParentGrandSuperStar = (IGrandSuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.SuperStar:
        //            holon.ParentSuperStarId = parentCelestialBody.Id;
        //            holon.ParentSuperStar = (ISuperStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Multiverse:
        //            holon.ParentMultiverseId = parentCelestialBody.Id;
        //            holon.ParentMultiverse = (IMultiverse)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Universe:
        //            holon.ParentUniverseId = parentCelestialBody.Id;
        //            holon.ParentUniverse = (IUniverse)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Dimension:
        //            holon.ParentDimensionId = parentCelestialBody.Id;
        //            holon.ParentDimension = (IDimension)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.GalaxyCluster:
        //            holon.ParentGalaxyClusterId = parentCelestialBody.Id;
        //            holon.ParentGalaxyCluster = (IGalaxyCluster)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Galaxy:
        //            holon.ParentGalaxyId = parentCelestialBody.Id;
        //            holon.ParentGalaxy = (IGalaxy)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.SolarSystem:
        //            holon.ParentSolarSystemId = parentCelestialBody.Id;
        //            holon.ParentSolarSystem = (ISolarSystem)parentCelestialBody;
        //            holon.ParentCelestialSpaceId = parentCelestialBody.Id;
        //            holon.ParentCelestialSpace = (ICelestialSpace)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Star:
        //            holon.ParentStarId = parentCelestialBody.Id;
        //            holon.ParentStar = (IStar)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Planet:
        //            holon.ParentPlanetId = parentCelestialBody.Id;
        //            holon.ParentPlanet = (IPlanet)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Moon:
        //            holon.ParentMoonId = parentCelestialBody.Id;
        //            holon.ParentMoon = (IMoon)parentCelestialBody;
        //            holon.ParentCelestialBodyId = parentCelestialBody.Id;
        //            holon.ParentCelestialBody = (ICelestialBody)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Zome:
        //            holon.ParentZomeId = parentCelestialBody.Id;
        //            holon.ParentZome = (IZome)parentCelestialBody;
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = ParentHolon;
        //            break;

        //        case HolonType.Holon:
        //            holon.ParentHolonId = parentCelestialBody.Id;
        //            holon.ParentHolon = parentCelestialBody;
        //            break;
        //    }

        //    holons.Add(holon);

        //    //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false);
        //    //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false); //TODO: Temp to test new code...

        //    if (saveHolon)
        //    {
        //        result = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType); //TODO: WE ONLY NEED TO SAVE THE NEW HOLON, NO NEED TO RE-SAVE THE WHOLE COLLECTION AGAIN! ;-)
        //        result.IsSaved = true;
        //    }
        //    else
        //    {
        //        result.Message = "Holon was not saved due to saveHolon being set to false.";
        //        result.IsSaved = false;
        //        result.Result = holon;
        //    }

        //    return result;
        //}


        //private IEnumerable<IHolon> RemoveCelesialBodies(IEnumerable<IHolon> holons)
        //{
        //    List<IHolon> holonsList = holons.ToList();

        //    for (int i = 0; i < holonsList.Count(); i++)
        //        holonsList[i] = RemoveCelesialBodies(holonsList[i]);

        //    return holonsList;
        //}

        //private IEnumerable<T> RemoveCelesialBodies<T>(IEnumerable<T> holons) where T : IHolon
        //{
        //    List<T> holonsList = holons.ToList();

        //    for (int i = 0; i < holonsList.Count(); i++)
        //        holonsList[i] = (T)RemoveCelesialBodies(holonsList[i]);

        //    return holonsList;
        //}

        //private IEnumerable<IHolon> RestoreCelesialBodies(IEnumerable<IHolon> holons)
        //{
        //    List<IHolon> restoredHolons = new List<IHolon>();

        //    foreach (IHolon holon in holons)
        //        restoredHolons.Add(RestoreCelesialBodies(holon));

        //    return restoredHolons;
        //}

        //private T RemoveCelesialBodies<T>(T holon) where T : IHolon
        //{
        //    if (holon.Id == Guid.Empty)
        //    {
        //        holon.Id = Guid.NewGuid();
        //        holon.IsNewHolon = true;
        //    }

        //    ICelestialBody celestialBody = holon as ICelestialBody;

        //    if (celestialBody != null)
        //    {
        //        _core[holon.Id] = celestialBody.CelestialBodyCore;
        //        celestialBody.CelestialBodyCore = null;
        //    }

        //    _parentOmiverse[holon.Id] = holon.ParentOmniverse;
        //    _parentDimension[holon.Id] = holon.ParentDimension;
        //    _parentMultiverse[holon.Id] = holon.ParentMultiverse;
        //    _parentUniverse[holon.Id] = holon.ParentUniverse;
        //    _parentGalaxyCluster[holon.Id] = holon.ParentGalaxyCluster;
        //    _parentGalaxy[holon.Id] = holon.ParentGalaxy;
        //    _parentSolarSystem[holon.Id] = holon.ParentSolarSystem;
        //    _parentGreatGrandSuperStar[holon.Id] = holon.ParentGreatGrandSuperStar;
        //    _parentGrandSuperStar[holon.Id] = holon.ParentGrandSuperStar;
        //    _parentSuperStar[holon.Id] = holon.ParentSuperStar;
        //    _parentStar[holon.Id] = holon.ParentStar;
        //    _parentPlanet[holon.Id] = holon.ParentPlanet;
        //    _parentMoon[holon.Id] = holon.ParentMoon;
        //    _parentCelestialSpace[holon.Id] = holon.ParentCelestialSpace;
        //    _parentCelestialBody[holon.Id] = holon.ParentCelestialBody;
        //    _parentZome[holon.Id] = holon.ParentZome;
        //    _parentHolon[holon.Id] = holon.ParentHolon;

        //    holon.ParentOmniverse = null;
        //    holon.ParentDimension = null;
        //    holon.ParentMultiverse = null;
        //    holon.ParentUniverse = null;
        //    holon.ParentGalaxyCluster = null;
        //    holon.ParentGalaxy = null;
        //    holon.ParentSolarSystem = null;
        //    holon.ParentGreatGrandSuperStar = null;
        //    holon.ParentGrandSuperStar = null;
        //    holon.ParentSuperStar = null;
        //    holon.ParentStar = null;
        //    holon.ParentPlanet = null;
        //    holon.ParentMoon = null;
        //    holon.ParentCelestialBody = null;
        //    holon.ParentCelestialSpace = null;
        //    holon.ParentZome = null;
        //    holon.ParentHolon = null;

        //    return holon;
        //}

        //private IHolon RestoreCelesialBodies(IHolon originalHolon)
        //{
        //    originalHolon.IsNewHolon = false;
        //    originalHolon.ParentOmniverse = _parentOmiverse[originalHolon.Id];
        //    originalHolon.ParentDimension = _parentDimension[originalHolon.Id];
        //    originalHolon.ParentMultiverse = _parentMultiverse[originalHolon.Id];
        //    originalHolon.ParentUniverse = _parentUniverse[originalHolon.Id];
        //    originalHolon.ParentGalaxyCluster = _parentGalaxyCluster[originalHolon.Id];
        //    originalHolon.ParentGalaxy = _parentGalaxy[originalHolon.Id];
        //    originalHolon.ParentSolarSystem = _parentSolarSystem[originalHolon.Id];
        //    originalHolon.ParentGreatGrandSuperStar = _parentGreatGrandSuperStar[originalHolon.Id];
        //    originalHolon.ParentGrandSuperStar = _parentGrandSuperStar[originalHolon.Id];
        //    originalHolon.ParentSuperStar = _parentSuperStar[originalHolon.Id];
        //    originalHolon.ParentStar = _parentStar[originalHolon.Id];
        //    originalHolon.ParentPlanet = _parentPlanet[originalHolon.Id];
        //    originalHolon.ParentMoon = _parentMoon[originalHolon.Id];
        //    originalHolon.ParentCelestialSpace = _parentCelestialSpace[originalHolon.Id];
        //    originalHolon.ParentCelestialBody = _parentCelestialBody[originalHolon.Id];
        //    originalHolon.ParentZome = _parentZome[originalHolon.Id];
        //    originalHolon.ParentHolon = _parentHolon[originalHolon.Id];

        //    _parentOmiverse.Remove(originalHolon.Id);
        //    _parentDimension.Remove(originalHolon.Id);
        //    _parentMultiverse.Remove(originalHolon.Id);
        //    _parentUniverse.Remove(originalHolon.Id);
        //    _parentGalaxyCluster.Remove(originalHolon.Id);
        //    _parentGalaxy.Remove(originalHolon.Id);
        //    _parentSolarSystem.Remove(originalHolon.Id);
        //    _parentGreatGrandSuperStar.Remove(originalHolon.Id);
        //    _parentGrandSuperStar.Remove(originalHolon.Id);
        //    _parentSuperStar.Remove(originalHolon.Id);
        //    _parentStar.Remove(originalHolon.Id);
        //    _parentPlanet.Remove(originalHolon.Id);
        //    _parentMoon.Remove(originalHolon.Id);
        //    _parentCelestialSpace.Remove(originalHolon.Id);
        //    _parentCelestialBody.Remove(originalHolon.Id);
        //    _parentZome.Remove(originalHolon.Id);
        //    _parentHolon.Remove(originalHolon.Id);

        //    ICelestialBody celestialBody = originalHolon as ICelestialBody;

        //    if (celestialBody != null)
        //    {
        //        celestialBody.CelestialBodyCore = _core[originalHolon.Id];
        //        _core.Remove(originalHolon.Id);
        //        return celestialBody;
        //    }

        //    return originalHolon;
        //}

        //private void HandleSaveHolonResult<T>(IHolon savingHolon, string callingMethodName, ref OASISResult<T> result) where T : IHolon, new()
        //{
        //    if (!result.IsError && result.Result != null)
        //    {
        //        //if (mapBaseHolonProperties)
        //        //  Mapper.MapBaseHolonProperties(savingHolon, result.Result);

        //        result.Result = (T)RestoreCelesialBodies(savingHolon);
        //        //OASISResult<IHolon> holonResult = new OASISResult<IHolon>(result.Result);
        //        //OASISResultHelper.CopyResult(result, holonResult);
        //        //OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = holonResult });

        //        OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = OASISResultHelper.CopyResult<T, IHolon>(result) });
        //    }
        //    else
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //}

        //private void HandleSaveHolonResult(IHolon savingHolon, string callingMethodName, ref OASISResult<IHolon> result)
        //{
        //    if (!result.IsError && result.Result != null)
        //    {
        //        //if (mapBaseHolonProperties)
        //        //   Mapper.MapBaseHolonProperties(savingHolon, result.Result);

        //        result.Result = RestoreCelesialBodies(savingHolon);
        //        //result.Result = RestoreCelesialBodies(result.Result);
        //        OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
        //    }
        //    else
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), Exception = result.Exception });
        //}

        //private void HandleSaveHolonsResult(IEnumerable<IHolon> savingHolons, string callingMethodName, ref OASISResult<IEnumerable<IHolon>> result)
        //{
        //    if (!result.IsError && result.Result != null)
        //    {
        //        //TODO: FIND OUT IF THIS IS STILL NEEDED ASAP?! THANKS! ;-)
        //        //if (mapBaseHolonProperties)
        //        //   result.Result = Mapper.MapBaseHolonProperties(savingHolons, result.Result);

        //        result.Result = RestoreCelesialBodies(savingHolons);
        //        OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
        //    }
        //    else
        //        OnError?.Invoke(this, new HolonErrorEventArgs() { Reason = string.Concat("Error in SaveHolonsAsync method. Error Details: ", result.Message), Exception = result.Exception });
        //}

        /*
        //https://stackoverflow.com/questions/2363801/what-would-be-the-best-way-to-implement-change-tracking-on-an-object
        //https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.ichangetracking?redirectedfrom=MSDN&view=net-5.0

        protected bool SetProperty<T>(string name, ref T oldValue, T newValue) where T : IComparable<T>
        {
            if (oldValue == null || oldValue.CompareTo(newValue) != 0)
            {
                oldValue = newValue;
               // PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
               // isDirty = true;
                return true;
            }
            return false;
        }
        // For nullable types
        protected void SetProperty<T>(string name, ref Nullable<T> oldValue, Nullable<T> newValue) where T : struct, IComparable<T>
        {
            if (oldValue.HasValue != newValue.HasValue || (newValue.HasValue && oldValue.Value.CompareTo(newValue.Value) != 0))
            {
                oldValue = newValue;
                //PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
            }
        }*/

        //private void SetProperties(OASISResult<IHolon> result)
        //{
        //    this.Name = result.Result.Name;
        //    this.Description = result.Result.Description;
        //    this.CreatedByAvatar = result.Result.CreatedByAvatar;
        //    this.CreatedByAvatarId = result.Result.CreatedByAvatarId;
        //    this.CreatedDate = result.Result.CreatedDate;
        //    this.CreatedOASISType = result.Result.CreatedOASISType;
        //    this.CreatedProviderType = result.Result.CreatedProviderType;
        //    this.CustomKey = result.Result.CustomKey;
        //    this.DeletedByAvatar = result.Result.DeletedByAvatar;
        //    this.DeletedByAvatarId = result.Result.DeletedByAvatarId;
        //    this.DeletedDate = result.Result.DeletedDate;
        //    this.HolonType = result.Result.HolonType;
        //    this.Id = result.Result.Id;
        //    this.InstanceSavedOnProviderType = result.Result.InstanceSavedOnProviderType;
        //    this.IsActive = result.Result.IsActive;
        //    this.IsChanged = result.Result.IsChanged;
        //    this.IsNewHolon = result.Result.IsNewHolon;
        //    this.IsSaving = result.Result.IsSaving;
        //    this.MetaData = result.Result.MetaData;
        //    this.ModifiedByAvatar = result.Result.ModifiedByAvatar;
        //    this.ModifiedByAvatarId = result.Result.ModifiedByAvatarId;
        //    this.ModifiedDate = result.Result.ModifiedDate;
        //    this.Original = result.Result.Original;
        //    this.PreviousVersionId = result.Result.PreviousVersionId;
        //    this.PreviousVersionProviderUniqueStorageKey = result.Result.PreviousVersionProviderUniqueStorageKey;
        //    this.ProviderMetaData = result.Result.ProviderMetaData;
        //    this.ProviderUniqueStorageKey = result.Result?.ProviderUniqueStorageKey;
        //    this.Version = result.Result.Version;
        //    this.VersionId = result.Result.VersionId;
        //    //this = Mapper<IHolon, HolonBase>.MapBaseHolonProperties(result.Result);
        //}

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
