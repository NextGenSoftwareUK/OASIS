using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class SCMSPhases : ControllerBase
    {
        SmartContractManagementRepository _smartContractRepository = new SmartContractManagementRepository();

        [HttpGet]
        public async Task<IEnumerable<Phase>> GetAllPhases()
        {
            var phases = await _smartContractRepository.GetAllPhases();
            return await Task.Run(() => phases.ToList());
        }
    }
}
