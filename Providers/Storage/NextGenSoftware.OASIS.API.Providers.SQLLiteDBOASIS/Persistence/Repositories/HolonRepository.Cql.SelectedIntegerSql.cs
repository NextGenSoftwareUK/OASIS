using System;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        internal static string SelectedIntegerReduction(CosmicProjectedNumericIntent intent, string source, string rank)
        {
            var input = Nullable.GetUnderlyingType(intent.InputType) ?? intent.InputType;
            if (input != typeof(int) && input != typeof(long))
                throw new NotSupportedException("The selected integer kernel requires an admitted signed integer domain.");
            var cell = "v" + intent.ColumnIndex;
            var value = cell + "v";
            var present = cell + "s=2";
            var parts = IntegralParts(intent.InputType, value);
            return "SELECT COALESCE(sum(CASE WHEN " + present + " THEN 1 ELSE 0 END),0) AS present,"
                + "COALESCE(sum(CASE WHEN " + present + " THEN " + parts.High + " ELSE 0 END),0) AS high,"
                + "COALESCE(sum(CASE WHEN " + present + " THEN " + parts.Low + " ELSE 0 END),0) AS low"
                + (intent.UsesInt64DoubleAverage ? "," + SqliteCqlCheckedInt64Average.Function
                    + "(CASE WHEN " + present + " THEN " + value + " ELSE NULL END ORDER BY " + rank + ") AS prefixOverflow" : "")
                + " FROM " + source;
        }
    }
}
