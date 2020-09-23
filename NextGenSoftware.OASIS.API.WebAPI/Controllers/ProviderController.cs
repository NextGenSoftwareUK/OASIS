using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProviderController : OASISControllerBase
    {
        public ProviderController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {

        }

        [Authorize(AvatarType.User)]
        [HttpGet]
        public ActionResult<IEnumerable<IOASISProvider>> GetAll()
        {
            return Ok(ProviderManager.GetAllProviders());
        }

        [Authorize(AvatarType.User)]
        [HttpGet]
        public ActionResult<IOASISProvider> SetCurrentStorageProvider(IOASISProvider provider)
        {
            return Ok(ProviderManager.SetAndActivateCurrentStorageProvider(provider));
        }
    }
}
