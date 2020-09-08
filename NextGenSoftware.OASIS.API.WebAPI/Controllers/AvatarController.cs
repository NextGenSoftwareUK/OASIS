using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core;
using WebAPI.Models;

namespace NextGenSoftware.OASIS.API.WebAPI.Controllers
{
    [Route("api/[controller]")]
    //[Route("api/[avatar]")] //TODO: Get this working, think better way?
    [ApiController]
    public class AvatarController : OASISControllerBase
    {
        private static AvatarManager _avatarManager;

        public AvatarController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }

        public AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                {
                    _avatarManager = new AvatarManager(GetAndActivateProvider());
                    _avatarManager.OnOASISManagerError += _avatarManager_OnOASISManagerError;
                }

                return _avatarManager;
            }
        }

        private void _avatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        {
            
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateModel model)
        {
            IEnumerable<IAvatar>_avatars = await AvatarManager.LoadAllAvatarsAsync();
            var avatar = await Task.Run(() => _avatars.SingleOrDefault(x => x.Username == model.Username && x.Password == model.Password));

            if (avatar == null)
                return BadRequest(new { message = "Avatar Name or password is incorrect" });

            avatar.Password = null;
            return Ok(avatar);
            //return Ok(avatar.WithoutPassword());
        }

        //TODO: Come back to this...
        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    List<Avatar> avatars = AvatarManager.LoadAllAvatarsAsync().Result;

        //    foreach (Avatar avatar in avatars)
        //        avatar.Password = null;
        //    }
        //    return Ok(avatars);
        //}

        //// GET api/values
        //[HttpGet]
        //public ActionResult<IEnumerable<string>> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/values/5
        //[HttpGet("{id}")]
        //public ActionResult<string> Get(int id)
        //{
        //    return "value";
        //}

        /*
        // GET api/values/5
        //[HttpGet("GetAvatarById/{sequenceNo}/{phaseNo}")]
        [HttpGet("GetAvatarById/{id}")]
        //[HttpGet("{id}")]
        public async Task<IAvatar> Get(Guid id)
        {
            return await Program.AvatarManager.LoadAvatarAsync(id);
        }
        */

        // GET api/values/5
        //[HttpGet("{id}")]
        [HttpGet("GetAvatarByIdForProvider/{id}/{providerType}")]
        public async Task<IAvatar> Get(Guid id, ProviderType providerType)
        {
            //TODO: This will fail if the requested provider has not been registered with the ProviderManager (soon this will bn automatic with MEF if the provider dll is in the providers hot folder).
            return await AvatarManager.LoadAvatarAsync(id, providerType);
        }

        /*
        //[HttpGet("{id}")]
        [HttpGet("GetAvatarByProviderKey/{id}")]
        public async Task<IAvatar> Get(string providerKey)
        {
            return await Program.AvatarManager.LoadAvatarAsync(providerKey);
        }

        [HttpGet("GetAvatarByProviderKeyForProvider/{id}/{providerType}")]
        public async Task<IAvatar> Get(string providerKey, ProviderType providerType)
        {
            return await Program.AvatarManager.LoadAvatarAsync(providerKey, providerType);
        }
        */

        /*
        [HttpGet("GetAvatarByUsernameAndPassword/{username}/{password}")]
        public IAvatar Get(string username, string password)
        {
            //TODO: Get Async version working... 
            //return await Program.AvatarManager.LoadAvatarAsync(username, password);
            return Program.AvatarManager.LoadAvatar(username, password);
        }

        [HttpGet("GetAvatarByUsernameAndPasswordForProvider/{username}/{password}/{providerType}")]
        public async Task<IAvatar> Get(string username, string password, ProviderType providerType)
        {
            return await Program.AvatarManager.LoadAvatarAsync(username, password, providerType);
        }*/

        [HttpGet("GetAvatar/{username}/{password}")]
        //[HttpGet("GetAvatarByUsernameAndPassword/{username}/{password}")]
        //[HttpGet("/{username}/{password}")]
        public IAvatar Get(string username, string password)
        {
            //TODO: Get Async version working... 
            //return await Program.AvatarManager.LoadAvatarAsync(username, password);

          //  ActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), OASISSettings.Value.StorageProviders.DefaultProvider));
            return AvatarManager.LoadAvatar(username, password);
        }

        [HttpGet("GetAvatarForProvider/{providerType}/{username}/{password}")]
        //[HttpGet("GetAvatarByUsernameAndPasswordForProvider/{username}/{password}/{providerType}")]
        //[HttpGet("/{username}/{password}/{providerType}")]
        public async Task<IAvatar> Get(ProviderType providerType, string username, string password)
        {
            //TODO: Get Async version working... 
            //return await Program.AvatarManager.LoadAvatarAsync(username, password, providerType);

          //  ActivateProvider(providerType);
            return AvatarManager.LoadAvatar(username, password, providerType);
        }

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
