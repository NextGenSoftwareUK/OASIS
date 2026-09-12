using HotChocolate.Types;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.Web8.WebAPI.GraphQL.Types
{
    public class MeshLinkType : ObjectType<MeshLink>
    {
        protected override void Configure(IObjectTypeDescriptor<MeshLink> descriptor)
        {
            descriptor.Description("A bidirectional link (mesh edge) between two nodes.");

            descriptor.Field(l => l.Id).Description("Unique link identifier.");
            descriptor.Field(l => l.NodeAId).Description("ID of the first node in the link.");
            descriptor.Field(l => l.NodeBId).Description("ID of the second node in the link.");
            descriptor.Field(l => l.LatencyMs).Description("Measured round-trip latency in milliseconds used for shortest-path routing.");
            descriptor.Field(l => l.IsActive).Description("Whether this link is currently active.");
        }
    }
}
