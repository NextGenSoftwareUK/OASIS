using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Models;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Repository.Interface;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Service.Entity;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Interface;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.Services.Service
{
    public class HolonService : EntityService<Holon>, IHolonService
    {
        private IHolonRepository _HolonRepository;

        public HolonService(IHolonRepository HolonRepository) : base(HolonRepository)
        {
            this._HolonRepository = HolonRepository;
        }
    }
}