using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoOASIS;
//using NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS;
//using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;
//using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
//using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
//using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

namespace NextGenSoftware.OASIS.API.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class SCMController : ControllerBase
    {

        [HttpGet("{scm}")]
        public async Task<ISearchResults> Get()
        {
            SearchResults result = new SearchResults();
            result.SearchResult = new List<string> { "boo!" };

            return result;
        }

    }
}
