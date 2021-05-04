using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IZomeBase : IHolon
    {
        List<Holon> Holons { get; set; }

        //event ZomeBase.HolonLoaded OnHolonLoaded;
        //event ZomeBase.HolonSaved OnHolonSaved;
        //event ZomeBase.HolonsLoaded OnHolonsLoaded;
        //event ZomeBase.Initialized OnInitialized;
        //event ZomeBase.ZomeError OnZomeError;

        Task<OASISResult<IEnumerable<IHolon>>> AddHolon(IHolon holon);
        Task<OASISResult<IEnumerable<IHolon>>> RemoveHolon(IHolon holon);
        Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon);
        Task<IHolon> LoadHolonAsync(Dictionary<ProviderType, string> providerKey, HolonType type = HolonType.Holon);
        Task<IEnumerable<IHolon>> LoadHolonsAsync(Guid id, HolonType type = HolonType.Holon);
        Task<IEnumerable<IHolon>> LoadHolonsAsync(Dictionary<ProviderType, string> providerKey, HolonType type = HolonType.Holon);
        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon);
        Task<OASISResult<IZome>> Save();
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons);
    }
}