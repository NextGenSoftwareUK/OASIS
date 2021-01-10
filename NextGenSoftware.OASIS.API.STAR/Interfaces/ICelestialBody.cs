using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.STAR
{
    public interface ICelestialBody : OASIS.API.Core.ICelestialBody
    {
        CelestialBodyCore CelestialBodyCore { get; set; }
        GenesisType GenesisType { get; set; }
        HoloNETClientBase HoloNETClient { get; }
        string RustCelestialBodyType { get; set; }

        List<IZome> Zomes { get; set; }


        //TODO: Come back to this...
        /*
        event CelestialBody.DataReceived OnDataReceived;
        event CelestialBody.Disconnected OnDisconnected;
        //event CelestialBody.HolonLoaded OnHolonLoaded;
        //event CelestialBody.HolonSaved OnHolonSaved;
        //event CelestialBody.HolonsLoaded OnHolonsLoaded;
        //event CelestialBody.Initialized OnInitialized;
        //event CelestialBody.ZomeError OnZomeError;
        //event CelestialBody.ZomesLoaded OnZomesLoaded;
        */

        /*
        void Dim();
        void Emit();
        void Evolve();
        CoronalEjection Flare();
        void Seed();
        void Shine();
        void Super();
        void Twinkle();
        void Love();
        void Mutate();
        void Radiate();
        void Reflect();
        */
        //Task Initialize(Guid id, HoloNETClientBase holoNETClient);
       // Task Initialize(Guid id, string holochainConductorURI, HoloNETClientType type);
        Task Initialize(HoloNETClientBase holoNETClient);
        Task Initialize(string holochainConductorURI, HoloNETClientType type);
     //   void LoadAll();
       // Task<IHolon> LoadHolonAsync(string rustHolonType, string hcEntryAddressHash);
        //void LoadHolons();
       // void LoadZomes();
      
        Task<bool> Save();
       // Task<IHolon> SaveHolonAsync(string rustHolonType, IHolon savingHolon);
       
    }
}