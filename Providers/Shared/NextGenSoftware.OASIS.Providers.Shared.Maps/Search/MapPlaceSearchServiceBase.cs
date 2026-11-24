using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Http;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Search
{
    public abstract class MapPlaceSearchServiceBase : IMapPlaceSearchService
    {
        public abstract string Name { get; }
        public string LastError { get; private set; }

        public async Task<OASISResult<List<MapPointOfInterest>>> SearchAsync(MapPlaceSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Near != null && !GeoMath.IsValid(request.Near))
                return MapResults.Error<List<MapPointOfInterest>>(Name + " needs a valid WGS84 bias point.");

            OASISResult<List<MapPointOfInterest>> result =
                await RequestSearchAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.IsError) return result;

            foreach (MapPointOfInterest place in result.Result)
            {
                place.Source = Name;
                if (request.Near != null && place.Distance <= 0f && place.Location != null)
                    place.Distance = (float)GeoMath.HaversineMetres(request.Near, place.Location);
            }

            if (request.Near != null) result.Result = result.Result.OrderBy(p => p.Distance).ToList();
            return result;
        }

        public async Task<ForwardGeocodingResponse<IPointOfInterest>> POISearchAsync(IRequest request)
        {
            if (!(request is MapPlaceSearchRequest search))
                throw new ArgumentException(Name + " searches take a " + nameof(MapPlaceSearchRequest) + ".", nameof(request));

            OASISResult<List<MapPointOfInterest>> result = await SearchAsync(search).ConfigureAwait(false);
            LastError = result.IsError ? result.Message : null;
            return new ForwardGeocodingResponse<IPointOfInterest>(!result.IsError,
                result.IsError ? new List<IPointOfInterest>() : result.Result.Cast<IPointOfInterest>().ToList());
        }

        public async Task<ForwardGeocodingResponse<IPointOfInterest>> BatchPOISearchAsync(IBatchRequest request)
        {
            if (!(request is MapBatchPlaceSearchRequest batch))
                throw new ArgumentException(Name + " batch searches take a " + nameof(MapBatchPlaceSearchRequest) + ".", nameof(request));

            var combined = new List<IPointOfInterest>();
            foreach (MapPlaceSearchRequest search in batch.Requests)
            {
                OASISResult<List<MapPointOfInterest>> result = await SearchAsync(search).ConfigureAwait(false);
                if (result.IsError)
                {
                    LastError = "Search '" + search.Query + "' failed: " + result.Message;
                    return new ForwardGeocodingResponse<IPointOfInterest>(false, combined);
                }

                combined.AddRange(result.Result);
            }

            LastError = null;
            return new ForwardGeocodingResponse<IPointOfInterest>(true, combined);
        }

        protected abstract Task<OASISResult<List<MapPointOfInterest>>> RequestSearchAsync(MapPlaceSearchRequest request,
            CancellationToken cancellationToken);
    }
}
