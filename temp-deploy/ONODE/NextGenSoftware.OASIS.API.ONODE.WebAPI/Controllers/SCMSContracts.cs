//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
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

//    [EnableCors()]
//    public class SCMSContracts : ControllerBase
//    {
//        SCMSRepository _scmsRepository = new();

//        [HttpGet]
//        public async Task<OASISResult<IEnumerable<Contract>>> GetAllContracts()
//        {
//            return await _scmsRepository.GetAllContracts();
//        }
//    }
//}
