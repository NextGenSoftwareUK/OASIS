using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.DNA;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    [EnableCors()]
    public class SCMSContacts : OASISControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        public SCMSContacts(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        {

        }

        [HttpGet]
        public async Task<IEnumerable<Contact>> GetAllContacts()
        {
            GetAndActivateDefaultProvider();
            return await Task.Run(() => _scmsRepository.GetAllContacts());
        }

        [HttpGet("GetAllContactsForSequenceAndPhase/{sequenceNo}/{phaseNo}")]
        //[HttpGet("/{sequenceNo}/{phaseNo}")]
        public async Task<IEnumerable<Contact>> GetAllContactsForSequenceAndPhase(int sequenceNo, int phaseNo)
        {
            GetAndActivateDefaultProvider();
            return await Task.Run(() => _scmsRepository.GetAllContacts(sequenceNo, phaseNo, false));
        }

        [HttpGet("GetAllContactsForSequenceAndPhase/{sequenceNo}/{phaseNo}/{loadPhase}")]
        //[HttpGet("/{sequenceNo}/{phaseNo}")]
        public async Task<IEnumerable<Contact>> GetAllContactsForSequenceAndPhase(int sequenceNo, int phaseNo, bool loadPhase = false)
        {
            GetAndActivateDefaultProvider();
            return await Task.Run(() => _scmsRepository.GetAllContacts(sequenceNo, phaseNo, loadPhase));
        }
    }
}
