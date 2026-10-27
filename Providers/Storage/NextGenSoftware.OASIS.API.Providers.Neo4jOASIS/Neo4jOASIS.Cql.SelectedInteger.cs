using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static void AppendSelectedIntegerReduction(StringBuilder text, string import, string final, int phase,
        string maximumWork, string maximumProof, CosmicProjectedNumericIntent intent, Func<object, string> bind)
    {
        var input = Nullable.GetUnderlyingType(intent.InputType) ?? intent.InputType;
        if (input != typeof(int) && input != typeof(long))
            throw new NotSupportedException("Selected integer reduction requires an admitted signed integer domain.");
        var value = "input[position].values[" + intent.ColumnIndex + "].v";
        var parts = IntegralParts(intent.InputType, "value");
        // The final position list already reflects every selected ordering/window.
        text.Append("UNION ALL WITH ").Append(import)
            .Append(" WITH input,work,proofBytes,").Append(final)
            .Append(",[position IN ").Append(final).Append(" WHERE ").Append(value)
            .Append(" IS NOT NULL | ").Append(value).Append("] AS checkedValues ")
            .Append("RETURN ").Append(phase).Append(" AS phase,-1 AS position,-1 AS sourcePosition,")
            .Append("CASE WHEN work+size(").Append(final).Append(")<=").Append(maximumWork)
            .Append(" AND proofBytes+25<=").Append(maximumProof)
            .Append(" THEN {high:reduce(total=0,value IN checkedValues | total+").Append(parts.High)
            .Append("),low:reduce(total=0,value IN checkedValues | total+").Append(parts.Low).Append(')');
        if (intent.UsesInt64DoubleAverage)
            text.Append(",prefixOverflow:").Append(CheckedInt64Prefix(bind));
        text.Append("} ELSE null END AS proof,CASE WHEN work+size(").Append(final).Append(")<=")
            .Append(maximumWork).Append(" AND proofBytes+25<=").Append(maximumProof)
            .Append(" THEN size(checkedValues) ELSE -1 END AS nativeCount,work AS stageWork ");
    }

    internal static CosmicProjectedNumericPacket ReadSelectedIntegerReduction(object raw, long count, CosmicProjectedNumericIntent intent)
    {
        var input = Nullable.GetUnderlyingType(intent.InputType) ?? intent.InputType;
        if ((input != typeof(int) && input != typeof(long)) || count < 0
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
