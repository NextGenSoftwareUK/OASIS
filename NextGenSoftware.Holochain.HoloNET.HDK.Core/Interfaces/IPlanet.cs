using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public interface IPlanet : OASIS.API.Core.IPlanet
    {
        HoloNETClientBase HoloNETClient { get; }

        event PlanetBase.DataReceived OnDataReceived;
        event PlanetBase.Disconnected OnDisconnected;
        event PlanetBase.HolonLoaded OnHolonLoaded;
        event PlanetBase.HolonSaved OnHolonSaved;
        event PlanetBase.Initialized OnInitialized;
        event PlanetBase.ZomeError OnZomeError;

        Task Initialize(Guid id, HoloNETClientBase holoNETClient);
        Task Initialize(Guid id, string holochainConductorURI, HoloNETClientType type);
        Task Initialize(HoloNETClientBase holoNETClient);
        Task Initialize(string holochainConductorURI, HoloNETClientType type);
      
       // Task<IHolon> LoadHolonAsync(string holonName, string hcEntryAddressHash);
    }
}