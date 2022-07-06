using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.Zomes
{
    public abstract class ZomeBase : Holon, IZomeBase
    {
        private HolonManager _holonManager = null;
        private const string CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET = "Both Id and ProviderUniqueStorageKey are null, one of these need to be set before calling this method.";
        private Dictionary<Guid, IOmiverse> _parentOmiverse = new Dictionary<Guid, IOmiverse>();
        private Dictionary<Guid, IDimension> _parentDimension = new Dictionary<Guid, IDimension>();
        private Dictionary<Guid, IMultiverse> _parentMultiverse = new Dictionary<Guid, IMultiverse>();
        private Dictionary<Guid, IUniverse> _parentUniverse = new Dictionary<Guid, IUniverse>();
        private Dictionary<Guid, IGalaxyCluster> _parentGalaxyCluster = new Dictionary<Guid, IGalaxyCluster>();
        private Dictionary<Guid, IGalaxy> _parentGalaxy = new Dictionary<Guid, IGalaxy>();
        private Dictionary<Guid, ISolarSystem> _parentSolarSystem = new Dictionary<Guid, ISolarSystem>();
        private Dictionary<Guid, IGreatGrandSuperStar> _parentGreatGrandSuperStar = new Dictionary<Guid, IGreatGrandSuperStar>();
        private Dictionary<Guid, IGrandSuperStar> _parentGrandSuperStar = new Dictionary<Guid, IGrandSuperStar>();
        private Dictionary<Guid, ISuperStar> _parentSuperStar = new Dictionary<Guid, ISuperStar>();
        private Dictionary<Guid, IStar> _parentStar = new Dictionary<Guid, IStar>();
        private Dictionary<Guid, IPlanet> _parentPlanet = new Dictionary<Guid, IPlanet>();
        private Dictionary<Guid, IMoon> _parentMoon = new Dictionary<Guid, IMoon>();
        private Dictionary<Guid, ICelestialSpace> _parentCelestialSpace = new Dictionary<Guid, ICelestialSpace>();
        private Dictionary<Guid, ICelestialBody> _parentCelestialBody = new Dictionary<Guid, ICelestialBody>();
        private Dictionary<Guid, IZome> _parentZome = new Dictionary<Guid, IZome>();
        private Dictionary<Guid, IHolon> _parentHolon = new Dictionary<Guid, IHolon>();
        private Dictionary<Guid, ICelestialBodyCore> _core = new Dictionary<Guid, ICelestialBodyCore>();

        public List<IHolon> _holons = new List<IHolon>();

        public List<IHolon> Holons
        {
            get
            {
                return _holons;
            }
            set
            {
                _holons = value;
            }
        }

        public event Events.Initialized OnInitialized;
        public event Events.HolonLoaded OnHolonLoaded;
        public event Events.HolonsLoaded OnHolonsLoaded;
        public event Events.HolonSaved OnHolonSaved;
       // public event Events.HolonSaved<T> OnHolonSaved;
        public event Events.HolonsSaved OnHolonsSaved;
        public event Events.ZomeSaved OnSaved;
        public event Events.HolonAdded OnHolonAdded;
        public event Events.HolonRemoved OnHolonRemoved;
        //public event Events.ZomesLoaded OnZomesLoaded;
        public event Events.ZomeError OnZomeError;

        ////TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public event Disconnected OnDisconnected;
        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public event DataReceived OnDataReceived;

        public ZomeBase()
        {
            OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

            //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
            if (result.IsError)
            {
                string errorMessage = string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message);
                ErrorHandling.HandleError(ref result, errorMessage, true, false, true);
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = errorMessage });
            }
            else
            {
                _holonManager = new HolonManager(result.Result);
                OnInitialized?.Invoke(this, new System.EventArgs());
            }
        }

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = await _holonManager.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = _holonManager.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }


        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(Dictionary<ProviderType, string> providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = await _holonManager.LoadHolonAsync(GetCurrentProviderKey(providerKey), loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(Dictionary<ProviderType, string> providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = _holonManager.LoadHolon(GetCurrentProviderKey(providerKey), loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (this.HolonType == HolonType.GreatGrandSuperStar)
                GetGreatGrandSuperStar(ref result, await LoadAllHolonsAsync(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, version));

            else if (this.Id != Guid.Empty)
                result = await LoadHolonAsync(Id);

            else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                result = await LoadHolonAsync(ProviderUniqueStorageKey);
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (this.HolonType == HolonType.GreatGrandSuperStar)
                GetGreatGrandSuperStar(ref result, LoadAllHolons(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, version));

            else if (this.Id != Guid.Empty)
                result = LoadHolon(Id);

            else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
                result = LoadHolon(ProviderUniqueStorageKey);
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            // this = result.Result;
            if (result != null && !result.IsError && result.Result != null)
                Mapper.MapBaseHolonProperties(result.Result, this);

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadAllHolons method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParentAsync method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParent method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Dictionary<ProviderType, string> providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadHolonsForParentAsync(GetCurrentProviderKey(providerKey), holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParentAsync method with providerKey ", GetCurrentProviderKey(providerKey), " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Dictionary<ProviderType, string> providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadHolonsForParent(GetCurrentProviderKey(providerKey), holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParent method with providerKey ", GetCurrentProviderKey(providerKey), " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (this.Id != Guid.Empty)
                result = await LoadHolonsForParentAsync(Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            else if (this.ProviderUniqueStorageKey != null)
                result = await LoadHolonsForParentAsync(ProviderUniqueStorageKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (this.Id != Guid.Empty)
                result = LoadHolonsForParent(Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);

            else if (this.ProviderUniqueStorageKey != null)
                result = LoadHolonsForParent(ProviderUniqueStorageKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<IHolon> result = await _holonManager.SaveHolonAsync(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError);
            HandleSaveHolonResult(savingHolon, "SaveHolonAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual OASISResult<IHolon> SaveHolon(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<IHolon> result = _holonManager.SaveHolon(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError);
            HandleSaveHolonResult(savingHolon, "SaveHolon", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon, new()
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<T> result = await _holonManager.SaveHolonAsync<T>(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError);
            HandleSaveHolonResult(savingHolon, "SaveHolonAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual OASISResult<T> SaveHolon<T>(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon, new()
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<T> result = _holonManager.SaveHolon<T>(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError);
            HandleSaveHolonResult(savingHolon, "SaveHolon", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            if (savingHolons == null)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            savingHolons = RemoveCelesialBodies(savingHolons);
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.SaveHolonsAsync(savingHolons, saveChildren, recursive, maxChildDepth, continueOnError);
            HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            if (savingHolons == null)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            savingHolons = RemoveCelesialBodies(savingHolons);
            OASISResult<IEnumerable<IHolon>> result = _holonManager.SaveHolons(savingHolons, saveChildren, recursive, maxChildDepth, continueOnError);
            HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual async Task<OASISResult<IZome>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            OASISResult<IZome> zomeResult = new OASISResult<IZome>((IZome)this);

            //First save the zome.
            OASISResult<IHolon> holonResult = await SaveHolonAsync(this, true, saveChildren, recursive, maxChildDepth, continueOnError);

            if (holonResult.IsError)
            {
                zomeResult.IsError = true;
                zomeResult.Message = holonResult.Message;
            }

            if (saveChildren && (!zomeResult.IsError && holonResult.Result != null)
                || continueOnError && (zomeResult.IsError || holonResult.Result == null))
            {
                ZomeHelper.SetParentIdsForZome(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the way it saves (batches, etc), which would be quicker than making multiple calls...)
                OASISResult<IEnumerable<IHolon>> holonsResult = await SaveHolonsAsync(this.Holons, true, saveChildren, recursive, maxChildDepth, continueOnError);

                if (holonsResult.IsError)
                {
                    zomeResult.IsError = true;
                    zomeResult.Message = holonsResult.Message;
                }

                if ((continueOnError && (holonsResult.IsError || holonResult.Result == null))
                    || (!holonResult.IsError && holonResult.Result != null))
                    this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.
            }

            OnSaved?.Invoke(this, new ZomeSavedEventArgs() { Result = zomeResult });
            return zomeResult;
        }

        public virtual OASISResult<IZome> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            OASISResult<IZome> zomeResult = new OASISResult<IZome>((IZome)this);

            //First save the zome.
            OASISResult<IHolon> holonResult = SaveHolon(this, true, saveChildren, recursive, maxChildDepth, continueOnError);

            if (holonResult.IsError)
            {
                zomeResult.IsError = true;
                zomeResult.Message = holonResult.Message;
            }

            if (saveChildren && (!zomeResult.IsError && holonResult.Result != null)
                || continueOnError && (zomeResult.IsError || holonResult.Result == null))
            {
                ZomeHelper.SetParentIdsForZome(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the the way it saves (batches, etc), which would be quicker than making multiple calls...)
                OASISResult<IEnumerable<IHolon>> holonsResult = SaveHolons(this.Holons, saveChildren, recursive, continueOnError);

                if (holonsResult.IsError)
                {
                    zomeResult.IsError = true;
                    zomeResult.Message = holonsResult.Message;
                }
                
                if ((continueOnError && (holonsResult.IsError || holonResult.Result == null)) 
                    || (!holonResult.IsError && holonResult.Result != null))
                    this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.
            }

            OnSaved?.Invoke(this, new ZomeSavedEventArgs() { Result = zomeResult });
            return zomeResult;
        }

        public async Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            holon.ParentHolonId = this.Id;
            holon.ParentZomeId = this.Id;
            this.Holons.Add(holon);

            OASISResult<IHolon> result = await SaveHolonAsync(holon, true, saveChildren, recursive, maxChildDepth, continueOnError);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in AddHolonAsync method with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IHolon> AddHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            holon.ParentHolonId = this.Id;
            holon.ParentZomeId = this.Id;
            this.Holons.Add(holon);

            OASISResult<IHolon> result = SaveHolon(holon, true, saveChildren, recursive, maxChildDepth, continueOnError);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in AddHolon method with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, bool deleteHolon = false, bool softDelete = true)
        {
            holon.ParentHolonId = Guid.Empty;
            holon.ParentZomeId = Guid.Empty;
            this.Holons.Remove(holon);

            OASISResult<IHolon> result = await SaveHolonAsync(holon, true, false, false, 0, false);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in RemoveHolonAsync method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });

            if (deleteHolon)
            {
                OASISResult<bool> deleteHolonResult = await _holonManager.DeleteHolonAsync(holon.Id, softDelete);

                if (deleteHolonResult.IsError)
                    OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in RemoveHolonAsync method attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });
            }

            OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IHolon> RemoveHolon(IHolon holon, bool deleteHolon = false, bool softDelete = true)
        {
            holon.ParentHolonId = Guid.Empty;
            holon.ParentZomeId = Guid.Empty;
            this.Holons.Remove(holon);

            OASISResult<IHolon> result = SaveHolon(holon, true, false, false, 0, false);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in RemoveHolon method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });

            if (deleteHolon)
            {
                OASISResult<bool> deleteHolonResult = _holonManager.DeleteHolon(holon.Id, softDelete);

                if (deleteHolonResult.IsError)
                    OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in RemoveHolon method attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });
            }

            OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            return result;
        }

        private string GetCurrentProviderKey(Dictionary<ProviderType, string> providerKey)
        {
            if (ProviderUniqueStorageKey.ContainsKey(ProviderManager.CurrentStorageProviderType.Value) && !string.IsNullOrEmpty(ProviderUniqueStorageKey[ProviderManager.CurrentStorageProviderType.Value]))
                return providerKey[ProviderManager.CurrentStorageProviderType.Value];
            else
                throw new Exception(string.Concat("ProviderUniqueStorageKey not found for CurrentStorageProviderType ", ProviderManager.CurrentStorageProviderType.Name));

            //TODO: Return OASISResult instead of throwing exceptions for ALL OASIS methods!
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

        private IEnumerable<IHolon> RemoveCelesialBodies(IEnumerable<IHolon> holons)
        {
            List<IHolon> holonsList = holons.ToList();

            for (int i = 0; i < holonsList.Count(); i++)
                holonsList[i] = RemoveCelesialBodies(holonsList[i]);

            return holonsList;
        }

        private IEnumerable<IHolon> RestoreCelesialBodies(IEnumerable<IHolon> holons)
        {
            List<IHolon> restoredHolons = new List<IHolon>();

            foreach (IHolon holon in holons)
                restoredHolons.Add(RestoreCelesialBodies(holon));

            return restoredHolons;
        }

        private IHolon RemoveCelesialBodies(IHolon holon)
        {
            if (holon.Id == Guid.Empty)
            {
                holon.Id = Guid.NewGuid();
                holon.IsNewHolon = true;
            }

            ICelestialBody celestialBody = holon as ICelestialBody;

            if (celestialBody != null)
            {
                _core[holon.Id] = celestialBody.CelestialBodyCore;
                celestialBody.CelestialBodyCore = null;
            }

            _parentOmiverse[holon.Id] = holon.ParentOmniverse;
            _parentDimension[holon.Id] = holon.ParentDimension;
            _parentMultiverse[holon.Id] = holon.ParentMultiverse;
            _parentUniverse[holon.Id] = holon.ParentUniverse;
            _parentGalaxyCluster[holon.Id] = holon.ParentGalaxyCluster;
            _parentGalaxy[holon.Id] = holon.ParentGalaxy;
            _parentSolarSystem[holon.Id] = holon.ParentSolarSystem;
            _parentGreatGrandSuperStar[holon.Id] = holon.ParentGreatGrandSuperStar;
            _parentGrandSuperStar[holon.Id] = holon.ParentGrandSuperStar;
            _parentSuperStar[holon.Id] = holon.ParentSuperStar;
            _parentStar[holon.Id] = holon.ParentStar;
            _parentPlanet[holon.Id] = holon.ParentPlanet;
            _parentMoon[holon.Id] = holon.ParentMoon;
            _parentCelestialSpace[holon.Id] = holon.ParentCelestialSpace;
            _parentCelestialBody[holon.Id] = holon.ParentCelestialBody;
            _parentZome[holon.Id] = holon.ParentZome;
            _parentHolon[holon.Id] = holon.ParentHolon;

            holon.ParentOmniverse = null;
            holon.ParentDimension = null;
            holon.ParentMultiverse = null;
            holon.ParentUniverse = null;
            holon.ParentGalaxyCluster = null;
            holon.ParentGalaxy = null;
            holon.ParentSolarSystem = null;
            holon.ParentGreatGrandSuperStar = null;
            holon.ParentGrandSuperStar = null;
            holon.ParentSuperStar = null;
            holon.ParentStar = null;
            holon.ParentPlanet = null;
            holon.ParentMoon = null;
            holon.ParentCelestialBody = null;
            holon.ParentCelestialSpace = null;
            holon.ParentZome = null;
            holon.ParentHolon = null;

            return holon;
        }

        private IHolon RestoreCelesialBodies(IHolon originalHolon) 
        {
            originalHolon.IsNewHolon = false;
            originalHolon.ParentOmniverse = _parentOmiverse[originalHolon.Id];
            originalHolon.ParentDimension = _parentDimension[originalHolon.Id];
            originalHolon.ParentMultiverse = _parentMultiverse[originalHolon.Id];
            originalHolon.ParentUniverse = _parentUniverse[originalHolon.Id];
            originalHolon.ParentGalaxyCluster = _parentGalaxyCluster[originalHolon.Id];
            originalHolon.ParentGalaxy = _parentGalaxy[originalHolon.Id];
            originalHolon.ParentSolarSystem = _parentSolarSystem[originalHolon.Id];
            originalHolon.ParentGreatGrandSuperStar = _parentGreatGrandSuperStar[originalHolon.Id];
            originalHolon.ParentGrandSuperStar = _parentGrandSuperStar[originalHolon.Id];
            originalHolon.ParentSuperStar = _parentSuperStar[originalHolon.Id];
            originalHolon.ParentStar = _parentStar[originalHolon.Id];
            originalHolon.ParentPlanet = _parentPlanet[originalHolon.Id];
            originalHolon.ParentMoon = _parentMoon[originalHolon.Id];
            originalHolon.ParentCelestialSpace = _parentCelestialSpace[originalHolon.Id];
            originalHolon.ParentCelestialBody = _parentCelestialBody[originalHolon.Id];
            originalHolon.ParentZome = _parentZome[originalHolon.Id];
            originalHolon.ParentHolon = _parentHolon[originalHolon.Id];

            _parentOmiverse.Remove(originalHolon.Id);
            _parentDimension.Remove(originalHolon.Id);
            _parentMultiverse.Remove(originalHolon.Id);
            _parentUniverse.Remove(originalHolon.Id);
            _parentGalaxyCluster.Remove(originalHolon.Id);
            _parentGalaxy.Remove(originalHolon.Id);
            _parentSolarSystem.Remove(originalHolon.Id);
            _parentGreatGrandSuperStar.Remove(originalHolon.Id);
            _parentGrandSuperStar.Remove(originalHolon.Id);
            _parentSuperStar.Remove(originalHolon.Id);
            _parentStar.Remove(originalHolon.Id);
            _parentPlanet.Remove(originalHolon.Id);
            _parentMoon.Remove(originalHolon.Id);
            _parentCelestialSpace.Remove(originalHolon.Id);
            _parentCelestialBody.Remove(originalHolon.Id);
            _parentZome.Remove(originalHolon.Id);
            _parentHolon.Remove(originalHolon.Id);

            ICelestialBody celestialBody = originalHolon as ICelestialBody;

            if (celestialBody != null)
            {
                celestialBody.CelestialBodyCore = _core[originalHolon.Id];
                _core.Remove(originalHolon.Id);
                return celestialBody;
            }

            return originalHolon;
        }

        private void HandleSaveHolonResult<T>(IHolon savingHolon, string callingMethodName, ref OASISResult<T> result, bool mapBaseHolonProperties = true) where T : IHolon, new()
        {
            if (!result.IsError && result.Result != null)
            {
                if (mapBaseHolonProperties)
                    Mapper<IHolon, T>.MapBaseHolonProperties(savingHolon, result.Result);

                result.Result = (T)RestoreCelesialBodies(savingHolon);
                OASISResult<IHolon> holonResult = new OASISResult<IHolon>(result.Result);
                OASISResultHelper<T, IHolon>.CopyResult(result, holonResult);
                OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = holonResult });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), Exception = result.Exception });
        }

        private void HandleSaveHolonResult(IHolon savingHolon, string callingMethodName, ref OASISResult<IHolon> result, bool mapBaseHolonProperties = true)
        {
            if (!result.IsError && result.Result != null)
            {
                if (mapBaseHolonProperties)
                    Mapper.MapBaseHolonProperties(savingHolon, result.Result);

                result.Result = RestoreCelesialBodies(savingHolon);
                //result.Result = RestoreCelesialBodies(result.Result);
                OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), Exception = result.Exception });
        }

        private void HandleSaveHolonsResult(IEnumerable<IHolon> savingHolons, string callingMethodName, ref OASISResult<IEnumerable<IHolon>> result, bool mapBaseHolonProperties = true)
        {
            if (!result.IsError && result.Result != null)
            {
                //TODO: FIND OUT IF THIS IS STILL NEEDED ASAP?! THANKS! ;-)
                if (mapBaseHolonProperties)
                    result.Result = Mapper.MapBaseHolonProperties(savingHolons, result.Result);

                result.Result = RestoreCelesialBodies(savingHolons);
                OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in SaveHolonsAsync method. Error Details: ", result.Message), Exception = result.Exception });
        }
    }
}