using HotChocolate.Types;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.Web8.WebAPI.GraphQL.Types
{
    public class MeshNodeType : ObjectType<GalacticNode>
    {
        protected override void Configure(IObjectTypeDescriptor<GalacticNode> descriptor)
        {
            descriptor.Description("A registered node in the WEB8 fractal holonic mesh.");

            descriptor.Field(n => n.Id).Description("Unique node identifier.");
            descriptor.Field(n => n.Name).Description("Human-readable node name.");
            descriptor.Field(n => n.Type).Description("Node type (e.g. Peer, Gateway, Bridge).");
            descriptor.Field(n => n.EndpointUrl).Description("HTTP endpoint this node receives relayed mesh messages at.");
            descriptor.Field(n => n.IsSovereign).Description("Whether the node retains full sovereignty within the mesh.");
            descriptor.Field(n => n.RegisteredUtc).Description("UTC timestamp when the node was registered.");
            descriptor.Field(n => n.LastSeenUtc).Description("UTC timestamp of the node's most recent heartbeat.");
        }
    }
}
