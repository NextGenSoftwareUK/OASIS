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
        private const string CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET = "Both Id and ProviderKey are null, one of these need to be set before calling this method.";
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
            OASISResult<IOASISStorage> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

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

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id)
        {
            OASISResult<IHolon> result = await _holonManager.LoadHolonAsync(id);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with id ", id, ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(Guid id)
        {
            OASISResult<IHolon> result = _holonManager.LoadHolon(id);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with id ", id, ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }


        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(Dictionary<ProviderType, string> providerKey)
        {
            OASISResult<IHolon> result = await _holonManager.LoadHolonAsync(GetCurrentProviderKey(providerKey));

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(Dictionary<ProviderType, string> providerKey)
        {
            OASISResult<IHolon> result = _holonManager.LoadHolon(GetCurrentProviderKey(providerKey));

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with providerKey ", providerKey, ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync()
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (this.HolonType == HolonType.GreatGrandSuperStar)
                GetGreatGrandSuperStar(ref result, await LoadAllHolonsAsync(HolonType.GreatGrandSuperStar));

            else if (this.Id != Guid.Empty)
                result = await LoadHolonAsync(Id);

            else if (this.ProviderKey != null && this.ProviderKey.Count > 0)
                result = await LoadHolonAsync(ProviderKey);
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon()
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (this.HolonType == HolonType.GreatGrandSuperStar)
                GetGreatGrandSuperStar(ref result, LoadAllHolons(HolonType.GreatGrandSuperStar));

            else if (this.Id != Guid.Empty)
                result = LoadHolon(Id);

            else if (this.ProviderKey != null && this.ProviderKey.Count > 0)
                result = LoadHolon(ProviderKey);
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadAllHolonsAsync(holonType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadAllHolons(holonType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadAllHolons method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadHolonsForParentAsync(id, holonType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParentAsync method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadHolonsForParent(id, holonType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParent method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Dictionary<ProviderType, string> providerKey, HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadHolonsForParentAsync(GetCurrentProviderKey(providerKey), holonType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParentAsync method with providerKey ", GetCurrentProviderKey(providerKey), " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Dictionary<ProviderType, string> providerKey, HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadHolonsForParent(GetCurrentProviderKey(providerKey), holonType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParent method with providerKey ", GetCurrentProviderKey(providerKey), " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (this.Id != Guid.Empty)
                result = await LoadHolonsForParentAsync(Id, holonType);

            else if (this.ProviderKey != null)
                result = await LoadHolonsForParentAsync(ProviderKey, holonType);
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (this.Id != Guid.Empty)
                result = LoadHolonsForParent(Id, holonType);

            else if (this.ProviderKey != null)
                result = LoadHolonsForParent(ProviderKey, holonType);
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon, bool mapBaseHolonProperties = true)
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<IHolon> result = await _holonManager.SaveHolonAsync(savingHolon);
            HandleSaveHolonResult(savingHolon, "SaveHolonAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual OASISResult<IHolon> SaveHolon(IHolon savingHolon, bool mapBaseHolonProperties = true)
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<IHolon> result = _holonManager.SaveHolon(savingHolon);
            HandleSaveHolonResult(savingHolon, "SaveHolon", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon savingHolon, bool mapBaseHolonProperties = true) where T : IHolon, new()
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<T> result = await _holonManager.SaveHolonAsync<T>(savingHolon);
            HandleSaveHolonResult(savingHolon, "SaveHolonAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual OASISResult<T> SaveHolon<T>(IHolon savingHolon, bool mapBaseHolonProperties = true) where T : IHolon, new()
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<T> result = _holonManager.SaveHolon<T>(savingHolon);
            HandleSaveHolonResult(savingHolon, "SaveHolon", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true)
        {
            if (savingHolons == null)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            savingHolons = RemoveCelesialBodies(savingHolons);
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.SaveHolonsAsync(savingHolons);
            HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true)
        {
            if (savingHolons == null)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            savingHolons = RemoveCelesialBodies(savingHolons);
            OASISResult<IEnumerable<IHolon>> result = _holonManager.SaveHolons(savingHolons);
            HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        //TODO: Add generic versions tommorw! :)

        public virtual async Task<OASISResult<IZome>> SaveAsync(bool saveChildren = true, bool continueOnError = true)
        {
            OASISResult<IZome> zomeResult = new OASISResult<IZome>((IZome)this);

            //First save the zome.
            OASISResult<IHolon> holonResult = await _holonManager.SaveHolonAsync(this);

            if (holonResult.IsError)
            {
                zomeResult.IsError = true;
                zomeResult.Message = holonResult.Message;
            }

            if (saveChildren && (!continueOnError && !zomeResult.IsError && holonResult.Result != null)
                || continueOnError && (zomeResult.IsError || holonResult.Result == null))
            {
                if (holonResult.Result != null)
                {
                    this.Id = holonResult.Result.Id;
                    this.ProviderKey = holonResult.Result.ProviderKey;
                    this.CreatedByAvatar = holonResult.Result.CreatedByAvatar;
                    this.CreatedByAvatarId = holonResult.Result.CreatedByAvatarId;
                    this.CreatedDate = holonResult.Result.CreatedDate;
                    this.ModifiedByAvatar = holonResult.Result.ModifiedByAvatar;
                    this.ModifiedByAvatarId = holonResult.Result.ModifiedByAvatarId;
                    this.ModifiedDate = holonResult.Result.ModifiedDate;
                    this.Children = holonResult.Result.Children;
                }

                ZomeHelper.SetParentIdsForZome(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the way it saves (batches, etc), which would be quicker than making multiple calls...)
                OASISResult<IEnumerable<IHolon>> holonsResult = await _holonManager.SaveHolonsAsync(this.Holons);

                if (holonsResult.IsError)
                {
                    zomeResult.IsError = true;
                    zomeResult.Message = holonsResult.Message;
                }

                if ((continueOnError && (holonsResult.IsError || holonResult.Result == null))
                    || (!continueOnError && !holonResult.IsError && holonResult.Result != null))
                {
                    this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.

                    // Now we need to save the zome again so its child holons have their ids set.
                    // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentZomeIds.
                    // But loading the zome with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).
                    holonResult = await _holonManager.SaveHolonAsync(this); //TODO: Not sure if we need to do this?

                    if (holonsResult.IsError)
                    {
                        zomeResult.IsError = true;
                        zomeResult.Message = holonsResult.Message;
                    }
                    //else
                    //{
                    //    //this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.

                    //    // Now we need to save the zome again so its child holons have their ids set.
                    //    // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentZomeIds.
                    //    // But loading the zome with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).
                    //    holonResult = _holonManager.SaveHolon(this);

                    //    if (holonsResult.IsError)
                    //    {
                    //        zomeResult.IsError = true;
                    //        zomeResult.Message = holonsResult.Message;
                    //    }
                    //}
                }
            }

            OnSaved?.Invoke(this, new ZomeSavedEventArgs() { Result = zomeResult });
            return zomeResult;
        }

        public virtual OASISResult<IZome> Save(bool saveChildren = true, bool continueOnError = true)
        {
            OASISResult<IZome> zomeResult = new OASISResult<IZome>((IZome)this);

            //First save the zome.
            OASISResult<IHolon> holonResult = _holonManager.SaveHolon(this);
            
            if (holonResult.IsError)
            {
                zomeResult.IsError = true;
                zomeResult.Message = holonResult.Message;
            }

            if (saveChildren && (!continueOnError && !zomeResult.IsError && holonResult.Result != null) 
                || continueOnError && (zomeResult.IsError || holonResult.Result == null))
            {
                if (holonResult.Result != null)
                {
                    this.Id = holonResult.Result.Id;
                    this.ProviderKey = holonResult.Result.ProviderKey;
                    this.CreatedByAvatar = holonResult.Result.CreatedByAvatar;
                    this.CreatedByAvatarId = holonResult.Result.CreatedByAvatarId;
                    this.CreatedDate = holonResult.Result.CreatedDate;
                    this.ModifiedByAvatar = holonResult.Result.ModifiedByAvatar;
                    this.ModifiedByAvatarId = holonResult.Result.ModifiedByAvatarId;
                    this.ModifiedDate = holonResult.Result.ModifiedDate;
                    this.Children = holonResult.Result.Children;
                }

                ZomeHelper.SetParentIdsForZome(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the the way it saves (batches, etc), which would be quicker than making multiple calls...)
                OASISResult<IEnumerable<IHolon>> holonsResult = _holonManager.SaveHolons(this.Holons);

                if (holonsResult.IsError)
                {
                    zomeResult.IsError = true;
                    zomeResult.Message = holonsResult.Message;
                }
                
                if ((continueOnError && (holonsResult.IsError || holonResult.Result == null)) 
                    || (!continueOnError && !holonResult.IsError && holonResult.Result != null))
                {
                    this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.

                    // Now we need to save the zome again so its child holons have their ids set.
                    // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentZomeIds.
                    // But loading the zome with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).
                    holonResult = _holonManager.SaveHolon(this); //TODO: Not sure if we need to do this?

                    if (holonsResult.IsError)
                    {
                        zomeResult.IsError = true;
                        zomeResult.Message = holonsResult.Message;
                    }
                    //else
                    //{
                    //    this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.

                    //    // Now we need to save the zome again so its child holons have their ids set.
                    //    // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentZomeIds.
                    //    // But loading the zome with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).
                    //    holonResult = _holonManager.SaveHolon(this);

                    //    if (holonsResult.IsError)
                    //    {
                    //        zomeResult.IsError = true;
                    //        zomeResult.Message = holonsResult.Message;
                    //    }
                    //}
                }
            }

            OnSaved?.Invoke(this, new ZomeSavedEventArgs() { Result = zomeResult });
            return zomeResult;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> AddHolonAsync(IHolon holon)
        {
            this.Holons.Add(holon);
            OASISResult<IEnumerable<IHolon>> result = await SaveHolonsAsync(this.Holons);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in AddHolonAsync method with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> AddHolon(IHolon holon)
        {
            this.Holons.Add(holon);
            OASISResult<IEnumerable<IHolon>> result = SaveHolons(this.Holons);
            OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> RemoveHolonAsync(IHolon holon)
        {
            this.Holons.Remove(holon);
            OASISResult<IEnumerable<IHolon>> result = await SaveHolonsAsync(this.Holons);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> RemoveHolon(IHolon holon)
        {
            this.Holons.Remove(holon);
            OASISResult<IEnumerable<IHolon>> result = SaveHolons(this.Holons);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            return result;
        }

        private string GetCurrentProviderKey(Dictionary<ProviderType, string> providerKey)
        {
            if (ProviderKey.ContainsKey(ProviderManager.CurrentStorageProviderType.Value) && !string.IsNullOrEmpty(ProviderKey[ProviderManager.CurrentStorageProviderType.Value]))
                return providerKey[ProviderManager.CurrentStorageProviderType.Value];
            else
                throw new Exception(string.Concat("ProviderKey not found for CurrentStorageProviderType ", ProviderManager.CurrentStorageProviderType.Name));

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

            _parentOmiverse[holon.Id] = holon.ParentOmiverse;
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
            _parentZome[holon.Id] = holon.ParentZome;
            _parentHolon[holon.Id] = holon.ParentHolon;

            holon.ParentOmiverse = null;
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
            holon.ParentZome = null;
            holon.ParentHolon = null;

            return holon;
        }

        private IHolon RestoreCelesialBodies(IHolon originalHolon) 
        {
            originalHolon.IsNewHolon = false;
            originalHolon.ParentOmiverse = _parentOmiverse[originalHolon.Id];
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
                OASISResultHolonToHolonHelper<T, IHolon>.CopyResult(result, holonResult);
                OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = holonResult });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });
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
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });
        }

        private void HandleSaveHolonsResult(IEnumerable<IHolon> savingHolons, string callingMethodName, ref OASISResult<IEnumerable<IHolon>> result, bool mapBaseHolonProperties = true)
        {
            if (!result.IsError && result.Result != null)
            {
                if (mapBaseHolonProperties)
                    result.Result = Mapper.MapBaseHolonProperties(savingHolons, result.Result);

                result.Result = RestoreCelesialBodies(savingHolons);
                OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in SaveHolonsAsync method. Error Details: ", result.Message), ErrorDetails = result.Exception });
        }
    }
}