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

        Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, bool continueOnError = true); 
        OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IHolon>> LoadHolonAsync(Dictionary<ProviderType, string> providerKey, bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IHolon> LoadHolon(Dictionary<ProviderType, string> providerKey, bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Dictionary<ProviderType, string> providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Dictionary<ProviderType, string> providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true); //TODO: Do we need to pass in the Id or ProviderKey when it can be got from the zome/holon itself like this method does?
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<T>> SaveHolonAsync<T>(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, bool continueOnError = true) where T : IHolon, new();
        OASISResult<T> SaveHolon<T>(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, bool continueOnError = true) where T : IHolon, new();
        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IHolon> SaveHolon(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IZome>> SaveAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IZome> Save(bool saveChildren = true, bool recursive = true, bool continueOnError  = true);
        Task<OASISResult<IEnumerable<IHolon>>> AddHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IEnumerable<IHolon>> AddHolon(IHolon holon, bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IHolon>>> RemoveHolonAsync(IHolon holon);
        OASISResult<IEnumerable<IHolon>> RemoveHolon(IHolon holon);
    }
}