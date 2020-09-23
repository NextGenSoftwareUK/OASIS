using AutoMapper;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    public class AutoMapperProfile : Profile
    {
        // mappings between model and entity objects
        public AutoMapperProfile()
        {
            CreateMap<Avatar, AccountResponse>();

            CreateMap<Avatar, AuthenticateResponse>();

            CreateMap<RegisterRequest, Avatar>();

            CreateMap<CreateRequest, Avatar>();

            CreateMap<UpdateRequest, Avatar>()
                .ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        // ignore null & empty string properties
                        if (prop == null) return false;
                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) return false;

                        // ignore null role
                        if (x.DestinationMember.Name == "AvatarType" && src.AvatarType == null) return false;

                        return true;
                    }
                ));
        }
    }
}