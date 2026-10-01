using HotChocolate.Types;
using NextGenSoftware.OASIS.Web9.Core.Models;

namespace NextGenSoftware.OASIS.Web9.WebAPI.GraphQL.Types
{
    public class LayerStatusType : ObjectType<LayerStatus>
    {
        protected override void Configure(IObjectTypeDescriptor<LayerStatus> descriptor)
        {
            descriptor.Description("The live, real-time status of a single OASIS layer (WEB4-WEB8), as observed by an HTTP probe.");
            descriptor.Field(f => f.LayerName).Description("The name of the OASIS layer (e.g. WEB4, WEB5).");
            descriptor.Field(f => f.BaseUrl).Description("The base URL that was probed.");
            descriptor.Field(f => f.IsReachable).Description("Whether the layer responded with a success status code.");
            descriptor.Field(f => f.ResponseTimeMs).Description("Round-trip probe time in milliseconds.");
            descriptor.Field(f => f.Error).Description("Error message if the probe failed; empty otherwise.");
            descriptor.Field(f => f.CheckedUtc).Description("UTC timestamp when this layer was last probed.");
            descriptor.Field(f => f.Metrics).Description("Cheap, real metrics pulled from the layer's own endpoints where available.");
        }
    }

    public class UnifiedStatusType : ObjectType<UnifiedStatusReport>
    {
        protected override void Configure(IObjectTypeDescriptor<UnifiedStatusReport> descriptor)
        {
            descriptor.Description("The aggregate, unified view across every probed OASIS layer — the network observing itself.");
            descriptor.Field(f => f.Layers).Description("Status for each probed layer (WEB4-WEB8), ordered by layer name.").Type<ListType<LayerStatusType>>();
            descriptor.Field(f => f.AllLayersHealthy).Description("True only when every probed layer responded successfully.");
            descriptor.Field(f => f.HealthyLayerCount).Description("Number of layers that are currently reachable.");
            descriptor.Field(f => f.TotalLayerCount).Description("Total number of layers that were probed.");
            descriptor.Field(f => f.GeneratedUtc).Description("UTC timestamp when this unified report was generated.");
        }
    }
}
