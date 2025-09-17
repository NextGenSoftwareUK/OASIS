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
//    [EnableCors]
//    public class SCMSLinks : ControllerBase
//    {
//        SCMSRepository _scmsRepository = new();

//        [HttpGet]
//        public async Task<OASISResult<IEnumerable<Link>>> GetAllLinks()
//        {
//            return await _scmsRepository.GetAllLinks();
//        }

//        [HttpGet("get-all-links-for-sequence-and-phase/{sequenceNo}/{phaseNo}/{loadPhase}/{loadFile}")]
//        public async Task<OASISResult<IEnumerable<Drawing>>> GetAllLinksForSequenceAndPhase(int SequenceNo, int PhaseNo, bool loadPhase = false, bool loadFile = true)
//        {
//            return await _scmsRepository.GetAllDrawings(SequenceNo, PhaseNo, loadPhase, loadFile);
//        }

//        [HttpGet("get-all-links-for-sequence-and-phase/{sequenceNo}/{phaseNo}")]
//        public async Task<OASISResult<IEnumerable<Drawing>>> GetAllLinksForSequenceAndPhase(int SequenceNo, int PhaseNo)
//        {
//            return await _scmsRepository.GetAllDrawings(SequenceNo, PhaseNo);
//        }
//    }
//}
