using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using static NextGenSoftware.OASIS.API.Core.Events.Events;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IZomeBase : IHolon
    {
        List<Holon> Holons { get; set; }

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

        Task<IHolon> LoadHolonAsync(Guid id);
        IHolon LoadHolon(Guid id);
        Task<IHolon> LoadHolonAsync(Dictionary<ProviderType, string> providerKey);
        IHolon LoadHolon(Dictionary<ProviderType, string> providerKey);
        Task<IEnumerable<IHolon>> LoadHolonsAsync(Guid id, HolonType type = HolonType.All);
        IEnumerable<IHolon> LoadHolons(Guid id, HolonType type = HolonType.All);
        OASISResult<Task<IEnumerable<IHolon>>> LoadHolonsAsync(Dictionary<ProviderType, string> providerKey, HolonType type = HolonType.All);
        IEnumerable<IHolon> LoadHolons(Dictionary<ProviderType, string> providerKey, HolonType type = HolonType.All);
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