using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[search]")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class SearchController : OASISControllerBase
    {
        private SearchManager _SearchManager;

        public SearchController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }

        private SearchManager SearchManager
        {
            get
            {
                if (_SearchManager == null)
                    _SearchManager = new SearchManager(GetAndActivateProvider());

                return _SearchManager;
            }
        }

        [HttpGet("{search}")]
        public async Task<ISearchResults> Get(string search)
        {
            return await SearchManager.SearchAsync(search);
        }

        [HttpGet("{search}/{providerType}/{setGlobally}")]
        public async Task<ISearchResults> Get(string search, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await SearchManager.SearchAsync(search);
        }
    }
}
