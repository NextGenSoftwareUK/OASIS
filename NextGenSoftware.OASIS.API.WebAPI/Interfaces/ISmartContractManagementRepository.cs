using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.WebAPI
{
    public interface ISmartContractManagementRepository
    {
        Task AddSequence(Sequence sequence);
        Task Update(Sequence sequence);
        Task Delete(string id);
        Task<Sequence> GetSequence(string id);
        Task<IEnumerable<Sequence>> GetAllSequences();
    }

}
