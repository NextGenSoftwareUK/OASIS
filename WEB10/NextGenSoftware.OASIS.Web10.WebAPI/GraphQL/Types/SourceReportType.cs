using HotChocolate.Types;
using NextGenSoftware.OASIS.Web10.Core.Models;

namespace NextGenSoftware.OASIS.Web10.WebAPI.GraphQL.Types
{
    public class SourceReportType : ObjectType<SourceReport>
    {
        protected override void Configure(IObjectTypeDescriptor<SourceReport> descriptor)
        {
            descriptor.Description(
                "WEB10 = WEB0 made literal: the foundational runtime/version identity " +
                "together with the live unified status across WEB4-WEB8 — the Alpha and the Omega.");

            descriptor.Field(r => r.OasisRuntimeVersion)
                .Description("The OASIS runtime version running at the foundation.");

            descriptor.Field(r => r.OasisApiVersion)
                .Description("The OASIS API version.");

            descriptor.Field(r => r.StarApiVersion)
                .Description("The STAR API (ODK) version.");

            descriptor.Field(r => r.UnifiedStatus)
                .Description("Live aggregate health status of every layer above the foundation (WEB4-WEB8), as reported by WEB9.");

            descriptor.Field(r => r.GeneratedUtc)
                .Description("UTC timestamp when this source report was generated.");
        }
    }
}
