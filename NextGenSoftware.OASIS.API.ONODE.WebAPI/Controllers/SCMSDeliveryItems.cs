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

    [EnableCors()]
    public class SCMSDeliveryItems : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<OASISResult<IEnumerable<DeliveryItem>>> GetAllDeliveryItems()
        {
            return await _scmsRepository.GetAllDeliveryItems();
        }
    }
}
