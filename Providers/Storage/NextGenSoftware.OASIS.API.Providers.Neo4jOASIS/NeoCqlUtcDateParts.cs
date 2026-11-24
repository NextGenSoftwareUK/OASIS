using System;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

// The caller guards the native instant and establishes UTC before evaluating.
internal static class NeoCqlUtcDateParts
{
    internal static string Value(CosmicProjectionFunction function,string instant)
    {
        var component=function switch {
            CosmicProjectionFunction.UtcYear=>"year",CosmicProjectionFunction.UtcMonth=>"month",
            CosmicProjectionFunction.UtcDay=>"day",CosmicProjectionFunction.UtcDayOfYear=>"ordinalDay",
            CosmicProjectionFunction.UtcHour=>"hour",CosmicProjectionFunction.UtcMinute=>"minute",
            CosmicProjectionFunction.UtcSecond=>"second",
            _=>throw new NotSupportedException("UTC date part is not registered.") };
        return "("+instant+")."+component;
    }
}
