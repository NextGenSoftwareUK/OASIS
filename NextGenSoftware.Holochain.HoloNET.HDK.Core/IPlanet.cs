using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public interface IPlanet
    {
        HoloNETClientBase HoloNETClient { get; }

        event PlanetBase.DataReceived OnDataReceived;
        event PlanetBase.Disconnected OnDisconnected;
        event PlanetBase.HolonLoaded OnHolonLoaded;
        event PlanetBase.HolonSaved OnHolonSaved;
        event PlanetBase.Initialized OnInitialized;
        event PlanetBase.ZomeError OnZomeError;

        void Dim();
        void Emit();
        void Evolve();
        Task Initialize(Guid id, HoloNETClientBase holoNETClient);
        Task Initialize(Guid id, string holochainConductorURI, PlanetBase.HoloNETClientType type);
        Task Initialize(HoloNETClientBase holoNETClient);
        Task Initialize(string holochainConductorURI, PlanetBase.HoloNETClientType type);
        void Light();
        void Load();
        Task<IHolon> LoadHolonAsync(string holonName, string hcEntryAddressHash);
        void Love();
        void Mutate();
        void Radiate();
        void Reflect();
        bool Save();
        Task<IHolon> SaveHolonAsync(IHolon savingHolon);
        void Seed();
        void Shine();
        void Super();
        void Twinkle();
    }
}