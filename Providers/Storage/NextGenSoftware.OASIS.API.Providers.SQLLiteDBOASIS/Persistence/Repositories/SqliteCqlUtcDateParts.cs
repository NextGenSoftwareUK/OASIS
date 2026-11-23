using System;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// Input is an already guarded UTC calendar SQL expression, not application SQL.
internal static class SqliteCqlUtcDateParts
{
    internal static string Value(CosmicProjectionFunction function,string source)
    {
        var component=function switch {
            CosmicProjectionFunction.UtcYear=>"substr("+source+",1,4)",
            CosmicProjectionFunction.UtcMonth=>"substr("+source+",6,2)",
            CosmicProjectionFunction.UtcDay=>"substr("+source+",9,2)",
            CosmicProjectionFunction.UtcDayOfYear=>"strftime('%j',substr("+source+",1,10))",
            CosmicProjectionFunction.UtcHour=>"substr("+source+",12,2)",
            CosmicProjectionFunction.UtcMinute=>"substr("+source+",15,2)",
            CosmicProjectionFunction.UtcSecond=>"substr("+source+",18,2)",
            _=>throw new NotSupportedException("UTC date part is not registered.") };
        return "CAST("+component+" AS INTEGER)";
    }
}
