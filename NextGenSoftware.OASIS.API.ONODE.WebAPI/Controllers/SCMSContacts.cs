using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [EnableCors()]
    public class SCMSContacts : OASISControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        public SCMSContacts() : base()
        {

        }

        [HttpGet]
        public async Task<OASISResult<IEnumerable<Contact>>> GetAllContacts()
        {
            GetAndActivateDefaultProvider();
            return new(await _scmsRepository.GetAllContacts());
        }

        [HttpGet("GetAllContactsForSequenceAndPhase/{sequenceNo}/{phaseNo}")]
        //[HttpGet("/{sequenceNo}/{phaseNo}")]
        public async Task<OASISResult<IEnumerable<Contact>>> GetAllContactsForSequenceAndPhase(int sequenceNo, int phaseNo)
        {
            GetAndActivateDefaultProvider();
            return new(await _scmsRepository.GetAllContacts(sequenceNo, phaseNo, false));
        }

        [HttpGet("GetAllContactsForSequenceAndPhase/{sequenceNo}/{phaseNo}/{loadPhase}")]
        //[HttpGet("/{sequenceNo}/{phaseNo}")]
        public async Task<OASISResult<IEnumerable<Contact>>> GetAllContactsForSequenceAndPhase(int sequenceNo, int phaseNo, bool loadPhase = false)
        {
            GetAndActivateDefaultProvider();
            return new(await _scmsRepository.GetAllContacts(sequenceNo, phaseNo, loadPhase));
        }
    }
}
