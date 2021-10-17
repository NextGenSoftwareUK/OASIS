using AutoMapper;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol
{
    public class HolonProfile : Profile
    {
        public HolonProfile()
        {
            CreateMap<DbModel.Models.Holon, NextGenSoftware.OASIS.API.Core.Holons.Holon>();
        }
    }
}