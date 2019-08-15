using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;

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
