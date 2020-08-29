using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
//using NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS;
//using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;
//using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
//using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
//using NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

namespace NextGenSoftware.OASIS.API.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AvatarController : ControllerBase
    {
        private AvatarManager _AvatarManager;

        private AvatarManager AvatarManager
        {
            get
            {
                if (_AvatarManager == null)
                    _AvatarManager = new AvatarManager(new HoloOASIS("ws://localhost:8888")); //Default to HoloOASIS Provider.

                return _AvatarManager;
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
        public async Task<IAvatar> Get(Guid id)
        {
            return await AvatarManager.LoadAvatarAsync(id);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<IAvatar> Get(Guid id, ProviderType providerType)
        {
            //TODO: This will fail if the requested provider has not been registered with the ProviderManager (soon this will bn automatic with MEF if the provider dll is in the providers hot folder).
            return await AvatarManager.LoadAvatarAsync(id, providerType);


            /*
            if (AvatarManager.DefaultProviderType != providerType)
            {
                switch (providerType)
                {
                    case ProviderType.ActivityPubOASIS:
                        AvatarManager.DefaultProvider = new AcitvityPubOASIS();
                        break;

                    case ProviderType.BlockStackOASIS:
                        AvatarManager.DefaultProvider = new BlockStackOASIS();
                        break;

                    case ProviderType.EOSOASIS:
                        //AvatarManager.DefaultProvider = new EOSOASIS();
                        break;

                    case ProviderType.EthereumOASIS:
                        AvatarManager.DefaultProvider = new EthereumOASIS();
                        break;

                    case ProviderType.HoloOASIS:
                        AvatarManager.DefaultProvider = new HoloOASIS(("ws://localhost:8888"));
                        break;

                    case ProviderType.IPFSOASIS:
                        AvatarManager.DefaultProvider = new IPFSOASIS;
                        break;

                    case ProviderType.LoonOASIS:
                        //AvatarManager.DefaultProvider = new LoonOASIS();
                        break;

                    case ProviderType.ScuttleBugOASIS:
                        //AvatarManager.DefaultProvider = new ScuttleBugOASIS();
                        break;

                    case ProviderType.SOLIDOASIS:
                        AvatarManager.DefaultProvider = new SOLIDOASIS();
                        break;

                    case ProviderType.StellarOASIS:
                        //AvatarManager.DefaultProvider = new ScuttleBugOASIS();
                        break;
                }

                AvatarManager.DefaultProviderType = providerType;
            }

            //AvatarManager.DefaultProvider = providerType;
            return await AvatarManager.LoadAvatarAsync(id);
            */
        }


        [HttpGet("{id}")]
        public async Task<IAvatar> Get(string providerKey)
        {
            return await AvatarManager.LoadAvatarAsync(providerKey);
        }

        [HttpGet("{id}")]
        public async Task<IAvatar> Get(string username, string password)
        {
            return await AvatarManager.LoadAvatarAsync(username, password);
        }


        //QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] IAvatar value)
        {
            AvatarManager.SaveAvatarAsync(value);
        }

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
