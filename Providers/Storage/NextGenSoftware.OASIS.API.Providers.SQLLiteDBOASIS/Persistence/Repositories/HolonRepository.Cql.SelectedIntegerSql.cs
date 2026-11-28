using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private static async Task<CosmicProjectedNumericPacket> ReadSelectedNumericPacketAsync(SqliteCommand numericCommand,
        CosmicProjectedNumericIntent numeric,Action check,CancellationToken cancellationToken)
    {
        var width=CqlSqlTranslator.SelectedNumericPartCount(numeric.InputType);
        var floatingInput=FloatingNumeric(numeric.InputType);
        var inputType=Nullable.GetUnderlyingType(numeric.InputType)??numeric.InputType;
        var decimalInput=inputType==typeof(decimal);
            await using var reader = await numericCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                || reader.FieldCount != (floatingInput ? 2 : decimalInput ? 1 + width : numeric.UsesInt64DoubleAverage ? 4 : 3))
                throw new InvalidDataException("Selected numeric reduction requires one exact native packet.");
            check();
            bool? prefixOverflow = null;
            if (numeric.UsesInt64DoubleAverage)
            {
                var flag = reader.GetInt64(3);
                if (flag is not (0 or 1)) throw new InvalidDataException("Checked numeric reduction requires its exact prefix acknowledgement.");
                prefixOverflow = flag == 1;
            }
            var participating = reader.GetInt64(0);
            long[] words;
            if (floatingInput)
            {
                var decoded = SqliteCqlFloatingAggregates.ReadPacket((byte[])reader.GetValue(1), inputType == typeof(float));
                if (decoded.IsError) throw new SqliteCqlNumericException(decoded.ErrorCode, decoded.Message);
                if (decoded.Result.Count != participating)
                    throw new InvalidDataException("Selected floating participation disagrees with its native packet.");
                words = decoded.Result.Parts;
            }
            else words = Enumerable.Range(1, width).Select(reader.GetInt64).ToArray();
            var packet = new CosmicProjectedNumericPacket(participating, words, prefixOverflow);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidDataException("Selected numeric reduction returned multiple packets.");
        check();return packet;
    }
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
