using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONode.WebAPI.Repositories;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [EnableCors]
    public class SCMSMaterials : ControllerBase
    {
        SCMSRepository _scmsRepository = new();

        [HttpGet]
        public async Task<OASISResult<IEnumerable<Material>>> GetAllMaterials()
        {
            return await _scmsRepository.GetAllMaterials();
        }
    }
}
