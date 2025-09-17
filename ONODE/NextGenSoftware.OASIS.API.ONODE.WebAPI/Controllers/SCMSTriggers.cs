//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Repositories;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [EnableCors]
//    public class SCMSTriggers : ControllerBase
//    {
//        SCMSRepository _scmsRepository = new();
        
//        [HttpGet]
//        public async Task<Common.OASISResult<IEnumerable<Trigger>>> GetAllTriggers()
//        {
//            return await _scmsRepository.GetAllTriggers();
//        }
//    }
//}
