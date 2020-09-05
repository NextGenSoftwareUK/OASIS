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
    public class SCMSLinks : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<IEnumerable<Link>> GetAllLinks()
        {
            return await Task.Run(() => _scmsRepository.GetAllLinks());
        }

        [HttpGet("GetAllLinksForSequenceAndPhase/{sequenceNo}/{phaseNo}/{loadPhase}/{loadFile}")]
        public async Task<IEnumerable<Drawing>> GetAllLinksForSequenceAndPhase(int SequenceNo, int PhaseNo, bool loadPhase = false, bool loadFile = true)
        {
            return await Task.Run(() => _scmsRepository.GetAllDrawings(SequenceNo, PhaseNo, loadPhase, loadFile));
        }

        [HttpGet("GetAllLinksForSequenceAndPhase/{sequenceNo}/{phaseNo}")]
        public async Task<IEnumerable<Drawing>> GetAllLinksForSequenceAndPhase(int SequenceNo, int PhaseNo)
        {
            return await Task.Run(() => _scmsRepository.GetAllDrawings(SequenceNo, PhaseNo));
        }

        //[HttpGet]
        //public async Task<DeliveryItem> GetDeliveryItems(string id)
        //{
        //    return await Task.Run(() => _scmsRepository.GetDelivery(id));
        //}
    }
}
