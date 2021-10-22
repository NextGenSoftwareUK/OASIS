using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Repositories;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors]
    public class SCMSFiles : ControllerBase
    {
        SCMSRepository _scmsRepository = new();

        [HttpGet]
        public async Task<OASISResult<IEnumerable<File>>> GetAllFiles()
        {
            return await _scmsRepository.GetAllFiles();
        }
    }
}
