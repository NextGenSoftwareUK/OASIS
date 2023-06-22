using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONode.WebAPI.Repositories;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors]
    public class SCMSSequences : ControllerBase
    {
        private SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<OASISResult<IEnumerable<Sequence>>> GetAllSequences()
        {
            return await _scmsRepository.GetAllSequences();
        }
    }
}