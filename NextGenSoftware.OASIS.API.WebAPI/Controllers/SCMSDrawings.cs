using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [EnableCors()]
    public class SCMSDrawings : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<IEnumerable<Drawing>> GetAllDrawings()
        {
            return await Task.Run(() => _scmsRepository.GetAllDrawings());
        }

        [HttpGet("GetAllDrawingsForSequenceAndPhase/{sequenceNo}/{phaseNo}/{includePhaseObject}/{includeFileObject}")]
        public async Task<IEnumerable<Drawing>> GetAllDrawingsForSequenceAndPhase(int SequenceNo, int PhaseNo, bool includePhaseObject = false, bool includeFileObject = true)
        {
            return await Task.Run(() => _scmsRepository.GetAllDrawings(SequenceNo, PhaseNo, includePhaseObject, includeFileObject));
        }

        [HttpGet("GetAllDrawingsForSequenceAndPhase/{sequenceNo}/{phaseNo}")]
        public async Task<IEnumerable<Drawing>> GetAllDrawingsForSequenceAndPhase(int SequenceNo, int PhaseNo)
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
