using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Service.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Interface;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Service
{
    public class TestService : EntityService<Test>, ITestService
    {
        private ITestRepository _testRepository;

        public TestService(ITestRepository testRepository) : base(testRepository)
        {
            this._testRepository = testRepository;
        }
    }
}