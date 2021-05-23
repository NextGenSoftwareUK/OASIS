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

        event HolonLoaded OnHolonLoaded;
        event HolonSaved OnHolonSaved;
        event HolonsLoaded OnHolonsLoaded;
        event Initialized OnInitialized;
        event ZomeError OnZomeError;

        Task<OASISResult<IEnumerable<IHolon>>> AddHolon(IHolon holon);
        IHolon LoadHolon(Guid id);
        Task<IHolon> LoadHolonAsync(Dictionary<ProviderType, string> providerKey);
        Task<IHolon> LoadHolonAsync(Guid id);
        IEnumerable<IHolon> LoadHolons(Dictionary<ProviderType, string> providerKey, HolonType type = HolonType.All);
        IEnumerable<IHolon> LoadHolons(Guid id, HolonType type = HolonType.All);
        Task<IEnumerable<IHolon>> LoadHolonsAsync(Dictionary<ProviderType, string> providerKey, HolonType type = HolonType.All);
        Task<IEnumerable<IHolon>> LoadHolonsAsync(Guid id, HolonType type = HolonType.All);
        Task<OASISResult<IEnumerable<IHolon>>> RemoveHolon(IHolon holon);
        Task<OASISResult<IZome>> Save();
        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon);
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons);
    }
}