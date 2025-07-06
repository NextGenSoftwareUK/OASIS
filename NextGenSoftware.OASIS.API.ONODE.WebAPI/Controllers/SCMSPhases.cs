//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
//using NextGenSoftware.OASIS.API.Core;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Repositories;
//using NextGenSoftware.OASIS.Common;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [EnableCors]
//    public class SCMSPhases : ControllerBase
//    {
//        SCMSRepository _scmsRepository = new();

//        [HttpGet]
//        public async Task<OASISResult<IEnumerable<Phase>>> GetAllPhases()
//        {
//            return await _scmsRepository.GetAllPhases();
//        }
//    }
//}
