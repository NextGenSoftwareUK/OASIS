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
//    public class SCMSDeliveryItems : ControllerBase
//    {
//        SCMSRepository _scmsRepository = new SCMSRepository();

//        [HttpGet]
//        public async Task<OASISResult<IEnumerable<DeliveryItem>>> GetAllDeliveryItems()
//        {
//            return await _scmsRepository.GetAllDeliveryItems();
//        }
//    }
//}
