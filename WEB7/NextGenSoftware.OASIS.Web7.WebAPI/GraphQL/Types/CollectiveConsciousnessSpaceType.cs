using HotChocolate.Types;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.WebAPI.GraphQL.Types
{
    public class CollectiveConsciousnessSpaceType : ObjectType<CollectiveConsciousnessSpace>
    {
        protected override void Configure(IObjectTypeDescriptor<CollectiveConsciousnessSpace> descriptor)
        {
            descriptor.Description("A shared intention field where multiple consenting symbiosis sessions co-create in real time.");

            descriptor.Field(f => f.Id)
                .Description("Unique identifier for this collective consciousness space.");

            descriptor.Field(f => f.Name)
                .Description("Human-readable name of the space.");

            descriptor.Field(f => f.ParticipantSessionIds)
                .Description("IDs of all active participant symbiosis sessions contributing to this space.");

            descriptor.Field(f => f.AggregateState)
                .Description("The aggregate (mean) intention state across every consenting participant — never any individual's raw signal.")
                .Type<IntentionStateType>();

            descriptor.Field(f => f.CreatedUtc)
                .Description("UTC timestamp when this space was created.");
        }
    }
}
