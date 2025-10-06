using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.STAR.Holons;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{

    //public class Asteroid : Holon, IAsteroid
    public class Asteroid //: CelestialHolon//, IAsteroid
    {
        //public SpaceQuadrantType SpaceQuadrant { get; set; }
        //public int SpaceSector { get; set; }
        //public float SuperGalacticLatitute { get; set; }
        //public float SuperGalacticLongitute { get; set; }
        //public float GalacticLatitute { get; set; }
        //public float GalacticLongitute { get; set; }
        //public float HorizontalLatitute { get; set; }
        //public float HorizontalLongitute { get; set; }
        //public float EquatorialLatitute { get; set; }
        //public float EquatorialLongitute { get; set; }
        //public float EclipticLatitute { get; set; }
        //public float EclipticLongitute { get; set; }
        //public int Size { get; set; }
        //public int Radius { get; set; }
        //public int Age { get; set; }
        //public int Temperature { get; set; }
        //public GenesisType GenesisType { get; set; }
        //public bool IsInitialized { get; }

        public long Mass { get; set; }
        public long Weight { get; set; }
        public long GravitaionalPull { get; set; }
        public int OrbitPositionFromParentStar { get; set; }
        public int CurrentOrbitAngleOfParentStar { get; set; } //Angle between 0 and 360 degrees of how far around the orbit it it of its parent star.
        public long DistanceFromParentStarInMetres { get; set; }
        public long RotationSpeed { get; set; }
        public int TiltAngle { get; set; }
        public int NumberRegisteredAvatars { get; set; }
        public int NunmerActiveAvatars { get; set; }

        public ICelestialBodyCore CelestialBodyCore { get; set; }
        public List<IMoon> Moons { get; set; } = new List<IMoon>();
        public int NumberActiveAvatars { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //public Asteroid() : base(HolonType.Asteroid) { } 

        public event HolonLoaded OnHolonLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonsLoaded OnHolonsLoaded;
        public event Initialized OnInitialized;
        public event ZomeError OnZomeError;
        public event ZomesLoaded OnZomesLoaded;
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyError OnCelestialBodyError;
        public event ZomeLoaded OnZomeLoaded;
        public event ZomeSaved OnZomeSaved;
        public event ZomesSaved OnZomesSaved;
        public event ZomesError OnZomesError;
        public event HolonError OnHolonError;
        public event HolonsSaved OnHolonsSaved;
        public event HolonsError OnHolonsError;

        public Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ICelestialBody> Save(bool saveChildren = true, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IHolon>> LoadCelestialBodyAsync()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IHolon> LoadCelestialBody()
        {
            throw new NotImplementedException();
        }

        public Task InitializeAsync()
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Dim()
        {
            throw new NotImplementedException();
        }

        public void Emit()
        {
            throw new NotImplementedException();
        }

        public void Evolve()
        {
            throw new NotImplementedException();
        }

        public CoronalEjection Flare()
        {
            throw new NotImplementedException();
        }

        public void Love()
        {
            throw new NotImplementedException();
        }

        public void Mutate()
        {
            throw new NotImplementedException();
        }

        public void Radiate()
        {
            throw new NotImplementedException();
        }

        public void Reflect()
        {
            throw new NotImplementedException();
        }

        public void Seed()
        {
            throw new NotImplementedException();
        }

        public void Shine()
        {
            throw new NotImplementedException();
        }

        public void Super()
        {
            throw new NotImplementedException();
        }

        public void Twinkle()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<ICelestialBody>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<ICelestialBody> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ICelestialBody> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<ICelestialBody>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<ICelestialBody> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<T>> InitializeAsync<T>() where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<T> Initialize<T>() where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

       
        public Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ICelestialBody> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<ICelestialBody>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<ICelestialBody> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<T>>> LoadZomesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<T>> LoadZomes<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IEnumerable<T>>> SaveZomesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            throw new NotImplementedException();
        }

        public OASISResult<IEnumerable<T>> SaveZomes<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            throw new NotImplementedException();
        }

        //Task<OASISResult<ICelestialBody>> ICelestialBody.InitializeAsync()
        //{
        //    throw new NotImplementedException();
        //}

        //OASISResult<ICelestialBody> ICelestialBody.Initialize()
        //{
        //    throw new NotImplementedException();
        //}
    }
}