//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;

//using NextGenSoftware.OASIS.API.Core;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{


//    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
//    //   [EnableCors()]
//    //[Route("api/[mapping]")]
//    [Route("api/[controller]")]
//    [ApiController]
//    public class MapController : OASISControllerBase
//    {
//        private MapManager _mapManager;

//        public MapController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
//        {

//        }

//        private MapManager MapManager
//        {
//            get
//            {
//                if (_mapManager == null)
//                    _mapManager = new MapManager(GetAndActivateProvider());

//                return _mapManager;
//            }
//        }


//        //[HttpPost("authenticate/{providerType}/{setGlobally}")]
//        //public ActionResult<AuthenticateResponse> Authenticate(AuthenticateRequest model, ProviderType providerType, bool setGlobally = false)
//        //{
//        //    GetAndActivateProvider(providerType, setGlobally);
//        //    return Authenticate(model);
//        //}

//        //[HttpPost("CreateRouteBetweenHolons/{holonDNA}")]
//        ////[HttpPost("CreateRouteBetweenHolons/{fromHolon}/{toHolon}")]
//        ////public ActionResult<bool> CreateRouteBetweenHolons(IHolon fromHolon, IHolon toHolon)
//        public ActionResult<bool> CreateRouteBetweenHolons(HolonDNA holonDNA)
//        {
//            return MapManager.CreateRouteBetweenHolons(holonDNA.FromHolon, holonDNA.ToHolon);
//        }

//        [HttpPost("Draw2DSpriteOnHUD/{sprite}/{x}/{y}")]
//        //public ActionResult<bool> Draw2DSpriteOnHUD(object sprite, float x, float y)
//        public ActionResult<bool> Draw2DSpriteOnHUD(string sprite, float x, float y)
//        {
//            return MapManager.Draw2DSpriteOnHUD(sprite, x, y);
//        }

//        [HttpPost("Draw2DSpriteOnMap/{sprite}/{x}/{y}")]
//        public ActionResult<bool> Draw2DSpriteOnMap(string sprite, float x, float y)
//        {
//            return MapManager.Draw2DSpriteOnMap(sprite, x, y);
//        }
//    }
//}
