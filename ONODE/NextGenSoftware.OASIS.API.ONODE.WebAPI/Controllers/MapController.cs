using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{


    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    //   [EnableCors()]
    //[Route("api/[mapping]")]
    [Route("api/map")]
    [ApiController]
    public class MapController : OASISControllerBase
    {
        private MapManager _mapManager;

        public MapController()
        {

        }

        private MapManager MapManager
        {
            get
            {
                if (_mapManager == null)
                    _mapManager = new MapManager(GetAndActivateDefaultStorageProvider());

                return _mapManager;
            }
        }

        /// <summary>
        /// Search the map.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <returns></returns>
        [HttpGet("Search/{searchParams}")]
        public async Task<OASISResult<ISearchResults>> Search(ISearchParams searchParams)
        {
            return new(await MapManager.SearchAsync(searchParams));
        }

        ///// <summary>
        ///// Search the map.
        ///// </summary>
        ///// <param name="searchParams"></param>
        ///// <param name="providerType"></param>
        ///// <param name="setGlobally"></param>
        ///// <returns></returns>
        //[HttpGet("Search/{searchParams}/{providerType}/{setGlobally}")]
        //public ActionResult<ISearchResults> Search(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        //{
        //    return Ok(MapManager.SearchAsync(searchParams).Result);
        //}

        /// <summary>
        /// Create and draw a route on the map between two holons.
        /// </summary>
        /// <param name="holonDNA"></param>
        /// <returns></returns>
        [HttpPost("CreateAndDrawRouteOnMapBetweenHolons/{holonDNA}")]
        public OASISResult<bool> CreateAndDrawRouteOnMapBetweenHolons(HolonDNA holonDNA)
        {
            return new(MapManager.CreateAndDrawRouteOnMapBetweenHolons(holonDNA.FromHolon, holonDNA.ToHolon));
        }

        ///// <summary>
        ///// Create and draw a route on the map between two holons.
        ///// </summary>
        ///// <param name="holonDNA"></param>
        ///// <returns></returns>
        //[HttpPost("CreateAndDrawRouteOnMapBetweenHolons/{holonDNA}/{providerType}/{setGlobally}")]
        //public ActionResult<bool> CreateAndDrawRouteOnMapBetweenHolons(HolonDNA holonDNA, ProviderType providerType, bool setGlobally = false)
        //{
        //    return MapManager.CreateAndDrawRouteOnMapBetweenHolons(holonDNA.FromHolon, holonDNA.ToHolon);
        //}

        /// <summary>
        /// Create and draw a route on the map between two points.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        [HttpPost("CreateAndDrawRouteOnMapBeweenPoints/{points}")]
        public OASISResult<bool> CreateAndDrawRouteOnMapBeweenPoints(MapPoints points)
        {
            return new(MapManager.CreateAndDrawRouteOnMapBeweenPoints(points));
        }

        ///// <summary>
        ///// Create and draw a route on the map between two points.
        ///// </summary>
        ///// <param name="points"></param>
        ///// <returns></returns>
        //[HttpPost("CreateAndDrawRouteOnMapBeweenPoints/{points}/{providerType}/{setGlobally}")]
        //public ActionResult<bool> CreateAndDrawRouteOnMapBeweenPoints(MapPoints points, ProviderType providerType, bool setGlobally = false)
        //{
        //    return MapManager.CreateAndDrawRouteOnMapBeweenPoints(points);
        //}

        /// <summary>
        /// Draw a 2D sprint on the Our World HUD.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [HttpPost("Draw2DSpriteOnHUD/{sprite}/{x}/{y}")]
        //public ActionResult<bool> Draw2DSpriteOnHUD(object sprite, float x, float y)
        public OASISResult<bool> Draw2DSpriteOnHUD(string sprite, float x, float y)
        {
            return new(MapManager.Draw2DSpriteOnHUD(sprite, x, y));
        }

        ///// <summary>
        ///// Draw a 2D sprint on the Our World HUD.
        ///// </summary>
        ///// <param name="sprite"></param>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        ///// <returns></returns>
        //[HttpPost("Draw2DSpriteOnHUD/{sprite}/{x}/{y}/{providerType}/{setGlobally}")]
        ////public ActionResult<bool> Draw2DSpriteOnHUD(object sprite, float x, float y)
        //public ActionResult<bool> Draw2DSpriteOnHUD(string sprite, float x, float y, ProviderType providerType, bool setGlobally = false)
        //{
        //    return MapManager.Draw2DSpriteOnHUD(sprite, x, y);
        //}

        /// <summary>
        /// Draw a 2D sprint on the map.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [HttpPost("Draw2DSpriteOnMap/{sprite}/{x}/{y}")]
        public OASISResult<bool> Draw2DSpriteOnMap(string sprite, float x, float y)
        {
            return new(MapManager.Draw2DSpriteOnMap(sprite, x, y));
        }

        ///// <summary>
        ///// Draw a 2D sprint on the map.
        ///// </summary>
        ///// <param name="sprite"></param>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        ///// <returns></returns>
        //[HttpPost("Draw2DSpriteOnMap/{sprite}/{x}/{y}")]
        //public ActionResult<bool> Draw2DSpriteOnMap(string sprite, float x, float y, ProviderType providerType, bool setGlobally = false)
        //{
        //    return MapManager.Draw2DSpriteOnMap(sprite, x, y);
        //}

        /// <summary>
        /// Draw a 3D object on the map.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        [HttpPost("Draw3DObjectOnMap/{obj}/{x}/{y}")]
        public OASISResult<bool> Draw3DObjectOnMap(string obj, float x, float y)
        {
            return new(MapManager.Draw3DObjectOnMap(obj, x, y));
        }

        /// <summary>
        /// Highlight a building on the map.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        [HttpPost("HighlightBuildingOnMap/{building}")]
        public OASISResult<bool> HighlightBuildingOnMap(Building building)
        {
            return new(MapManager.HighlightBuildingOnMap(building));
        }

        /// <summary>
        /// Pam the map down.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("PamMapDown/{value}")]
        public OASISResult<bool> PamMapDown(float value)
        {
            return new(MapManager.PamMapDown(value));
        }

        /// <summary>
        /// Pam the map left.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("PamMapLeft/{value}")]
        public OASISResult<bool> PamMapLeft(float value)
        {
            return new(MapManager.PamMapLeft(value));
        }

        /// <summary>
        /// Pam the map right.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("PamMapRight/{value}")]
        public OASISResult<bool> PamMapRight(float value)
        {
            return new(MapManager.PamMapRight(value));
        }

        /// <summary>
        /// Pam the map up.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("PamMapUp/{value}")]
        public OASISResult<bool> PamMapUp(float value)
        {
            return new(MapManager.PamMapUp(value));
        }

        /// <summary>
        /// Select building on map.
        /// </summary>
        /// <param name="building"></param>
        /// <returns></returns>
        [HttpPost("SelectBuildingOnMap/{building}")]
        public OASISResult<bool> SelectBuildingOnMap(Building building)
        {
            return new(MapManager.SelectBuildingOnMap(building));
        }

        /// <summary>
        /// Select holon on map.
        /// </summary>
        /// <param name="holon"></param>
        /// <returns></returns>
        [HttpPost("SelectHolonOnMap/{holon}")]
        public OASISResult<bool> SelectHolonOnMap(Holon holon)
        {
            return new(MapManager.SelectHolonOnMap(holon));
        }

        /// <summary>
        /// Select quest on map.
        /// </summary>
        /// <param name="quest"></param>
        /// <returns></returns>
        [HttpPost("SelectQuestOnMap/{quest}")]
        public OASISResult<bool> SelectQuestOnMap(Quest quest)
        {
            return new(MapManager.SelectQuestOnMap(quest));
        }

        /// <summary>
        /// Zoom map in.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("ZoomMapIn/{value}")]
        public OASISResult<bool> ZoomMapIn(float value)
        {
            return new(MapManager.ZoomMapIn(value));
        }

        /// <summary>
        /// Zoom map out.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost("ZoomMapOut/{value}")]
        public OASISResult<bool> ZoomMapOut(float value)
        {
            return new(MapManager.ZoomMapOut(value));
        }

        /// <summary>
        /// Zoom to holon on map.
        /// </summary>
        /// <param name="holon"></param>
        /// <returns></returns>
        [HttpPost("ZoomToHolonOnMap/{holon}")]
        public OASISResult<bool> ZoomToHolonOnMap(Holon holon)
        {
            return new(MapManager.ZoomToHolonOnMap(holon));
        }

        /// <summary>
        /// Zoom to quest on map.
        /// </summary>
        /// <param name="quest"></param>
        /// <returns></returns>
        [HttpPost("ZoomToQuestOnMap/{quest}")]
        public OASISResult<bool> ZoomToQuestOnMap(Quest quest)
        {
            return new(MapManager.ZoomToQuestOnMap(quest));
        }


        /// <summary>
        /// Get nearby locations for the current avatar
        /// </summary>
        /// <param name="latitude">Current latitude</param>
        /// <param name="longitude">Current longitude</param>
        /// <param name="radiusKm">Search radius in kilometers</param>
        /// <returns>List of nearby locations</returns>
        [Authorize]
        [HttpGet("nearby")]
        public async Task<OASISResult<List<MapLocation>>> GetNearbyLocations([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 10.0)
        {
            return await MapManager.Instance.GetNearbyLocationsAsync(Avatar.Id, latitude, longitude, radiusKm);
        }

        /// <summary>
        /// Visit a location
        /// </summary>
        /// <param name="locationId">Location ID</param>
        /// <param name="purpose">Optional purpose for the visit</param>
        /// <returns>Success status</returns>
        [Authorize]
        [HttpPost("visit/{locationId}")]
        public async Task<OASISResult<bool>> VisitLocation(Guid locationId, [FromBody] string purpose = null)
        {
            return await MapManager.Instance.VisitLocationAsync(Avatar.Id, locationId, purpose);
        }

        /// <summary>
        /// Get visit history for the current avatar
        /// </summary>
        /// <param name="limit">Number of visits to return</param>
        /// <param name="offset">Number of visits to skip</param>
        /// <returns>Visit history</returns>
        [Authorize]
        [HttpGet("visit-history")]
        public async Task<OASISResult<List<MapVisit>>> GetVisitHistory([FromQuery] int limit = 50, [FromQuery] int offset = 0)
        {
            return await MapManager.Instance.GetVisitHistoryAsync(Avatar.Id, limit, offset);
        }

        /// <summary>
        /// Search for locations
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="type">Optional location type filter</param>
        /// <param name="latitude">Optional latitude for proximity search</param>
        /// <param name="longitude">Optional longitude for proximity search</param>
        /// <param name="radiusKm">Optional search radius in kilometers</param>
        /// <returns>List of matching locations</returns>
        [HttpGet("search")]
        public async Task<OASISResult<List<MapLocation>>> SearchLocations(
            [FromQuery] string query,
            [FromQuery] LocationType? type = null,
            [FromQuery] double? latitude = null,
            [FromQuery] double? longitude = null,
            [FromQuery] double? radiusKm = null)
        {
            return await MapManager.Instance.SearchLocationsAsync(query, type, latitude, longitude, radiusKm);
        }

        /// <summary>
        /// Get map statistics for the current avatar
        /// </summary>
        /// <returns>Map statistics</returns>
        [Authorize]
        [HttpGet("stats")]
        public async Task<OASISResult<Dictionary<string, object>>> GetMapStats()
        {
            return await MapManager.Instance.GetMapStatsAsync(Avatar.Id);
        }
    }
}
