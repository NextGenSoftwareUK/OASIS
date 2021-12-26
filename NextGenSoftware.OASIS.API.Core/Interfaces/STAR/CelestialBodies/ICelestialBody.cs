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
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyError OnCelestialBodyError;
        public event ZomeLoaded OnZomeLoaded;
        public event ZomeSaved OnZomeSaved;
        public event ZomeError OnZomeError;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomesSaved OnZomesSaved;
        public event ZomesError OnZomesError;
        public event HolonLoaded OnHolonLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonError OnHolonError;
        public event HolonsLoaded OnHolonsLoaded;
        public event HolonsSaved OnHolonsSaved;
        public event HolonsError OnHolonsError;
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
        //Task<OASISResult<ICelestialBody>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, bool continueOnError = true) where T : ICelestialBody, new();
        //OASISResult<ICelestialBody> Save<T>(bool saveChildren = true, bool recursive = true, bool continueOnError = true) where T : ICelestialBody, new();
        Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<ICelestialBody> Save(bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<ICelestialBody>> LoadAsync(bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<ICelestialBody> Load(bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true);
        OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, bool continueOnError = true);

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