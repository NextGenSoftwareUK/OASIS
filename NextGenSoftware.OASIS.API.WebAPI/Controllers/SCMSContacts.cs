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
    public class SCMSContacts : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<IEnumerable<Contact>> GetAllContacts()
        {
            return await Task.Run(() => _scmsRepository.GetAllContacts());
        }

        [HttpGet("GetAllContactsForSequenceAndPhase/{sequenceNo}/{phaseNo}")]
        //[HttpGet("/{sequenceNo}/{phaseNo}")]
        public async Task<IEnumerable<Contact>> GetAllContactsForSequenceAndPhase(int sequenceNo, int phaseNo)
        {
            return await Task.Run(() => _scmsRepository.GetAllContacts(sequenceNo, phaseNo, false));
        }

        [HttpGet("GetAllContactsForSequenceAndPhase/{sequenceNo}/{phaseNo}/{loadPhase}")]
        //[HttpGet("/{sequenceNo}/{phaseNo}")]
        public async Task<IEnumerable<Contact>> GetAllContactsForSequenceAndPhase(int sequenceNo, int phaseNo, bool loadPhase = false)
        {
            return await Task.Run(() => _scmsRepository.GetAllContacts(sequenceNo, phaseNo, loadPhase));
        }
    }
}
