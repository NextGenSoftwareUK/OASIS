using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ORIAServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [EnableCors()]
    public class SCMSNotes : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<IEnumerable<Note>> GetAllNotes()
        {
            return await Task.Run(() => _scmsRepository.GetAllNotes());
        }

        //[HttpGet]
        //public async Task<DeliveryItem> GetDeliveryItems(string id)
        //{
        //    return await Task.Run(() => _scmsRepository.GetDelivery(id));
        //}
    }
}
