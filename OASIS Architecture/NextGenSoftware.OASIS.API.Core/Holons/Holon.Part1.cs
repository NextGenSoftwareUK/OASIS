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

        /// <summary>
        /// Optional per-call override for holon MetaData encryption. Not persisted.
        /// Set this before calling SaveHolon to override the global HolonDataEncryption setting in OASISDNA.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public EncryptionSettings DataEncryptionOverride { get; set; }

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




        //TODO: TEMP MOVED TO HOLONBASE TILL REFACTOR CODEBASE.
        //public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique key used by each provider (e.g. hashaddress in hc, accountname for Telos, id in MongoDB etc).        
        //public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get; set; } = new Dictionary<ProviderType, Dictionary<string, string>>(); // Key/Value pair meta data can be stored here, which is unique for that provider.
        //public string CustomKey { get; set; } //A custom key that can be used to load the holon by (other than Id or ProviderKey).
        //public bool IsNewHolon { get; set; } //TODO: Want to remove this ASAP!
        //public bool IsSaving { get; set; }

        //public Guid PreviousVersionId { get; set; }
        //public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get; set; } = new Dictionary<ProviderType, string>();

        //public EnumValue<ProviderType> CreatedProviderType { get; set; } // The primary provider that this holon was originally saved with (it can then be auto-replicated to other providers to give maximum redundancy/speed via auto-load balancing etc).
        //public EnumValue<ProviderType> InstanceSavedOnProviderType { get; set; }
        //public EnumValue<OASISType> CreatedOASISType { get; set; }


        //FROM CELESTIALHOLON - TODO: NEED TO REFFACTOR CODEBASE LATER TO USE ICELESTIALHOLON INSTEAD OF IHolon WHERE APPROPRIATE.
        public IList<INode> Nodes { get; set; } = new List<INode>();
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
        public IStar ParentStar { get; set; } //The Star this Holon belongs to.
        public Guid ParentPlanetId { get; set; } //The Planet this Holon belongs to.
        public IPlanet ParentPlanet { get; set; } //The Planet this Holon belongs to.
        public Guid ParentMoonId { get; set; } //The Moon this Holon belongs to.
        public IMoon ParentMoon { get; set; } //The Moon this Holon belongs to.
        public Guid ParentCelestialSpaceId { get; set; } // The CelestialSpace Id this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        public ICelestialSpace ParentCelestialSpace { get; set; } // The CelestialSpace this holon belongs to (this could be a Solar System, Galaxy, Universe, etc). 
        public Guid ParentCelestialBodyId { get; set; } // The CelestialBody Id this holon belongs to (this could be a moon, planet, star, etc). 
        public ICelestialBody ParentCelestialBody { get; set; } // The CelestialBody  this holon belongs to (this could be a moon, planet, star, etc). 
        public Guid ParentZomeId { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.
        public IZome ParentZome { get; set; } // The zome this holon belongs to. Zomes are like re-usable modules that other OApp's can be composed of. Zomes contain collections of nested holons (data objects). Holons can be infinite depth.




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
    }
}
