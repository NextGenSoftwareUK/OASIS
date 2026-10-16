using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private Task<OASISResult<Holon>> SaveNativeHolonAsync(Holon holon,bool updating,bool synchronous)
        => MongoNativeRecordWriter.SaveAsync(_dbContext.Holon,holon,updating,synchronous);
}
