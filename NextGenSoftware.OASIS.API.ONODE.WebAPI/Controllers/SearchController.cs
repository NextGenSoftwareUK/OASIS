using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
  //  [Route("api/[search]")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class SearchController : OASISControllerBase
    {
        private SearchManager _SearchManager;

        //public SearchController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        //{

        //}

        public SearchController()
        {

        }

        private SearchManager SearchManager
        {
            get
            {
                if (_SearchManager == null)
                    _SearchManager = new SearchManager(GetAndActivateDefaultProvider());

                return _SearchManager;
            }
        }

        [HttpGet("{searchParams}")]
        public async Task<ISearchResults> Get(string searchParams)
        {
            return await SearchManager.SearchAsync(new SearchParams() { SearchQuery = searchParams });
        }

        [HttpGet("{searchParams}/{providerType}/{setGlobally}")]
        public async Task<ISearchResults> Get(string searchParams, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await Get(searchParams);
        }
    }
}
