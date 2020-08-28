using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public interface ICelestialBodyBase : OASIS.API.Core.ICelestialBodyBase
    {
        HoloNETClientBase HoloNETClient { get; }

        event CelestialBodyBase.DataReceived OnDataReceived;
        event CelestialBodyBase.Disconnected OnDisconnected;
        event CelestialBodyBase.HolonLoaded OnHolonLoaded;
        event CelestialBodyBase.HolonSaved OnHolonSaved;
        event CelestialBodyBase.Initialized OnInitialized;
        event CelestialBodyBase.ZomeError OnZomeError;

        Task Initialize(Guid id, HoloNETClientBase holoNETClient);
        Task Initialize(Guid id, string holochainConductorURI, HoloNETClientType type);
        Task Initialize(HoloNETClientBase holoNETClient);
        Task Initialize(string holochainConductorURI, HoloNETClientType type);
      
       // Task<IHolon> LoadHolonAsync(string holonName, string hcEntryAddressHash);
    }
}