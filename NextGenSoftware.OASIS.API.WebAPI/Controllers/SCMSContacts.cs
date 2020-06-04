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
    public class SCMSContacts : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<IEnumerable<Contact>> GetAllContacts()
        {
            return await Task.Run(() => _scmsRepository.GetAllContacts());
        }

        [HttpGet("GetAllContacts/{sequenceNo}/{phaseNo}")]
        //[HttpGet("/{sequenceNo}/{phaseNo}")]
        public async Task<IEnumerable<Contact>> GetAllContacts(int SequenceNo, int PhaseNo)
        {
            return await Task.Run(() => _scmsRepository.GetAllContacts(SequenceNo, PhaseNo));
        }
    }
}
