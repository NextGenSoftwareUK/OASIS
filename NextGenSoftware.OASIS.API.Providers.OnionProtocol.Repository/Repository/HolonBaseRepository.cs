using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Repository
{
    public class HolonBaseRepository : EntityRepository<HolonBase, IEntityProvider<HolonBase>>, IHolonBaseRepository
    {
        private IEntityProvider<HolonBase> _testProvider;

        public HolonBaseRepository(IEntityProvider<HolonBase> testProvider) : base(testProvider)
        {
            this._testProvider = testProvider;
        }
    }
}