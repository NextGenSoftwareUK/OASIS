using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;

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

        public MapController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }

        private MapManager MapManager
        {
            get
            {
                if (_mapManager == null)
                    _mapManager = new MapManager(GetAndActivateProvider());

                return _mapManager;
            }
        }

        [HttpGet("CreateRouteBetweenHolons/{holonDNA}")]
        public ActionResult<bool> CreateRouteBetweenHolons(HolonDNA holonDNA)
        {
            return MapManager.CreateRouteBetweenHolons(holonDNA.FromHolon, holonDNA.ToHolon);
        }

        [HttpGet("Draw2DSpriteOnHUD/{sprite}/{x}/{y}")]
        //public ActionResult<bool> Draw2DSpriteOnHUD(object sprite, float x, float y)
        public ActionResult<bool> Draw2DSpriteOnHUD(string sprite, float x, float y)
        {
            return MapManager.Draw2DSpriteOnHUD(sprite, x, y);
        }

        [HttpGet("Draw2DSpriteOnMap/{sprite}/{x}/{y}")]
        public ActionResult<bool> Draw2DSpriteOnMap(string sprite, float x, float y)
        {
            return MapManager.Draw2DSpriteOnMap(sprite, x, y);
        }

        [HttpGet("Draw3DObjectOnMap/{obj}/{x}/{y}")]
        public ActionResult<bool> Draw3DObjectOnMap(string obj, float x, float y)
        {
            return MapManager.Draw3DObjectOnMap(obj, x, y);
        }

        [HttpGet("DrawRouteOnMap/{points}")]
        public ActionResult<bool> DrawRouteOnMap(MapPoints points)
        {
            return MapManager.DrawRouteOnMap(points);
        }

        [HttpGet("HighlightBuildingOnMap/{building}")]
        public ActionResult<bool> HighlightBuildingOnMap(Building building)
        {
            return MapManager.HighlightBuildingOnMap(building);
        }

        [HttpGet("SearchMap/{searchParams}")]
        public ActionResult<ISearchResults> SearchMap(ISearchParams searchParams)
        {
            return Ok(MapManager.SearchAsync(searchParams).Result);
        }

        [HttpGet("PanMapDown/{value}")]
        public ActionResult<bool> PanMapDown(float value)
        {
            return Ok(MapManager.PanMapDown(value));
        }

        [HttpGet("PanMapLeft/{value}")]
        public ActionResult<bool> PanMapLeft(float value)
        {
            return Ok(MapManager.PanMapLeft(value));
        }

        [HttpGet("PanMapRight/{value}")]
        public ActionResult<bool> PanMapRight(float value)
        {
            return Ok(MapManager.PanMapRight(value));
        }

        [HttpGet("PanMapUp/{value}")]
        public ActionResult<bool> PanMapUp(float value)
        {
            return Ok(MapManager.PanMapUp(value));
        }

        [HttpGet("SelectBuildingOnMap/{building}")]
        public ActionResult<bool> SelectBuildingOnMap(Building building)
        {
            return Ok(MapManager.SelectBuildingOnMap(building));
        }

        [HttpGet("SelectHolonOnMap/{holon}")]
        public ActionResult<bool> SelectHolonOnMap(Holon holon)
        {
            return Ok(MapManager.SelectHolonOnMap(holon));
        }

        [HttpGet("SelectQuestOnMap/{quest}")]
        public ActionResult<bool> SelectQuestOnMap(Quest quest)
        {
            return Ok(MapManager.SelectQuestOnMap(quest));
        }

        [HttpGet("ZoomMapIn/{value}")]
        public ActionResult<bool> ZoomMapIn(float value)
        {
            return Ok(MapManager.ZoomMapIn(value));
        }

        [HttpGet("ZoomMapOut/{value}")]
        public ActionResult<bool> ZoomMapOut(float value)
        {
            return Ok(MapManager.ZoomMapOut(value));
        }

        [HttpGet("ZoomToHolonOnMap/{holon}")]
        public ActionResult<bool> ZoomToHolonOnMap(Holon holon)
        {
            return Ok(MapManager.ZoomToHolonOnMap(holon));
        }

        [HttpGet("ZoomToQuestOnMap/{quest}")]
        public ActionResult<bool> ZoomToQuestOnMap(Quest quest)
        {
            return Ok(MapManager.ZoomToQuestOnMap(quest));
        }
    }
}
