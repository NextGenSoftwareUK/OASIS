using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.STAR
{
    public interface ICelestialBodyold : OASIS.API.Core.ICelestialBody
    {
        HoloNETClientBase HoloNETClient { get; }

        event CelestialBody.DataReceived OnDataReceived;
        event CelestialBody.Disconnected OnDisconnected;
        event CelestialBody.HolonLoaded OnHolonLoaded;
        event CelestialBody.HolonSaved OnHolonSaved;
        event CelestialBody.Initialized OnInitialized;
        event CelestialBody.ZomeError OnZomeError;

        Task Initialize(Guid id, HoloNETClientBase holoNETClient);
        Task Initialize(Guid id, string holochainConductorURI, HoloNETClientType type);
        Task Initialize(HoloNETClientBase holoNETClient);
        Task Initialize(string holochainConductorURI, HoloNETClientType type);
        Task<IHolon> LoadHolonAsync(string rustHolonType, string providerKey);
        Task<IHolon> SaveHolonAsync(string rustHolonType, IHolon savingHolon);

        // Task<IHolon> LoadHolonAsync(string holonName, string hcEntryAddressHash);
    }
}