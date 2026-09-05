using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{

    public class Comet : Holon//, IComet
    {
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
        public long Size { get; set; }
        public int Radius { get; set; }
        public long Age { get; set; }
        public long Mass { get; set; }
        public int Temperature { get; set; }
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
        public GenesisType GenesisType { get; set; }
        public bool IsInitialized { get; }
        public List<IMoon> Moons { get; set; } = new List<IMoon>();
        public int NumberActiveAvatars { get; set; }

        public Comet() { }

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
            => Task.FromResult(new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." });

        public OASISResult<ICelestialBody> Save(bool saveChildren = true, bool continueOnError = true)
            => new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." };

        public Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync()
            => Task.FromResult(new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() });

        public OASISResult<IEnumerable<IZome>> LoadZomes()
            => new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() };

        public Task<OASISResult<IHolon>> LoadCelestialBodyAsync()
            => Task.FromResult(new OASISResult<IHolon> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." });

        public OASISResult<IHolon> LoadCelestialBody()
            => new OASISResult<IHolon> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." };

        public Task InitializeAsync() => Task.CompletedTask;

        public void Initialize() { }

        public void Dim() { }
        public void Emit() { }
        public void Evolve() { }
        public CoronalEjection Flare() => new CoronalEjection();
        public void Love() { }
        public void Mutate() { }
        public void Radiate() { }
        public void Reflect() { }
        public void Seed() { }
        public void Shine() { }
        public void Super() { }
        public void Twinkle() { }

        public Task<OASISResult<ICelestialBody>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialBody, new()
            => Task.FromResult(new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." });

        public OASISResult<ICelestialBody> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialBody, new()
            => new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." };

        public Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            => Task.FromResult(new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." });

        public OASISResult<ICelestialBody> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            => new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." };

        public Task<OASISResult<IHolon>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => Task.FromResult(new OASISResult<IHolon> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." });

        public OASISResult<IHolon> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => new OASISResult<IHolon> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." };

        public Task<OASISResult<ICelestialBody>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : ICelestialBody, new()
            => Task.FromResult(new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." });

        public OASISResult<ICelestialBody> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : ICelestialBody, new()
            => new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." };

        public Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() });

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() };

        public Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            => Task.FromResult(new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() });

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
            => new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() };

        public Task<OASISResult<T>> InitializeAsync<T>() where T : ICelestialBody, new()
            => Task.FromResult(new OASISResult<T> { Result = new T() });

        public OASISResult<T> Initialize<T>() where T : ICelestialBody, new()
            => new OASISResult<T> { Result = new T() };

        public Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
            => Task.FromResult(new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." });

        public OASISResult<ICelestialBody> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
            => new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." };

        public Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
            => Task.FromResult(new OASISResult<T> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." });

        public OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
            => new OASISResult<T> { IsError = true, Message = "Use the OASIS Storage Provider to save celestial bodies." };

        public Task<OASISResult<ICelestialBody>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
            => Task.FromResult(new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." });

        public OASISResult<ICelestialBody> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
            => new OASISResult<ICelestialBody> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." };

        public Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
            => Task.FromResult(new OASISResult<T> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." });

        public OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
            => new OASISResult<T> { IsError = true, Message = "Use the OASIS Storage Provider to load celestial bodies." };

        public Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
            => Task.FromResult(new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() });

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
            => new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() };

        public Task<OASISResult<IEnumerable<T>>> LoadZomesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
            => Task.FromResult(new OASISResult<IEnumerable<T>> { Result = new List<T>() });

        public OASISResult<IEnumerable<T>> LoadZomes<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new()
            => new OASISResult<IEnumerable<T>> { Result = new List<T>() };

        public Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
            => Task.FromResult(new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() });

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
            => new OASISResult<IEnumerable<IZome>> { Result = new List<IZome>() };

        public Task<OASISResult<IEnumerable<T>>> SaveZomesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
            => Task.FromResult(new OASISResult<IEnumerable<T>> { Result = new List<T>() });

        public OASISResult<IEnumerable<T>> SaveZomes<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
            => new OASISResult<IEnumerable<T>> { Result = new List<T>() };

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