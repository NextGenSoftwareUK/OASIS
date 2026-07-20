using HotChocolate.Types;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL.Types
{
    public class HolonType : ObjectType<IHolon>
    {
        protected override void Configure(IObjectTypeDescriptor<IHolon> descriptor)
        {
            descriptor.Name("Holon");

            descriptor.Field(h => h.Id).Name("id");
            descriptor.Field(h => h.Name).Name("name");
            descriptor.Field(h => h.Description).Name("description");

            descriptor.Field("holonType")
                .Resolve(ctx => ctx.Parent<IHolon>().HolonType.ToString());
        }
    }
}
