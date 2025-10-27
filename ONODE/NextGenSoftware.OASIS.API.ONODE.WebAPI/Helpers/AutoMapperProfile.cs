using AutoMapper;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    public class AutoMapperProfile : Profile
    {
        // mappings between model and entity objects
        public AutoMapperProfile()
        {
            //CreateMap<Core.Avatar, AccountResponse>();
            //CreateMap<Core.Avatar, AuthenticateResponse>();
            //CreateMap<IAvatar, AccountResponse>();
            //CreateMap<IAvatar, AuthenticateResponse>();

            //CreateMap<RegisterRequest, Core.Avatar>();
            //CreateMap<CreateRequest, Core.Avatar>();
            //CreateMap<RegisterRequest, IAvatar>();
            CreateMap<CreateRequest, IAvatar>();
            CreateMap<API.Core.Holons.Avatar, IAvatar>();
            
            // Avatar Session mappings
            CreateMap<API.Core.Objects.Avatar.AvatarSession, Models.Avatar.AvatarSession>();
            CreateMap<Models.Avatar.AvatarSession, API.Core.Objects.Avatar.AvatarSession>();
            CreateMap<API.Core.Objects.Avatar.AvatarSessionStats, Models.Avatar.AvatarSessionStats>();
            CreateMap<Models.Avatar.AvatarSessionStats, API.Core.Objects.Avatar.AvatarSessionStats>();

            CreateMap<UpdateRequest, IAvatar>()
                .ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        // ignore null & empty string properties
                        if (prop == null) 
                            return false;

                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop)) 
                            return false;

                        // ignore null AvatarType
                        if (x.DestinationMember.Name == "AvatarType" && src.AvatarType == null) 
                            return false;

                        return true;
                    }
                ));

            CreateMap<RegisterRequest, IAvatar>()
                .ForAllMembers(x => x.Condition(
                    (src, dest, prop) =>
                    {
                        // ignore null & empty string properties
                        if (prop == null)
                            return false;

                        if (prop.GetType() == typeof(string) && string.IsNullOrEmpty((string)prop))
                            return false;

                        // ignore null AvatarType
                        if (x.DestinationMember.Name == "AvatarType" && src.AvatarType == null)
                            return false;

                        //x.Ignore()

                        if (x.DestinationMember.Name == "KarmaEarnt")
                            return false;

                        return true;
                    }
                ));
        }
    }
}