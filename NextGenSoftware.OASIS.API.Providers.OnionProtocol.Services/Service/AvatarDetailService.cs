using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Service.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Interface;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Service
{
    public class AvatarDetailService : EntityService<AvatarDetail>, IAvatarDetailService
    {
        private IAvatarDetailRepository _AvatarDetailRepository;

        public AvatarDetailService(IAvatarDetailRepository AvatarDetailRepository) : base(AvatarDetailRepository)
        {
            this._AvatarDetailRepository = AvatarDetailRepository;
        }
    }
}