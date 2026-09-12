using HotChocolate.Types;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.WebAPI.GraphQL.Types
{
    public class IntentionStateType : ObjectType<IntentionState>
    {
        protected override void Configure(IObjectTypeDescriptor<IntentionState> descriptor)
        {
            descriptor.Description("The processed, human-meaningful intention/cognitive state derived from raw bio-signals.");

            descriptor.Field(f => f.Focus)
                .Description("0-1, derived from EEG alpha/beta ratio — higher means more focused/attentive.");

            descriptor.Field(f => f.EmotionalValence)
                .Description("-1 (negative) to +1 (positive) emotional valence, derived from HRV and GSR.");

            descriptor.Field(f => f.Arousal)
                .Description("0-1 arousal/activation level, derived from GSR and HRV.");

            descriptor.Field(f => f.CognitiveLoad)
                .Description("0-1 cognitive load, derived from EEG theta/beta ratio.");

            descriptor.Field(f => f.Features)
                .Description("Raw computed signal features keyed by feature name (e.g. 'eeg_alpha_power').");

            descriptor.Field(f => f.ComputedUtc)
                .Description("UTC timestamp when this intention state was computed.");
        }
    }
}
