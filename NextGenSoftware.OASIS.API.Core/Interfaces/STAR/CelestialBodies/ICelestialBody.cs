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
        event HolonLoaded OnHolonLoaded;
        event HolonsLoaded OnHolonsLoaded;
        event HolonSaved OnHolonSaved;
        event ZomeError OnZomeError;
        event ZomesLoaded OnZomesLoaded;
        // event DataReceived OnDataReceived;
        // event Disconnected OnDisconnected;

        public int Mass { get; set; }
        public int Weight { get; set; }
        public int GravitaionalPull { get; set; }
        public int OrbitPositionFromParentStar { get; set; }
        //public int OrbitPositionFromParentSuperStar { get; set; } //Only applies to SolarSystems. //TODO: Maybe better to make SolarSystem.ParentStar point to the SuperStar it orbits rather than the Star at the centre of it?
        public int CurrentOrbitAngleOfParentStar { get; set; } //Angle between 0 and 360 degrees of how far around the orbit it it of its parent star.
        public int DistanceFromParentStarInMetres { get; set; }
        public int RotationSpeed { get; set; }
        public int TiltAngle { get; set; }
        public int NumberRegisteredAvatars { get; set; }
        public int NunmerActiveAvatars { get; set; }

        ICelestialBodyCore CelestialBodyCore { get; set; }
        //GenesisType GenesisType { get; set; }
        //bool IsInitialized { get; }
        Task<OASISResult<ICelestialBody>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialBody, new();
        OASISResult<ICelestialBody> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialBody, new();
        Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool continueOnError = true);
        OASISResult<ICelestialBody> Save(bool saveChildren = true, bool continueOnError = true);
        Task<OASISResult<ICelestialBody>> LoadAsync(bool loadZomes = true, bool continueOnError = true);
        OASISResult<ICelestialBody> Load(bool loadZomes = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync();
        OASISResult<IEnumerable<IZome>> LoadZomes();
        //Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync();
        //OASISResult<ICelestialBody> LoadCelestialBody();
       // Task<OASISResult<IHolon>> LoadCelestialBodyAsync();
        //OASISResult<IHolon> LoadCelestialBody();
        //Task InitializeAsync();
        //void Initialize();
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