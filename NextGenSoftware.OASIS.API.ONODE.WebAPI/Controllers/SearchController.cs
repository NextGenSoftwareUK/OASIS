using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    //[Route("api/[search]")]
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
                    _SearchManager = new SearchManager(GetAndActivateProvider());

                return _SearchManager;
            }
        }

        [HttpGet("{searchParams}")]
        public async Task<ISearchResults> Get(ISearchParams searchParams)
        {
            return await SearchManager.SearchAsync(searchParams);
        }

        [HttpGet("{searchParams}/{providerType}/{setGlobally}")]
        public async Task<ISearchResults> Get(ISearchParams searchParams, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return await SearchManager.SearchAsync(searchParams);
        }
    }
}
