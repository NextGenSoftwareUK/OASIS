using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[mission]")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class MissionController : OASISControllerBase
    {
        private MapManager _mapManager;

        public MissionController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }

        //private MapManager MapManager
        //{
        //    get
        //    {
        //        if (_mapManager == null)
        //            _mapManager = new MapManager(GetAndActivateProvider());

        //        return _mapManager;
        //    }
        //}

        //[HttpGet("{search}")]
        //public async Task<ISearchResults> Get(string search)
        //{
        //    return await SearchManager.SearchAsync(search);
        //}

        //[HttpGet("{search}/{providerType}/{setGlobally}")]
        //public async Task<ISearchResults> Get(string search, ProviderType providerType, bool setGlobally = false)
        //{
        //    GetAndActivateProvider(providerType, setGlobally);
        //    return await SearchManager.SearchAsync(search);
        //}
    }
}
