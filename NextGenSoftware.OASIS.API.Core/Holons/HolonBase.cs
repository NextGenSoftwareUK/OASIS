using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public abstract class HolonBase : IHolonBase, INotifyPropertyChanged
    {
        private string _name;
        private string _description;

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

        //public HolonBase(Dictionary<ProviderType, string> providerKeys)
        //{
        //    ProviderUniqueStorageKey = providerKeys;
        //}

        public HolonBase(HolonType holonType)
        {
            IsNewHolon = true;
            HolonType = holonType;
        }

        public HolonBase()
        {
            IsNewHolon = true;
        }

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
        //public Dictionary<string, string> CustomKeys { get; set; }

        public bool IsNewHolon { get; set; }
        public bool IsChanged { get; set; }
        public bool IsSaving { get; set; }

        //public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique key used by each provider (e.g. hashaddress in hc, accountname for Telos, id in MongoDB etc).        
        //public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.
        //public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>(); // Key/Value pair meta data can be stored here that applies globally across ALL providers.
        public HolonType HolonType { get; set; }

        /*
        public Guid ParentOmniverseId { get; set; } //The Omniverse this Holon belongs to.
        public IOmiverse ParentOmniverse { get; set; } //The Omniverse this Holon belongs to.
        public Guid ParentMultiverseId { get; set; } //The Multiverse this Holon belongs to.
        public IMultiverse ParentMultiverse { get; set; } //The Multiverse this Holon belongs to.
        public Guid ParentUniverseId { get; set; } //The Universe this Holon belongs to.
        public IUniverse ParentUniverse { get; set; } //The Universe this Holon belongs to.
        public Guid ParentDimensionId { get; set; } //The Dimension this Holon belongs to.
        public IDimension ParentDimension { get; set; } //The Dimension this Holon belongs to.
        public DimensionLevel DimensionLevel { get; set; } //The dimension this Holon belongs to (a holon can have a different version of itself in each dimension (asscended/evolved versions of itself).
        public SubDimensionLevel SubDimensionLevel { get; set; } //The sub-dimension/plane this Holon belongs to.
        public Guid ParentGalaxyClusterId { get; set; } //The GalaxyCluster this Holon belongs to.
        public IGalaxyCluster ParentGalaxyCluster { get; set; } //The GalaxyCluster this Holon belongs to.
        public Guid ParentGalaxyId { get; set; } //The Galaxy this Holon belongs to.
        public IGalaxy ParentGalaxy { get; set; } //The Galaxy this Holon belongs to.
        public Guid ParentSolarSystemId { get; set; } //The SolarSystem this Holon belongs to.
        public ISolarSystem ParentSolarSystem { get; set; } //The SolarSystem this Holon belongs to.
        public Guid ParentGreatGrandSuperStarId { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get; set; } //The GreatGrandSuperStar this Holon belongs to.
        public Guid ParentGrandSuperStarId { get; set; } //The GrandSuperStar this Holon belongs to.
        public IGrandSuperStar ParentGrandSuperStar { get; set; } //The GrandSuperStar this Holon belongs to.
        public Guid ParentSuperStarId { get; set; } //The SuperStar this Holon belongs to.
        public ISuperStar ParentSuperStar { get; set; } //The SuperStar this Holon belongs to.
        public Guid ParentStarId { get; set; } //The Star this Holon belongs to.
        //public ICelestialBody ParentStar { get; set; } //The Star this Holon belongs to.
        public IStar ParentStar { get; set; } //The Star this Holon belongs to.
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        //public ICelestialBody ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.
        //public ICelestialBody ParentMoon { get; set; } //The Moon this Holon belongs to.
        public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        //public Guid ParentCelestialBodyId { get; set; } //The CelestialBody (Planet or Moon (OApp)) this Holon belongs to.
        //public ICelestialBody ParentCelestialBody { get; set; } //The CelestialBody (Planet or Moon (OApp)) this Holon belongs to.
        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public Guid ParentHolonId { get; set; }
        public IHolon ParentHolon { get; set; }
        public IEnumerable<IHolon> Children { get; set; }
        public ObservableCollection<IHolon> ChildrenTest { get; set; }
        */
        public Guid CreatedByAvatarId { get; set; }
        public Avatar CreatedByAvatar { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        public Avatar ModifiedByAvatar { get; set; }
        public DateTime ModifiedDate { get; set; }
        public Guid DeletedByAvatarId { get; set; }
        public Avatar DeletedByAvatar { get; set; }
        public DateTime DeletedDate { get; set; }
        public int Version { get; set; }
        public Guid VersionId { get; set; }
        public Guid PreviousVersionId { get; set; }
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();
        public bool IsActive { get; set; }
        public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
                                                                         //public List<INode> Nodes { get; set; } // List of nodes/fields (int, string, bool, etc) that belong to this Holon (STAR ODK auto-generates these when generating dynamic code from DNA Templates passed in).
                                                                         //  public ObservableCollection<INode> Nodes { get; set; }

        public EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }

        public EnumValue<OASISType> CreatedOASISType { get; set; }

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

                /*
                if (Original.Nodes.Count != Nodes.Count)
                    return true;

                foreach (INode node in Original.Nodes)
                {
                   // if (node)
                }

                if (Original.MetaData.Keys.Count != MetaData.Keys.Count)
                    return true;

                foreach (string key in Original.MetaData.Keys)
                {
                    if (Original.MetaData[key] != MetaData[key])
                        return true;
                }

                //Will recursively check all children.
                if (checkChildren)
                {
                    if (Original.Children.Count() != Children.Count())
                        return true;

                    List<IHolon> origChildren = Original.Children.ToList();
                    List<IHolon> children = Children.ToList();

                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].Id != origChildren[i].Id)
                            return true;

                        if (children[i].Name != origChildren[i].Name)
                            return true;

                        if (children[i].Description != origChildren[i].Description)
                            return true;

                        //TODO: Add rest of properties here.

                        if (children[i].HasHolonChanged())
                            return true;
                    }
                }*/

            }
            //TODO: Finish this ASAP!

            return Id == Guid.Empty;
        }

        public async Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = await HolonManager.Instance.LoadHolonAsync(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

                if (result != null && !result.IsError && result.Result != null)
                    SetProperties(result.Result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadAsync Calling HolonManager.LoadHolonAsync. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result = await HolonManager.Instance.LoadHolonAsync<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    MapMetaData<T>();
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.LoadAsync Calling HolonManager.LoadHolonAsync. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = HolonManager.Instance.LoadHolon(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

                if (result != null && !result.IsError && result.Result != null)
                    SetProperties(result.Result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Load Calling HolonManager.LoadHolon. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result = HolonManager.Instance.LoadHolon<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

                if (result != null && !result.IsError && result.Result != null)
                {
                    SetProperties(result.Result);
                    MapMetaData<T>();
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Load Calling HolonManager.LoadHolon. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = await HolonManager.Instance.SaveHolonAsync((IHolon)this, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

                if (result != null && !result.IsError && result.Result != null)
                    SetProperties(result.Result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.SaveAsync Calling HolonManager.SaveHolonAsync. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result = await HolonManager.Instance.SaveHolonAsync<T>((IHolon)this, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

                if (result != null && !result.IsError && result.Result != null)
                    SetProperties(result.Result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.SaveAsync Calling HolonManager.SaveHolonAsync. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IHolon> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            try
            {
                result = HolonManager.Instance.SaveHolon((IHolon)this, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

                if (result != null && !result.IsError && result.Result != null)
                    SetProperties(result.Result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Save Calling HolonManager.SaveHolon. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();

            try
            {
                result = HolonManager.Instance.SaveHolon<T>((IHolon)this, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

                if (result != null && !result.IsError && result.Result != null)
                    SetProperties(result.Result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Save Calling HolonManager.SaveHolon. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAsync(bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                result = await HolonManager.Instance.DeleteHolonAsync(this.Id, softDelete, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.DeleteAsync Calling HolonManager.DeleteHolonAsync. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<bool> Delete(bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                result = HolonManager.Instance.DeleteHolon(this.Id, softDelete, providerType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured in HolonBase.Delete Calling HolonManager.DeleteHolon. Reason: {ex}");
            }

            return result;
        }

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
            this.CustomKey = holon.CustomKey;
            this.DeletedByAvatar = holon.DeletedByAvatar;
            this.DeletedByAvatarId = holon.DeletedByAvatarId;
            this.DeletedDate = holon.DeletedDate;
            this.HolonType = holon.HolonType;
            this.Id = holon.Id;
            this.InstanceSavedOnProviderType = holon.InstanceSavedOnProviderType;
            this.IsActive = holon.IsActive;
            this.IsChanged = holon.IsChanged;
            this.IsNewHolon = holon.IsNewHolon;
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
    }
}
