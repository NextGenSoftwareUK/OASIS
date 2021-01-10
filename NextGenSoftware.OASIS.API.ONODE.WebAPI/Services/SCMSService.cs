//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using MongoDB.Driver;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
//{
//    public class SCMSService : ISCMSService
//    {
//        SCMSRepository _smartContractRepository = new SCMSRepository();
//        private IEnumerable<Sequence> _sequences = null;

//        private void Init()
//        {

//        }


//        public async Task<IEnumerable<Sequence>> GetAllSequences()
//        {
//            _sequences = await _smartContractRepository.GetAllSequences();
//            return await Task.Run(() => _sequences.ToList());
//        }
//    }
//}

//TODO: Dont think this is used? Double check later... :)