using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Repository
{
    public class TestRepository : EntityRepository<Test, IEntityProvider<Test>>, ITestRepository
    {
        private IEntityProvider<Test> _testProvider;

        public TestRepository(IEntityProvider<Test> testProvider) : base(testProvider)
        {
            this._testProvider = testProvider;
        }
    }
}