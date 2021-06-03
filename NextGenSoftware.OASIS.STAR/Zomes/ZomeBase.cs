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
        public List<Holon> _holons = new List<Holon>();

        public List<Holon> Holons
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
                ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);
            else
            {
                _holonManager = new HolonManager(result.Result);
                OnInitialized?.Invoke(this, new System.EventArgs());
            }
        }

        public virtual async Task<IHolon> LoadHolonAsync(Guid id)
        {
            IHolon holon = await _holonManager.LoadHolonAsync(id);
            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Holon = holon });
            return holon;
        }

        public virtual IHolon LoadHolon(Guid id)
        {
            IHolon holon = _holonManager.LoadHolon(id);
            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Holon = holon });
            return holon;
        }


        public virtual async Task<IHolon> LoadHolonAsync(Dictionary<ProviderType, string> providerKey)
        {
            IHolon holon = await _holonManager.LoadHolonAsync(GetCurrentProviderKey(providerKey));
            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Holon = holon });
            return holon;
        }

        public virtual IHolon LoadHolon(Dictionary<ProviderType, string> providerKey)
        {
            IHolon holon = _holonManager.LoadHolon(GetCurrentProviderKey(providerKey));
            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Holon = holon });
            return holon;
        }

        public virtual async Task<IEnumerable<IHolon>> LoadHolonsAsync(Guid id, HolonType type = HolonType.All)
        {
            IEnumerable<IHolon> holons = await _holonManager.LoadHolonsForParentAsync(id, type);
            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Holons = (List<IHolon>)holons });
            return holons;
        }

        public virtual IEnumerable<IHolon> LoadHolons(Guid id, HolonType type = HolonType.All)
        {
            IEnumerable<IHolon> holons = _holonManager.LoadHolonsForParent(id, type);
            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Holons = (List<IHolon>)holons });
            return holons;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsAsync(Dictionary<ProviderType, string> providerKey, HolonType type = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadHolonsForParentAsync(GetCurrentProviderKey(providerKey), type);
            
            if (!result.IsError)
                OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Holons = (List<IHolon>)result.Result });

            return result;
        }

        public virtual IEnumerable<IHolon> LoadHolons(Dictionary<ProviderType, string> providerKey, HolonType type = HolonType.All)
        {
            IEnumerable<IHolon> holons = _holonManager.LoadHolonsForParent(GetCurrentProviderKey(providerKey), type);
            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Holons = (List<IHolon>)holons });
            return holons;
        }

        public virtual async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon)
        {
            OASISResult<IHolon> result = await _holonManager.SaveHolonAsync(savingHolon);
            OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IHolon> SaveHolon(IHolon savingHolon)
        {
            OASISResult<IHolon> result = _holonManager.SaveHolon(savingHolon);
            OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.SaveHolonsAsync(savingHolons);
            OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.SaveHolons(savingHolons);
            OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IZome>> SaveAsync()
        {
            OASISResult<IZome> zomeResult = new OASISResult<IZome>((IZome)this);

            //First save the zome.
            OASISResult<IHolon> holonResult = await _holonManager.SaveHolonAsync(this);

            if (!zomeResult.IsError)
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

                ZomeHelper.SetParentIdsForZome(this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the the way it saves (batches, etc), which would be quicker than making multiple calls...)
                OASISResult<IEnumerable<IHolon>> holonsResult = await _holonManager.SaveHolonsAsync(this.Holons);

                if (holonsResult.IsError)
                {
                    zomeResult.IsError = true;
                    zomeResult.Message = holonsResult.Message;
                }
                else
                {
                    //TODO Causes cast error - fix tomorrow! :)
                    this.Holons = (List<Holon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.

                    // Now we need to save the zome again so its child holons have their ids set.
                    // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentZomeIds.
                    // But loading the zome with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).
                    holonResult = await _holonManager.SaveHolonAsync(this);

                    if (holonsResult.IsError)
                    {
                        zomeResult.IsError = true;
                        zomeResult.Message = holonsResult.Message;
                    }
                }
            }
            else
            {
                zomeResult.IsError = true;
                zomeResult.Message = holonResult.Message;
            }

            OnSaved?.Invoke(this, new ZomeSavedEventArgs() { Result = zomeResult });
            return zomeResult;
        }

        public virtual OASISResult<IZome> Save()
        {
            OASISResult<IZome> zomeResult = new OASISResult<IZome>((IZome)this);

            //First save the zome.
            OASISResult<IHolon> holonResult = _holonManager.SaveHolon(this);

            if (!zomeResult.IsError)
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

                ZomeHelper.SetParentIdsForZome(this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the the way it saves (batches, etc), which would be quicker than making multiple calls...)
                OASISResult<IEnumerable<IHolon>> holonsResult = _holonManager.SaveHolons(this.Holons);

                if (holonsResult.IsError)
                {
                    zomeResult.IsError = true;
                    zomeResult.Message = holonsResult.Message;
                }
                else
                {
                    this.Holons = (List<Holon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.

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
            else
            {
                zomeResult.IsError = true;
                zomeResult.Message = holonResult.Message;
            }

            OnSaved?.Invoke(this, new ZomeSavedEventArgs() { Result = zomeResult });
            return zomeResult;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> AddHolonAsync(IHolon holon)
        {
            this.Holons.Add((Holon)holon);
            OASISResult<IEnumerable<IHolon>> result = await SaveHolonsAsync(this.Holons);
            OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> AddHolon(IHolon holon)
        {
            this.Holons.Add((Holon)holon);
            OASISResult<IEnumerable<IHolon>> result = SaveHolons(this.Holons);
            OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> RemoveHolonAsync(IHolon holon)
        {
            this.Holons.Remove((Holon)holon);
            OASISResult<IEnumerable<IHolon>> result = await SaveHolonsAsync(this.Holons);
            OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> RemoveHolon(IHolon holon)
        {
            this.Holons.Remove((Holon)holon);
            OASISResult<IEnumerable<IHolon>> result = SaveHolons(this.Holons);
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
    }
}
