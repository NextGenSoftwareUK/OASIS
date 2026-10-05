//using Microsoft.AspNetCore.Cors;
//using Microsoft.AspNetCore.Mvc;
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
//    public class SCMSContacts : OASISControllerBase
//    {
//        private readonly SCMSRepository _scmsRepository = new();
        
//        [HttpGet]
//        public async Task<OASISResult<IEnumerable<Contact>>> GetAllContacts()
//        {
//            GetAndActivateDefaultStorageProvider();
//            return await _scmsRepository.GetAllContacts();
//        }

//        [HttpGet("get-all-contacts-for-sequence-and-phase/{sequenceNo}/{phaseNo}")]
//        public async Task<OASISResult<IEnumerable<Contact>>> GetAllContactsForSequenceAndPhase(int sequenceNo, int phaseNo)
//        {
//            GetAndActivateDefaultStorageProvider();
//            return await _scmsRepository.GetAllContacts(sequenceNo, phaseNo, false);
//        }

//        [HttpGet("get-all-contacts-for-sequence-and-phase/{sequenceNo}/{phaseNo}/{loadPhase}")]
//        public async Task<OASISResult<IEnumerable<Contact>>> GetAllContactsForSequenceAndPhase(int sequenceNo, int phaseNo, bool loadPhase = false)
//        {
//            GetAndActivateDefaultStorageProvider();
//            return await _scmsRepository.GetAllContacts(sequenceNo, phaseNo, loadPhase);
//        }
//    }
//}
