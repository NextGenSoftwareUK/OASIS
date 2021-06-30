using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.OASIS.API.Core.Events.Events;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBody : ICelestialHolon
    {
        // event DataReceived OnDataReceived;
       // event Disconnected OnDisconnected;
        event HolonLoaded OnHolonLoaded;
        event HolonSaved OnHolonSaved;
        event HolonsLoaded OnHolonsLoaded;
        event Initialized OnInitialized;
        event ZomeError OnZomeError;
        event ZomesLoaded OnZomesLoaded;

        public int Size { get; set; }
        public int Mass { get; set; }
        public int Weight { get; set; }
        public int GravitaionalPull { get; set; }

        ICelestialBodyCore CelestialBodyCore { get; set; }
        GenesisType GenesisType { get; set; }
        bool IsInitialized { get; }
        Task<OASISResult<ICelestialBody>> SaveAsync();
        OASISResult<ICelestialBody> Save();
        Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync();
        OASISResult<IEnumerable<IZome>> LoadZomes();
        Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync();
        OASISResult<ICelestialBody> LoadCelestialBody();
        Task InitializeAsync();
        void Initialize();
        void Dim();
        void Emit();
        void Evolve();
        CoronalEjection Flare();
        void Love();
        void Mutate();
        void Radiate();
        void Reflect();
        void Seed();
        void Shine();
        void Super();
        void Twinkle();
    }
}