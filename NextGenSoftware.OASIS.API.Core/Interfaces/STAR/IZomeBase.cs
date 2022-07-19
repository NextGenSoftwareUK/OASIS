using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using static NextGenSoftware.OASIS.API.Core.Events.Events;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IZomeBase : IHolon
    {
        List<IHolon> Holons { get; set; }

        event Initialized OnInitialized;
        event HolonLoaded OnHolonLoaded;
        event HolonsLoaded OnHolonsLoaded;
        event HolonSaved OnHolonSaved;
        event HolonsSaved OnHolonsSaved;
        //event ZomesLoaded OnZomesLoaded;
        event ZomeSaved OnSaved;
        event HolonAdded OnHolonAdded;
        event HolonRemoved OnHolonRemoved;
        event ZomeError OnZomeError;

        Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<T>> LoadHolonAsync<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();        
        OASISResult<T> LoadHolon<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();
        Task<OASISResult<IHolon>> LoadHolonAsync(Dictionary<ProviderType, string> providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IHolon> LoadHolon(Dictionary<ProviderType, string> providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<T>> LoadHolonAsync<T>(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();
        OASISResult<T> LoadHolon<T>(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();

        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();
        OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();
        OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0); //TODO: Do we need to pass in the Id or ProviderUniqueStorageKey when it can be got from the zome/holon itself like this method does?
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();         //TODO: Do we need to pass in the Id or ProviderUniqueStorageKey when it can be got from the zome/holon itself like this method does?
        OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new();

        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        OASISResult<IHolon> SaveHolon(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        Task<OASISResult<T>> SaveHolonAsync<T>(T savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon, new();
        OASISResult<T> SaveHolon<T>(T savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon, new();
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        Task<OASISResult<IEnumerable<T>>> SaveHolonsAsync<T>(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon, new();
        OASISResult<IEnumerable<T>> SaveHolons<T>(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon, new();

        Task<OASISResult<IZome>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        OASISResult<IZome> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError  = true);
        
        Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        OASISResult<IHolon> AddHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        Task<OASISResult<T>> AddHolonAsync<T>(T holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon, new();
        OASISResult<T> AddHolon<T>(T holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon, new();

        Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, bool deleteHolon = false, bool softDelete = true);
        OASISResult<IHolon> RemoveHolon(IHolon holon, bool deleteHolon = false, bool softDelete = true);
        Task<OASISResult<T>> RemoveHolonAsync<T>(T holon, bool deleteHolon = false, bool softDelete = true) where T : IHolon, new();
        OASISResult<T> RemoveHolon<T>(T holon, bool deleteHolon = false, bool softDelete = true) where T : IHolon, new();
    }
}