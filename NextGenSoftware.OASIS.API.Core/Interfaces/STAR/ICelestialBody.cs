using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.OASIS.API.Core.Events.Events;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBody : IHolon
    {
        // event DataReceived OnDataReceived;
       // event Disconnected OnDisconnected;
        event HolonLoaded OnHolonLoaded;
        event HolonSaved OnHolonSaved;
        event HolonsLoaded OnHolonsLoaded;
        event Initialized OnInitialized;
        event ZomeError OnZomeError;
        event ZomesLoaded OnZomesLoaded;

        ICelestialBodyCore CelestialBodyCore { get; set; }
        GenesisType GenesisType { get; set; }
        bool IsInitialized { get; }
        Task<OASISResult<ICelestialBody>> SaveAsync();
        Task<OASISResult<List<IZome>>> LoadZomesAsync();
        OASISResult<List<IZome>> LoadZomes();
        Task LoadCelestialBodyAsync();
        void LoadCelestialBody();
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