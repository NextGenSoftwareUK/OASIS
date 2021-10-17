using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Service.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Interface;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Service
{
    public class AvatarService : EntityService<Avatar>, IAvatarService
    {
        private IAvatarRepository _testRepository;

        public AvatarService(IAvatarRepository AvatarRepository) : base(AvatarRepository)
        {
            this._testRepository = AvatarRepository;
        }
    }
}