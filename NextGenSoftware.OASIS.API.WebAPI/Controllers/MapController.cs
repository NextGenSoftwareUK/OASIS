using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[map]")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
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

        public bool CreateRouteBetweenHolons(IHolon fromHolon, IHolon toHolon)
        {
            return  MapManager.CreateRouteBetweenHolons(fromHolon, toHolon);
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            return MapManager.Draw2DSpriteOnHUD(sprite, x, y);
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            return MapManager.Draw2DSpriteOnMap(sprite, x, y);
        }
    }
}
