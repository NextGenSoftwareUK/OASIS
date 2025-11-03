using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialBody : ICelestialHolon
    {
        ICelestialBodyCore CelestialBodyCore { get; set; }
        int CurrentOrbitAngleOfParentStar { get; set; }
        long Density { get; set; }
        long DistanceFromParentStarInMetres { get; set; }
        long GravitaionalPull { get; set; }
        Guid Id { get; set; }
        long Mass { get; set; }
        int NumberActiveAvatars { get; set; }
        int NumberRegisteredAvatars { get; set; }
        long OrbitPeriod { get; set; }
        int OrbitPositionFromParentStar { get; set; }
        Dictionary<ProviderType, string> ProviderUniqueStorageKey { get; set; }
        long RotationPeriod { get; set; }
        long RotationSpeed { get; set; }
        int TiltAngle { get; set; }
        long Weight { get; set; }

        event EventDelegates.CelestialBodyError OnCelestialBodyError;
        event EventDelegates.CelestialBodyLoaded OnCelestialBodyLoaded;
        event EventDelegates.CelestialBodySaved OnCelestialBodySaved;
        event EventDelegates.HolonError OnHolonError;
        event EventDelegates.HolonLoaded OnHolonLoaded;
        event EventDelegates.HolonSaved OnHolonSaved;
        event EventDelegates.HolonsError OnHolonsError;
        event EventDelegates.HolonsLoaded OnHolonsLoaded;
        event EventDelegates.HolonsSaved OnHolonsSaved;
        event EventDelegates.ZomeAdded OnZomeAdded;
        event EventDelegates.ZomeError OnZomeError;
        event EventDelegates.ZomeLoaded OnZomeLoaded;
        event EventDelegates.ZomeRemoved OnZomeRemoved;
        event EventDelegates.ZomeSaved OnZomeSaved;
        event EventDelegates.ZomesError OnZomesError;
        event EventDelegates.ZomesLoaded OnZomesLoaded;
        event EventDelegates.ZomesSaved OnZomesSaved;

        void Dim();
        void Emit();
        void Evolve();
        ICoronalEjection Flare();
        List<IHolon> GetHolonsThatBelongToZome(IZome zome);
        IZome GetZomeById(Guid id);
        IZome GetZomeByName(string name);
        IZome GetZomeThatHolonBelongsTo(IHolon holon);
        //OASISResult<API.Core.Interfaces.STAR.ICelestialBody> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        //Task<OASISResult<API.Core.Interfaces.STAR.ICelestialBody>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> LoadZomes<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> LoadZomesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IZome, new();
        void Love();
        void Mutate();
        void Radiate();
        void Reflect();
        OASISResult<ICelestialBody> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        void Seed();
        void Shine();
        void Super();
        void Twinkle();
    }
}