using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public interface ISmartContractManagementService
    {
        Task<IEnumerable<Sequence>> GetAllSequences();
    }
}
                                                                                                   