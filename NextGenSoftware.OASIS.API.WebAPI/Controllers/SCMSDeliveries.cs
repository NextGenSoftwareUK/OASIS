using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [EnableCors()]
    public class SCMSDeliveries : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<IEnumerable<Delivery>> GetAllDeliveries()
        {
            return await Task.Run(() => _scmsRepository.GetAllDeliveries());
        }

        [HttpGet("GetAllDeliveriesForSequenceAndPhase/{sequenceNo}/{phaseNo}/{loadDeliveryItems}/{loadSignedByUser}/{loadSentToPhase}/{loadFile}")]
        public async Task<IEnumerable<Delivery>> GetAllDeliveriesForSequenceAndPhase(int SequenceNo, int PhaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadFile = true)
        {
            //return await Task.Run(() => _scmsRepository.GetAllDeliveries(SequenceNo, PhaseNo, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadMaterial, loadFile));
            return await Task.Run(() => _scmsRepository.GetAllDeliveries(SequenceNo, PhaseNo, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadFile));
        }

        /*
        [HttpGet("GetAllDeliveriesForSequenceAndPhase/{sequenceNo}/{phaseNo}/{loadDeliveryItems}/{loadSignedByUser}/{loadSentToPhase}/{loadMaterial}/{loadFile}")]
        public async Task<IEnumerable<Delivery>> GetAllDeliveriesForSequenceAndPhase(int SequenceNo, int PhaseNo, bool loadDeliveryItems = true, bool loadSignedByUser = true, bool loadSentToPhase = true, bool loadMaterial = true, bool loadFile = true)
        {
            return await Task.Run(() => _scmsRepository.GetAllDeliveries(SequenceNo, PhaseNo, loadDeliveryItems, loadSignedByUser, loadSentToPhase, loadMaterial, loadFile));
        }
        */

        [HttpGet("GetAllDeliveriesForSequenceAndPhase/{sequenceNo}/{phaseNo}")]
        public async Task<IEnumerable<Delivery>> GetAllDeliveriesForSequenceAndPhase(int SequenceNo, int PhaseNo)
        {
            return await Task.Run(() => _scmsRepository.GetAllDeliveries(SequenceNo, PhaseNo));
        }

        //[HttpGet]
        //public async Task<Delivery> GetAllDelivery(string id)
        //{
        //    return await Task.Run(() => _scmsRepository.GetDelivery(id));
        //}
    }
}
