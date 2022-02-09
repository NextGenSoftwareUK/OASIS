using System.Collections.Generic;
using System.Threading.Tasks;
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

        OASISResult<ICelestialSpace> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<ICelestialSpace>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IEnumerable<ICelestialBody>> LoadCelestialBodies(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IEnumerable<ICelestialBody>>> LoadCelestialBodiesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IEnumerable<ICelestialSpace>> LoadCelestialSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IEnumerable<ICelestialSpace>>> LoadCelestialSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<ICelestialSpace> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        Task<OASISResult<ICelestialSpace>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        OASISResult<IEnumerable<ICelestialBody>> SaveCelestialBodies(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        Task<OASISResult<IEnumerable<ICelestialBody>>> SaveCelestialBodiesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        OASISResult<IEnumerable<ICelestialSpace>> SaveCelestialSpaces(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        Task<OASISResult<IEnumerable<ICelestialSpace>>> SaveCelestialSpacesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
    }
}