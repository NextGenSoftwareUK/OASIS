using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [EnableCors()]
    public class SCMSDrawings : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<OASISResult<IEnumerable<Drawing>>> GetAllDrawings()
        {
            return new(await _scmsRepository.GetAllDrawings());
        }

        [HttpGet("GetAllDrawingsForSequenceAndPhase/{sequenceNo}/{phaseNo}/{loadPhase}/{loadFile}")]
        public async Task<OASISResult<IEnumerable<Drawing>>> GetAllDrawingsForSequenceAndPhase(int SequenceNo, int PhaseNo, bool loadPhase = false, bool loadFile = true)
        {
            return new(await _scmsRepository.GetAllDrawings(SequenceNo, PhaseNo, loadPhase, loadFile));
        }

        [HttpGet("GetAllDrawingsForSequenceAndPhase/{sequenceNo}/{phaseNo}")]
        public async Task<OASISResult<IEnumerable<Drawing>>> GetAllDrawingsForSequenceAndPhase(int SequenceNo, int PhaseNo)
        {
            return new(await _scmsRepository.GetAllDrawings(SequenceNo, PhaseNo));
        }

        //[HttpGet]
        //public async Task<DeliveryItem> GetDeliveryItems(string id)
        //{
        //    return await Task.Run(() => _scmsRepository.GetDelivery(id));
        //}
    }
}
