using AutoMapper;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol
{
    public class AvatarProfile : Profile
    {
        public AvatarProfile()
        {
            CreateMap<DbModel.Models.Avatar, NextGenSoftware.OASIS.API.Core.Holons.Avatar>();
        }
    }
}