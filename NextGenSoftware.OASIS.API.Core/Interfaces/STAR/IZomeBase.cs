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

        Task<OASISResult<IHolon>> LoadHolonAsync(Guid id);
        OASISResult<IHolon> LoadHolon(Guid id);
        Task<OASISResult<IHolon>> LoadHolonAsync(Dictionary<ProviderType, string> providerKey);
        OASISResult<IHolon> LoadHolon(Dictionary<ProviderType, string> providerKey);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Dictionary<ProviderType, string> providerKey, HolonType holonType = HolonType.All);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Dictionary<ProviderType, string> providerKey, HolonType holonType = HolonType.All);
        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon);
        OASISResult<IHolon> SaveHolon(IHolon savingHolon);
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons);
        OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons);
        Task<OASISResult<IZome>> SaveAsync();
        OASISResult<IZome> Save();
        Task<OASISResult<IEnumerable<IHolon>>> AddHolonAsync(IHolon holon);
        OASISResult<IEnumerable<IHolon>> AddHolon(IHolon holon);
        Task<OASISResult<IEnumerable<IHolon>>> RemoveHolonAsync(IHolon holon);
        OASISResult<IEnumerable<IHolon>> RemoveHolon(IHolon holon);
    }
}