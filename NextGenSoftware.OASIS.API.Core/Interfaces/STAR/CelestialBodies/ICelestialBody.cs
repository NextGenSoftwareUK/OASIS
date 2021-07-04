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

        public SpaceQuadrantType SpaceQuadrant { get; set; }
        public int SpaceSector { get; set; }
        public float SuperGalacticLatitute { get; set; }
        public float SuperGalacticLongitute { get; set; }
        public float GalacticLatitute { get; set; }
        public float GalacticLongitute { get; set; }
        public float HorizontalLatitute { get; set; }
        public float HorizontalLongitute { get; set; }
        public float EquatorialLatitute { get; set; }
        public float EquatorialLongitute { get; set; }
        public float EclipticLatitute { get; set; }
        public float EclipticLongitute { get; set; }
        public int Size { get; set; }
        public int Radius { get; set; }
        public int Age { get; set; }
        public int Mass { get; set; }
        public int Temperature { get; set; }
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