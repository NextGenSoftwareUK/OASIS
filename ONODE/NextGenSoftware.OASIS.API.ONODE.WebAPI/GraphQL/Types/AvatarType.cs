using HotChocolate.Types;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL.Types
{
    public class AvatarType : ObjectType<IAvatar>
    {
        protected override void Configure(IObjectTypeDescriptor<IAvatar> descriptor)
        {
            descriptor.Name("Avatar");

            descriptor.Field(a => a.AvatarId).Name("id");
            descriptor.Field(a => a.Username).Name("username");
            descriptor.Field(a => a.Email).Name("email");
            descriptor.Field(a => a.FirstName).Name("firstName");
            descriptor.Field(a => a.LastName).Name("lastName");
            descriptor.Field(a => a.FullName).Name("fullName");
            descriptor.Field(a => a.Title).Name("title");
            descriptor.Field(a => a.IsVerified).Name("isVerified");
            descriptor.Field(a => a.IsBeamedIn).Name("isBeamedIn");
            descriptor.Field(a => a.DID).Name("did");

            descriptor.Field("avatarType")
                .Resolve(ctx => ctx.Parent<IAvatar>().AvatarType?.Value.ToString());
        }
    }
}
