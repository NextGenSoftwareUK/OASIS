using AutoMapper;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol
{
    public class AvatarDetailProfile : Profile
    {
        public AvatarDetailProfile()
        {
            CreateMap<DbModel.Models.AvatarDetail, NextGenSoftware.OASIS.API.Core.Holons.AvatarDetail>();
        }
    }
}