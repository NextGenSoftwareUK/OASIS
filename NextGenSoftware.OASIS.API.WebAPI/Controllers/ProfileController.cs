using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;
    
namespace NextGenSoftware.OASIS.API.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private ProfileManager _profileManager;

        private ProfileManager ProfileManager
        {
            get
            {
                if (_profileManager == null)
                    _profileManager = new ProfileManager(new HoloOASIS("ws://localhost:8888"));

                return _profileManager;
            }
        }

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
        public async Task<IProfile> Get(Guid id)
        {
            return await ProfileManager.LoadProfileAsync(id);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<IProfile> Get(Guid id, ProviderType providerType)
        {
            //TODO: This will fail if the requested provider has not been registered with the ProviderManager (soon this will bn automatic with MEF if the provider dll is in the providers hot folder).
            return await ProfileManager.LoadProfileAsync(id, providerType);


            /*
            if (ProfileManager.DefaultProviderType != providerType)
            {
                switch (providerType)
                {
                    case ProviderType.ActivityPubOASIS:
                        ProfileManager.DefaultProvider = new AcitvityPubOASIS();
                        break;

                    case ProviderType.BlockStackOASIS:
                        ProfileManager.DefaultProvider = new BlockStackOASIS();
                        break;

                    case ProviderType.EOSOASIS:
                        //ProfileManager.DefaultProvider = new EOSOASIS();
                        break;

                    case ProviderType.EthereumOASIS:
                        ProfileManager.DefaultProvider = new EthereumOASIS();
                        break;

                    case ProviderType.HoloOASIS:
                        ProfileManager.DefaultProvider = new HoloOASIS(("ws://localhost:8888"));
                        break;

                    case ProviderType.IPFSOASIS:
                        ProfileManager.DefaultProvider = new IPFSOASIS;
                        break;

                    case ProviderType.LoonOASIS:
                        //ProfileManager.DefaultProvider = new LoonOASIS();
                        break;

                    case ProviderType.ScuttleBugOASIS:
                        //ProfileManager.DefaultProvider = new ScuttleBugOASIS();
                        break;

                    case ProviderType.SOLIDOASIS:
                        ProfileManager.DefaultProvider = new SOLIDOASIS();
                        break;

                    case ProviderType.StellarOASIS:
                        //ProfileManager.DefaultProvider = new ScuttleBugOASIS();
                        break;
                }

                ProfileManager.DefaultProviderType = providerType;
            }

            //ProfileManager.DefaultProvider = providerType;
            return await ProfileManager.LoadProfileAsync(id);
            */
        }


        [HttpGet("{id}")]
        public async Task<IProfile> Get(string providerKey)
        {
            return await ProfileManager.LoadProfileAsync(providerKey);
        }

        [HttpGet("{id}")]
        public async Task<IProfile> Get(string username, string password)
        {
            return await ProfileManager.LoadProfileAsync(username, password);
        }


        //QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] IProfile value)
        {
            ProfileManager.SaveProfileAsync(value);
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
