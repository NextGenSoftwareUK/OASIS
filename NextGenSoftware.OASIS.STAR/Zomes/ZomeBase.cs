using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private IOmiverse _parentOmiverse = null;
        private IDimension _parentDimension = null;
        private IMultiverse _parentMultiverse = null;
        private IUniverse _parentUniverse = null;
        private IGalaxyCluster _parentGalaxyCluster = null;
        private IGalaxy _parentGalaxy = null;
        private ISolarSystem _parentSolarSystem = null;
        private IGreatGrandSuperStar _parentGreatGrandSuperStar = null;
        private IGrandSuperStar _parentGrandSuperStar = null;
        private ISuperStar _parentSuperStar = null;
        private IStar _parentStar = null;
        private IPlanet _parentPlanet = null;
        private IMoon _parentMoon = null;
        private IZome _parentZome = null;
        private IHolon _parentHolon = null;
        private ICelestialBodyCore _core = null;

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

            else if (this.ProviderKey != null)
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

        public virtual async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon)
        {
            RemoveCelesialBodies(savingHolon);
            OASISResult<IHolon> result = await _holonManager.SaveHolonAsync(savingHolon);
            HandleSaveHolonResult(savingHolon, "SaveHolonAsync", ref result);
            return result;
        }

        public virtual OASISResult<IHolon> SaveHolon(IHolon savingHolon)
        {
            RemoveCelesialBodies(savingHolon);
            OASISResult<IHolon> result = _holonManager.SaveHolon(savingHolon);
            HandleSaveHolonResult(savingHolon, "SaveHolon", ref result);
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.SaveHolonsAsync(savingHolons);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in SaveHolonsAsync method. Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.SaveHolons(savingHolons);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in SaveHolons method. Error Details: ", result.Message), ErrorDetails = result.Exception });

            OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
            return result;
        }

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
                this.Id = holonResult.Result.Id;
                this.ProviderKey = holonResult.Result.ProviderKey;
                this.CreatedByAvatar = holonResult.Result.CreatedByAvatar;
                this.CreatedByAvatarId = holonResult.Result.CreatedByAvatarId;
                this.CreatedDate = holonResult.Result.CreatedDate;
                this.ModifiedByAvatar = holonResult.Result.ModifiedByAvatar;
                this.ModifiedByAvatarId = holonResult.Result.ModifiedByAvatarId;
                this.ModifiedDate = holonResult.Result.ModifiedDate;
                this.Children = holonResult.Result.Children;

                ZomeHelper.SetParentIdsForZome(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the the way it saves (batches, etc), which would be quicker than making multiple calls...)
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
                    holonResult = await _holonManager.SaveHolonAsync(this);

                    if (holonsResult.IsError)
                    {
                        zomeResult.IsError = true;
                        zomeResult.Message = holonsResult.Message;
                    }
                    else
                    {
                        this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.

                        // Now we need to save the zome again so its child holons have their ids set.
                        // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentZomeIds.
                        // But loading the zome with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).
                        holonResult = _holonManager.SaveHolon(this);

                        if (holonsResult.IsError)
                        {
                            zomeResult.IsError = true;
                            zomeResult.Message = holonsResult.Message;
                        }
                    }
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
                this.Id = holonResult.Result.Id;
                this.ProviderKey = holonResult.Result.ProviderKey;
                this.CreatedByAvatar = holonResult.Result.CreatedByAvatar;
                this.CreatedByAvatarId = holonResult.Result.CreatedByAvatarId;
                this.CreatedDate = holonResult.Result.CreatedDate;
                this.ModifiedByAvatar = holonResult.Result.ModifiedByAvatar;
                this.ModifiedByAvatarId = holonResult.Result.ModifiedByAvatarId;
                this.ModifiedDate = holonResult.Result.ModifiedDate;
                this.Children = holonResult.Result.Children;

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
                    holonResult = _holonManager.SaveHolon(this);

                    if (holonsResult.IsError)
                    {
                        zomeResult.IsError = true;
                        zomeResult.Message = holonsResult.Message;
                    }
                    else
                    {
                        this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.

                        // Now we need to save the zome again so its child holons have their ids set.
                        // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentZomeIds.
                        // But loading the zome with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).
                        holonResult = _holonManager.SaveHolon(this);

                        if (holonsResult.IsError)
                        {
                            zomeResult.IsError = true;
                            zomeResult.Message = holonsResult.Message;
                        }
                    }
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

        private void RemoveCelesialBodies(IHolon holon)
        {
            ICelestialBody celestialBody = holon as ICelestialBody;

            if (celestialBody != null)
            {
                _core = celestialBody.CelestialBodyCore;
                celestialBody.CelestialBodyCore = null;
            }

            _parentOmiverse = holon.ParentOmiverse;
            _parentDimension = holon.ParentDimension;
            _parentMultiverse = holon.ParentMultiverse;
            _parentUniverse = holon.ParentUniverse;
            _parentGalaxyCluster = holon.ParentGalaxyCluster;
            _parentGalaxy = holon.ParentGalaxy;
            _parentSolarSystem = holon.ParentSolarSystem;
            _parentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            _parentGrandSuperStar = holon.ParentGrandSuperStar;
            _parentSuperStar = holon.ParentSuperStar;
            _parentStar = holon.ParentStar;
            _parentPlanet = holon.ParentPlanet;
            _parentMoon = holon.ParentMoon;
            _parentZome = holon.ParentZome;
            _parentHolon = holon.ParentHolon;

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
        }

        private IHolon RestoreCelesialBodies(IHolon originalHolon, IHolon savedHolon)
        {
            ICelestialBody celestialBody = originalHolon as ICelestialBody;

            if (celestialBody != null)
                celestialBody.CelestialBodyCore = _core;



            celestialBody.ParentOmiverse = _parentOmiverse;
            celestialBody.ParentDimension = _parentDimension;
            celestialBody.ParentMultiverse = _parentMultiverse;
            celestialBody.ParentUniverse = _parentUniverse;
            celestialBody.ParentGalaxyCluster = _parentGalaxyCluster;
            celestialBody.ParentGalaxy = _parentGalaxy;
            celestialBody.ParentSolarSystem = _parentSolarSystem;
            celestialBody.ParentGreatGrandSuperStar = _parentGreatGrandSuperStar;
            celestialBody.ParentGrandSuperStar = _parentGrandSuperStar;
            celestialBody.ParentSuperStar = _parentSuperStar;
            celestialBody.ParentStar = _parentStar;
            celestialBody.ParentPlanet = _parentPlanet;
            celestialBody.ParentMoon = _parentMoon;
            celestialBody.ParentZome = _parentZome;
            celestialBody.ParentHolon = _parentHolon;

            return celestialBody;
        }

        private void HandleSaveHolonResult(IHolon savingHolon, string callingMethodName, ref OASISResult<IHolon> result)
        {
            if (!result.IsError && result.Result != null)
            {
                result.Result = RestoreCelesialBodies(savingHolon, result.Result);
                OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), ErrorDetails = result.Exception });
        }
    }
}