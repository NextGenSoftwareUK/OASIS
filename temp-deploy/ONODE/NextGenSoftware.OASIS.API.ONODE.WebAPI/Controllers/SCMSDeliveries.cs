//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NextGenSoftware.OASIS.API.DNA;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Repositories;
//using NextGenSoftware.OASIS.Common;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    [EnableCors]
//    public class SCMSDeliveries : OASISControllerBase
//    {
//        SCMSRepository _scmsRepository = new();

//        [HttpGet]
//        public async Task<OASISResult<IEnumerable<Delivery>>> GetAllDeliveries()
//        {
//            GetAndActivateDefaultStorageProvider();
//            return await _scmsRepository.GetAllDeliveries();
//        }

//        [HttpGet("get-all-deliveries-for-sequence-and-phase/{sequenceNo}/{phaseNo}/{loadDeliveryItems}/{loadSignedByUser}/{loadSentToPhase}/{loadFile}")]
//        public async Task<OASISResult<IEnumerable<Delivery>>> GetAllDeliveriesForSequenceAndPhase(int SequenceNo, int PhaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true)
//        {
//            GetAndActivateDefaultStorageProvider();
//            return await _scmsRepository.GetAllDeliveries(SequenceNo, PhaseNo, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadFile);
//        }
        
//        [HttpGet("get-all-deliveries-for-sequence-and-phase/{sequenceNo}/{phaseNo}")]
//        public async Task<OASISResult<IEnumerable<Delivery>>> GetAllDeliveriesForSequenceAndPhase(int SequenceNo, int PhaseNo)
//        {
//            GetAndActivateDefaultStorageProvider();
//            return await _scmsRepository.GetAllDeliveries(SequenceNo, PhaseNo);
//        }
//    }
//}
