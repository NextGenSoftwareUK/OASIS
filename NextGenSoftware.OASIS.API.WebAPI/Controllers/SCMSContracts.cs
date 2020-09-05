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
    public class SCMSContracts : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();

        [HttpGet]
        public async Task<IEnumerable<Contract>> GetAllContracts()
        {
            return await Task.Run(() => _scmsRepository.GetAllContracts());
        }
    }
}
