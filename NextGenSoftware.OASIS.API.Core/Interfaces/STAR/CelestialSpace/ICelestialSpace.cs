using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using static NextGenSoftware.OASIS.API.Core.Events.Events;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ICelestialSpace : ICelestialHolon
    {
        event CelestialSpaceLoaded OnCelestialSpaceLoaded;
        event CelestialSpaceSaved OnCelestialSpaceSaved;
        event CelestialSpaceError OnCelestialSpaceError;
        event CelestialSpacesLoaded OnCelestialSpacesLoaded;
        event CelestialSpacesSaved OnCelestialSpacesSaved;
        event CelestialSpacesError OnCelestialSpacesError;
        event CelestialBodyLoaded OnCelestialBodyLoaded;
        event CelestialBodySaved OnCelestialBodySaved;
        event CelestialBodyError OnCelestialBodyError;
        event CelestialBodiesLoaded OnCelestialBodiesLoaded;
        event CelestialBodiesSaved OnCelestialBodiesSaved;
        event CelestialBodiesError OnCelestialBodiesError;
        event ZomeLoaded OnZomeLoaded;
        event ZomeSaved OnZomeSaved;
        event ZomeError OnZomeError;
        event ZomesLoaded OnZomesLoaded;
        event ZomesSaved OnZomesSaved;
        event ZomesError OnZomesError;
        event HolonLoaded OnHolonLoaded;
        event HolonSaved OnHolonSaved;
        event HolonError OnHolonError;
        event HolonsLoaded OnHolonsLoaded;
        event HolonsSaved OnHolonsSaved;
        event HolonsError OnHolonsError;

        OASISResult<ICelestialSpace> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialSpace>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();
        Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();

        OASISResult<IEnumerable<ICelestialBody>> LoadCelestialBodies(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<ICelestialBody>>> LoadCelestialBodiesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> LoadCelestialBodies<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();
        Task<OASISResult<IEnumerable<T>>> LoadCelestialBodiesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();

        OASISResult<IEnumerable<ICelestialSpace>> LoadCelestialSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<ICelestialSpace>>> LoadCelestialSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> LoadCelestialSpaces<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();
        Task<OASISResult<IEnumerable<T>>> LoadCelestialSpacesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();

        OASISResult<ICelestialSpace> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ICelestialSpace>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default);
        OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();
        Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();

        OASISResult<IEnumerable<ICelestialBody>> SaveCelestialBodies(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<ICelestialBody>>> SaveCelestialBodiesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> SaveCelestialBodies<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();
        Task<OASISResult<IEnumerable<T>>> SaveCelestialBodiesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new();

        OASISResult<IEnumerable<ICelestialSpace>> SaveCelestialSpaces(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<ICelestialSpace>>> SaveCelestialSpacesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<T>> SaveCelestialSpaces<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();
        Task<OASISResult<IEnumerable<T>>> SaveCelestialSpacesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new();
    }
}