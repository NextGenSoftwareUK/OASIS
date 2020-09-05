using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ORIAServices
{
    public interface ISCMSService
    {
        Task<IEnumerable<Sequence>> GetAllSequences();
    }
}
                                                                                                   