using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    //[EnableCors(origins: "http://mywebclient.azurewebsites.net", headers: "*", methods: "*")]
    [EnableCors()]
    public class SCMSSequences : ControllerBase
    {
        SCMSRepository _scmsRepository = new SCMSRepository();
        //private IEnumerable<Sequence> _sequences = null;

        //  private ISmartContractManagementService _smartContractManagementService;

        //public SmartContractManagementController(ISmartContractManagementService smartContractService)
        //{
        //    _smartContractManagementService = smartContractService;
        //}


        
    //[HttpGet("{GetAllSequences}")]
    [HttpGet]
    //[HttpGet("GetAllSequences")]
    //public async Task<IActionResult> GetAllSequences()
    //public async Task<IActionResult> Get()
    public async Task<OASISResult<IEnumerable<Sequence>>> GetAllSequences()
   {
       return new(await _scmsRepository.GetAllSequences());
   }

        /*
    //[HttpGet("{GetAllPhases}")]
    [HttpGet]
    [Route("[action]/{phases}")]
    //[HttpGet("GetAllSequences")]
    //public async Task<IActionResult> GetAllSequences()
    //public async Task<IActionResult> Get()
    public async Task<IEnumerable<Phase>> GetAllPhases()
    {
        //var sequences = await _smartContractManagementService.GetAllSequences();

        var phases = await _smartContractRepository.GetAllPhases();
        return await Task.Run(() => phases.ToList());

        //return sequences;
    }*/

        /*
        [HttpGet("{smartcontractmanagement}")]
        public async Task<ISearchResults> Get(string search)
        {
            SearchResults result = new SearchResults();
            result.SearchResult = new List<string> { "boo!" };

            return result;
        }*/

        //[HttpGet("{smartcontractmanagement}")]
        ////[HttpGet("GetAllSequences")]
        ////public async Task<IActionResult> GetAllSequences()
        ////public async Task<IActionResult> Get()
        //public async Task<string> Get()
        //{
        //    //var sequences = await _smartContractManagementService.GetAllSequences();

        //    return "BOO!";

        //    //return sequences;
        //}
    }
}
