using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        internal static int SelectedNumericPartCount(Type declared)
        {
            var input = Nullable.GetUnderlyingType(declared) ?? declared;
            if (input == typeof(decimal)) return CosmicDecimalScalarResults.LimbCount;
            if (input == typeof(float)) return CosmicFloatingAggregateResults.SingleLimbCount;
            if (input == typeof(double)) return CosmicFloatingAggregateResults.DoubleLimbCount;
            if (input == typeof(int) || input == typeof(long) || input == typeof(ulong)) return 2;
            throw new NotSupportedException("An exact selected numeric word domain is required.");
        }

        internal static string SelectedNumericReduction(CosmicProjectedNumericIntent intent, string source, string rank)
        {
            var input = Nullable.GetUnderlyingType(intent.InputType) ?? intent.InputType;
            var cell = "v" + intent.ColumnIndex;
            var value = cell + "v";
            var present = cell + "s=2";
            var count = "SELECT COALESCE(sum(CASE WHEN " + present + " THEN 1 ELSE 0 END),0) AS present,";
            if (input == typeof(float) || input == typeof(double))
            {
                var function = input == typeof(float) ? SqliteCqlFloatingAggregates.SingleFunction : SqliteCqlFloatingAggregates.DoubleFunction;
                return count + function + "(CASE WHEN " + present + " THEN " + value + " ELSE NULL END) AS packet FROM " + source;
            }
            if (input == typeof(decimal))
                return count + string.Join(",", DecimalParts(value).Select((part,index) =>
                    "COALESCE(sum(CASE WHEN " + present + " THEN " + part + " ELSE 0 END),0) AS p" + index)) + " FROM " + source;
            if (input != typeof(int) && input != typeof(long) && input != typeof(ulong))
                throw new NotSupportedException("The selected numeric kernel requires an admitted exact numeric domain.");
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
