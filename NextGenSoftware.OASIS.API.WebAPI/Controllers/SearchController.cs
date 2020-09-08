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
    public class SearchController : ControllerBase
    {
        private SearchManager _SearchManager;

        private SearchManager SearchManager
        {
            get
            {
                //TODO: Check if this or AvatarManager way is better in Program?
                if (_SearchManager == null)
                    _SearchManager = new SearchManager(new MongoOASIS("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/test?retryWrites=true&w=majority", "OASISAPI")); //Default to HoloOASIS Provider.
                    //_SearchManager = new SearchManager(new HoloOASIS("ws://localhost:8888")); //Default to HoloOASIS Provider.

                return _SearchManager;
            }
        }

        /*
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<ISearch> Get(Guid id)
        {
            return await SearchManager.LoadSearchAsync(id);
        }*/

        // GET api/values/5
        //[HttpGet("{id}")]
        //public async Task<ISearch> Get(Guid id, ProviderType providerType)
        //{
        //    //TODO: This will fail if the requested provider has not been registered with the ProviderManager (soon this will bn automatic with MEF if the provider dll is in the providers hot folder).
        //    return await SearchManager.SearchAsync(id, providerType);


            /*
            if (SearchManager.DefaultProviderType != providerType)
            {
                switch (providerType)
                {
                    case ProviderType.ActivityPubOASIS:
                        SearchManager.DefaultProvider = new AcitvityPubOASIS();
                        break;

                    case ProviderType.BlockStackOASIS:
                        SearchManager.DefaultProvider = new BlockStackOASIS();
                        break;

                    case ProviderType.EOSOASIS:
                        //SearchManager.DefaultProvider = new EOSOASIS();
                        break;

                    case ProviderType.EthereumOASIS:
                        SearchManager.DefaultProvider = new EthereumOASIS();
                        break;

                    case ProviderType.HoloOASIS:
                        SearchManager.DefaultProvider = new HoloOASIS(("ws://localhost:8888"));
                        break;

                    case ProviderType.IPFSOASIS:
                        SearchManager.DefaultProvider = new IPFSOASIS;
                        break;

                    case ProviderType.LoonOASIS:
                        //SearchManager.DefaultProvider = new LoonOASIS();
                        break;

                    case ProviderType.ScuttleBugOASIS:
                        //SearchManager.DefaultProvider = new ScuttleBugOASIS();
                        break;

                    case ProviderType.SOLIDOASIS:
                        SearchManager.DefaultProvider = new SOLIDOASIS();
                        break;

                    case ProviderType.StellarOASIS:
                        //SearchManager.DefaultProvider = new ScuttleBugOASIS();
                        break;
                }

                SearchManager.DefaultProviderType = providerType;
            }

            //SearchManager.DefaultProvider = providerType;
            return await SearchManager.LoadSearchAsync(id);
            */
      //  }


        //[HttpGet("{id}")]
        //public async Task<ISearch> Get(string providerKey)
        //{
        //    return await SearchManager.LoadSearchAsync(providerKey);
        //}

        //[HttpGet("{id}")]
        //public async Task<ISearch> Get(string username, string password)
        //{
        //    return await SearchManager.LoadSearchAsync(username, password);
        //}

        [HttpGet("{search}")]
        public async Task<ISearchResults> Get(string search)
        {
            return await SearchManager.SearchAsync(search);
        }

        //[HttpGet("{search}")]
        //public async Task<ISearchResults> Index(string searchTerm)
        //{
        //    return await SearchManager.SearchAsync(searchTerm);
        //}
        

        //QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg

        // PUT api/values/5
        //[HttpPut("{search}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //    SearchManager.SearchAsync(value);
        //}

        // POST api/values
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
