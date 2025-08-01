using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialSpace : ICelestialHolon
    {
        IStar NearestStar { get; set; }
        ReadOnlyCollection<ICelestialBody> CelestialBodies { get; }
        ReadOnlyCollection<ICelestialSpace> CelestialSpaces { get; }
        //new ReadOnlyCollection<IHolon> Children { get; }

        event CelestialBodiesError OnCelestialBodiesError;
        event CelestialBodiesLoaded OnCelestialBodiesLoaded;
        event CelestialBodiesSaved OnCelestialBodiesSaved;
        event CelestialBodyAdded OnCelestialBodyAdded;
        event CelestialBodyError OnCelestialBodyError;
        event CelestialBodyLoaded OnCelestialBodyLoaded;
        event CelestialBodyRemoved OnCelestialBodyRemoved;
        event CelestialBodySaved OnCelestialBodySaved;
        event CelestialSpaceAdded OnCelestialSpaceAdded;
        event CelestialSpaceError OnCelestialSpaceError;
        event CelestialSpaceLoaded OnCelestialSpaceLoaded;
        event CelestialSpaceRemoved OnCelestialSpaceRemoved;
        event CelestialSpaceSaved OnCelestialSpaceSaved;
        event CelestialSpacesError OnCelestialSpacesError;
        event CelestialSpacesLoaded OnCelestialSpacesLoaded;
        event CelestialSpacesSaved OnCelestialSpacesSaved;
        event HolonError OnHolonError;
        event HolonLoaded OnHolonLoaded;
        event HolonSaved OnHolonSaved;
        event HolonsError OnHolonsError;
        event HolonsLoaded OnHolonsLoaded;
        event HolonsSaved OnHolonsSaved;
        event ZomeError OnZomeError;
        event ZomeLoaded OnZomeLoaded;
        event ZomeSaved OnZomeSaved;
        event ZomesError OnZomesError;
        event ZomesLoaded OnZomesLoaded;
        event ZomesSaved OnZomesSaved;

        OASISResult<ICelestialBody> AddCelestialBody(ICelestialBody celestialBody, bool saveCelestialBody = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialBody>> AddCelestialBodyAsync(ICelestialBody celestialBody, bool saveCelestialBody = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<ICelestialSpace> AddCelestialSpace(ICelestialSpace celestialSpace, bool saveCelestialSpace = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialSpace>> AddCelestialSpaceAsync(ICelestialSpace celestialSpace, bool saveCelestialSpace = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<ICelestialSpace> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<ICelestialSpace>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        OASISResult<IEnumerable<ICelestialBody>> LoadCelestialBodies(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> LoadCelestialBodies<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();
        OASISResult<ICelestialBodiesAndSpaces> LoadCelestialBodiesAndSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<ICelestialBodiesAndSpaces<T1, T2>> LoadCelestialBodiesAndSpaces<T1, T2>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new();
        Task<OASISResult<ICelestialBodiesAndSpaces>> LoadCelestialBodiesAndSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialBodiesAndSpaces<T1, T2>>> LoadCelestialBodiesAndSpacesAsync<T1, T2>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new();
        Task<OASISResult<IEnumerable<ICelestialBody>>> LoadCelestialBodiesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> LoadCelestialBodiesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();
        OASISResult<IEnumerable<ICelestialSpace>> LoadCelestialSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> LoadCelestialSpaces<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();
        Task<OASISResult<IEnumerable<ICelestialSpace>>> LoadCelestialSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> LoadCelestialSpacesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();
        OASISResult<ICelestialBody> RemoveCelestialBody(ICelestialBody celestialBody, Guid avatarId, bool deleteCelestialBody = true, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialBody>> RemoveCelestialBodyAsync(ICelestialBody celestialBody, Guid avatarId, bool deleteCelestialBody = true, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        OASISResult<ICelestialSpace> RemoveCelestialSpace(ICelestialSpace celestialSpace, Guid avatarId, bool deleteCelestialSpace = true, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialSpace>> RemoveCelestialSpaceAsync(ICelestialSpace celestialSpace, Guid avatarId, bool deleteCelestialSpace = true, bool softDelete = true, ProviderType providerType = ProviderType.Default);
        OASISResult<ICelestialSpace> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        Task<OASISResult<ICelestialSpace>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new();
        OASISResult<IEnumerable<ICelestialBody>> SaveCelestialBodies(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<ICelestialBody>>> SaveCelestialBodiesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> SaveCelestialBodies<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();
        Task<OASISResult<IEnumerable<T>>> SaveCelestialBodiesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();
        OASISResult<ICelestialBodiesAndSpaces> SaveCelestialBodiesAndSpaces(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialBodiesAndSpaces>> SaveCelestialBodiesAndSpacesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        OASISResult<ICelestialBodiesAndSpaces<T1, T2>> SaveCelestialBodiesAndSpaces<T1, T2>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new();
        Task<OASISResult<ICelestialBodiesAndSpaces<T1, T2>>> SaveCelestialBodiesAndSpacesAsync<T1, T2>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new();
        OASISResult<IEnumerable<ICelestialSpace>> SaveCelestialSpaces(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> SaveCelestialSpaces<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();
        Task<OASISResult<IEnumerable<ICelestialSpace>>> SaveCelestialSpacesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<T>>> SaveCelestialSpacesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();
    }
}