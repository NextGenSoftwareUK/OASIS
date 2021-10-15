using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Service.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Interface;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Service
{
    public class HolonBaseService : EntityService<HolonBase>, IHolonBaseService
    {
        private IHolonBaseRepository _HolonBaseRepository;

        public HolonBaseService(IHolonBaseRepository HolonBaseRepository) : base(HolonBaseRepository)
        {
            this._HolonBaseRepository = HolonBaseRepository;
        }
    }
}