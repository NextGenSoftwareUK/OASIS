using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal static int SelectedNumericWidth(Type declared)
    {
        var input = Nullable.GetUnderlyingType(declared) ?? declared;
        return input == typeof(decimal) ? CosmicDecimalScalarResults.LimbCount
            : input == typeof(float) ? CosmicFloatingAggregateResults.SingleLimbCount
            : input == typeof(double) ? CosmicFloatingAggregateResults.DoubleLimbCount : 2;
    }

    private static void AppendSelectedIntegerReduction(StringBuilder text, string import, string final, int phase,
        string maximumWork, string maximumProof, CosmicProjectedNumericIntent intent, Func<object, string> bind)
    {
        var input = Nullable.GetUnderlyingType(intent.InputType) ?? intent.InputType;
        if (input != typeof(int) && input != typeof(long) && input != typeof(ulong) && input != typeof(decimal) && input != typeof(float) && input != typeof(double))
            throw new NotSupportedException("Selected integer reduction requires an admitted exact numeric domain.");
        var value = "input[position].values[" + intent.ColumnIndex + "].v";
        var wide = input == typeof(decimal) || input == typeof(float) || input == typeof(double);
        var width = SelectedNumericWidth(input);
        var packetBytes = 9 + 8 * width;
        var parts = wide ? default : IntegralParts(intent.InputType, "value");
        var nativeParts = input == typeof(decimal) ? DecimalAggregateParts("value")
            : wide ? FloatingAggregateParts("value", input == typeof(float), bind) : null;
        // The final position list already reflects every selected ordering/window.
        text.Append("UNION ALL WITH ").Append(import)
            .Append(" WITH input,work,proofBytes,").Append(final)
            .Append(",[position IN ").Append(final).Append(" WHERE ").Append(value)
            .Append(" IS NOT NULL | ").Append(value).Append("] AS checkedValues ");
        if (wide) text.Append("WITH *,[value IN checkedValues | ").Append(nativeParts).Append("] AS numericParts ");
        text.Append("RETURN ").Append(phase).Append(" AS phase,-1 AS position,-1 AS sourcePosition,")
            .Append("CASE WHEN work+size(").Append(final).Append(")<=").Append(maximumWork)
            .Append(" AND proofBytes+").Append(packetBytes).Append("<=").Append(maximumProof)
            .Append(" THEN {");
        if (wide)
            text.Append("parts:[index IN range(0,").Append(width - 1)
                .Append(") | reduce(total=0,words IN numericParts | total+words[index])]");
        else text.Append("high:reduce(total=0,value IN checkedValues | total+").Append(parts.High)
            .Append("),low:reduce(total=0,value IN checkedValues | total+").Append(parts.Low).Append(')');
        if (intent.UsesInt64DoubleAverage)
            text.Append(",prefixOverflow:").Append(CheckedInt64Prefix(bind));
        text.Append("} ELSE null END AS proof,CASE WHEN work+size(").Append(final).Append(")<=")
            .Append(maximumWork).Append(" AND proofBytes+").Append(packetBytes).Append("<=").Append(maximumProof)
            .Append(" THEN size(checkedValues) ELSE -1 END AS nativeCount,work AS stageWork ");
    }

    internal static CosmicProjectedNumericPacket ReadSelectedIntegerReduction(object raw, long count, CosmicProjectedNumericIntent intent)
    {
        var input = Nullable.GetUnderlyingType(intent.InputType) ?? intent.InputType;
        if (input == typeof(decimal) || input == typeof(float) || input == typeof(double))
        {
            if (count < 0 || raw is not IReadOnlyDictionary<string, object> wide || wide.Count != 1
                || !wide.TryGetValue("parts", out var words))
                throw new InvalidDataException("Selected numeric reduction requires its exact domain word/count schema.");
            var decoded = input == typeof(decimal) ? ReadDecimalAggregateParts(words)
                : ReadFloatingAggregateParts(words, SelectedNumericWidth(input));
            return new CosmicProjectedNumericPacket(count, decoded);
        }
        if ((input != typeof(int) && input != typeof(long) && input != typeof(ulong)) || count < 0
            || raw is not IReadOnlyDictionary<string, object> packet
            || packet.Count != (intent.UsesInt64DoubleAverage ? 3 : 2)
            || !packet.TryGetValue("high", out var high) || high is not long highWord
            || !packet.TryGetValue("low", out var low) || low is not long lowWord)
            throw new InvalidDataException("Selected integer reduction requires its exact native word/count schema.");
        bool? overflow = null;
        if (intent.UsesInt64DoubleAverage)
        {
            if (!packet.TryGetValue("prefixOverflow", out var flag) || flag is not bool)
                throw new InvalidDataException("Selected Int64 mean requires its Boolean checked-prefix acknowledgement.");
            overflow = (bool)flag;
        }
        return new CosmicProjectedNumericPacket(count, new[] { highWord, lowWord }, overflow);
    }
}
