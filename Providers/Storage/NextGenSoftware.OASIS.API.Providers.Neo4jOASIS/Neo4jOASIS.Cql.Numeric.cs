using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal sealed class NeoCqlNumericValue
    {
        internal long High { get; }
        internal long Low { get; }
        internal long Count { get; }
        internal long[] DecimalParts { get; }
        internal long[] FloatingParts { get; }

        internal NeoCqlNumericValue(long[] parts, long count, bool floating = false)
        {
            if (floating) FloatingParts = (long[])parts.Clone(); else DecimalParts = (long[])parts.Clone();
            Count = count;
        }

        internal NeoCqlNumericValue(long high, long low, long count)
        {
            High = high;
            Low = low;
            Count = count;
        }
    }

    internal static NeoCqlNumericValue DecodeCqlNumeric(IReadOnlyDictionary<string, object> row, bool decimalInput = false, bool required = false, int floatingWidth = 0)

    {
        if (floatingWidth != 0 && floatingWidth != CosmicFloatingAggregateResults.SingleLimbCount
            && floatingWidth != CosmicFloatingAggregateResults.DoubleLimbCount || floatingWidth != 0 && decimalInput)
            throw new InvalidDataException("One explicit numeric packet domain is required.");
        if (row.Count != (decimalInput || floatingWidth != 0 ? 6 : 7) || !row.TryGetValue("present", out var present) || present is not long count
            || !row.TryGetValue("inputs", out var inputs) || inputs is not long work
            || !row.TryGetValue("valid", out var valid) || valid is not bool
            || !row.TryGetValue("traversalCycle", out var cycle) || cycle is not bool
            || !row.TryGetValue("traversalLimit", out var limit) || limit is not bool
            || count < 0 || work < 0 || count > work || required && count != work)
            throw new InvalidDataException("Neo4j numeric reduction requires exact integer limbs and consistent counts.");
        if (work > CosmicNumericScalarResults.MaximumParticipatingValues)
            throw new NeoCqlLimitException("Neo4j numeric reduction exceeds its input-work budget.");
        if (floatingWidth != 0)
        {
            if (!row.TryGetValue("parts", out var raw) || raw is not IReadOnlyList<object> parts
                || parts.Count != floatingWidth || parts.Any(part => part is not long))
                throw new InvalidDataException("Floating reduction requires its exact declared Int64 word array.");
            return new NeoCqlNumericValue(parts.Cast<long>().ToArray(), count, floating: true);
        }
        if (decimalInput)
        {
            if (!row.TryGetValue("parts", out var rawParts)) throw new InvalidDataException("Missing Decimal limbs.");
            return new NeoCqlNumericValue(ReadDecimalAggregateParts(rawParts), count);
        }
        if (!row.TryGetValue("high", out var high) || high is not long highInteger
            || !row.TryGetValue("low", out var low) || low is not long lowInteger)
            throw new InvalidDataException("Integer reduction requires exact Int64 limbs.");
        return new NeoCqlNumericValue(highInteger, lowInteger, count);
    }

    private static long[] ReadDecimalAggregateParts(object raw)
    {
        if (raw is not IReadOnlyList<object> parts || parts.Count != CosmicDecimalScalarResults.LimbCount
            || parts.Any(part => part is not long))
            throw new InvalidDataException("Decimal reduction requires seven exact Int64 limb sums.");
        return parts.Cast<long>().ToArray();
    }

    private static (string High, string Low) IntegralParts(Type declared, string value)
    {
        var type = Nullable.GetUnderlyingType(declared) ?? declared;
        if (type == typeof(ulong))
        {
            // The canonical key has exactly20 digits. Each decimal chunk and
            // upper*1410065408+lower fit Int64, even at UInt64.MaxValue.
            var upper = "toInteger(substring(" + value + ",0,10))";
            var lower = "toInteger(substring(" + value + ",10,10))";
            var remainder = "(" + upper + "*1410065408+" + lower + ")";
            return ("(" + upper + "*2+" + remainder + "/4294967296)", "(" + remainder + "%4294967296)");
        }
        if (!type.IsEnum && type != typeof(byte) && type != typeof(sbyte) && type != typeof(short)
            && type != typeof(ushort) && type != typeof(int) && type != typeof(uint) && type != typeof(long))
            throw new NotSupportedException("Native integral parts require an exact integer domain.");
        return ("(" + value + "/4294967296)", "(" + value + "%4294967296)");
    }

    private static string CompileCqlNumeric<T, TResult>(NeoCqlSource compiled, CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        if (query.Selector == null || (query.Operation != CosmicScalarOperation.Sum && query.Operation != CosmicScalarOperation.Average))
            throw new NotSupportedException("An admitted numeric aggregate is required.");
        var translator = compiled.Translator;
        var cell = translator.TranslateProjectionPath(query.Selector);
        var inputType = Nullable.GetUnderlyingType(query.Selector.ValueType) ?? query.Selector.ValueType;
        var decimalInput = inputType == typeof(decimal);
        var floating = inputType == typeof(float) || inputType == typeof(double);
        var wide = decimalInput || floating;
        var width = decimalInput ? CosmicDecimalScalarResults.LimbCount
            : inputType == typeof(float) ? CosmicFloatingAggregateResults.SingleLimbCount
            : inputType == typeof(double) ? CosmicFloatingAggregateResults.DoubleLimbCount : 0;
        var parts = wide ? default : IntegralParts(query.Selector.ValueType, "selectedValue");
        var fields = wide ? "parts,present,inputs" : "high,low,present,inputs";
        var reductions = wide
            ? string.Join(",", Enumerable.Range(0, width).Select(index => "sum(numericParts[" + index + "]) AS d" + index))
            : "sum(CASE WHEN selectedState=2 THEN " + parts.High + " ELSE 0 END) AS high,"
                + "sum(CASE WHEN selectedState=2 THEN " + parts.Low + " ELSE 0 END) AS low";
        var reduced = wide ? "[" + string.Join(",", Enumerable.Range(0, width).Select(index => "d" + index))
            + "] AS parts,present,inputs" : fields;
        var zeroParts = "[" + string.Join(",", Enumerable.Repeat("0", width)) + "]";
        var nativeParts = floating ? FloatingAggregateParts("selectedValue", inputType == typeof(float), translator.Bind)
            : decimalInput ? DecimalAggregateParts("selectedValue") : null;
        var keys = translator.TranslateOrdering(query.Source.Orderings);
        var aliases = keys.Select((_, index) => "cqlNumericSort" + index).ToArray();
        var keyValues = string.Concat(keys.Select((key, index) => "," + key.Expression + " AS " + aliases[index]));
        var order = string.Join(",", keys.Select((key, index) => aliases[index] + (key.Descending ? " DESC" : " ASC"))
            .Concat(new[] { "p.f_Id ASC" }));
        translator.Parameters.Add("numericOffset", (long)query.Source.Offset);
        // Include one detection row. It is never published as a truncated sum.
        translator.Parameters.Add("numericTake", Math.Min(query.Source.Limit.HasValue ? (long)query.Source.Limit.Value : long.MaxValue,
            (long)CosmicNumericScalarResults.MaximumParticipatingValues + 1));
        var invalid = Nullable.GetUnderlyingType(query.Selector.ValueType) == null ? "selectedState <> 2" : "selectedState = 3";
        var zeros = (wide ? zeroParts + " AS parts" : "0 AS high,0 AS low") + ",0 AS present,0 AS inputs";
        var traversal = translator.TraversalClauses.Count == 0 ? "" : " UNION ALL " + compiled.EligibleSource
            + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit
            + " RETURN " + zeros + ",false AS valid," + compiled.Cycle + " AS traversalCycle,"
            + compiled.TraversalLimit + " AS traversalLimit LIMIT 1";
        // Integer division and remainder keep both limbs integral. At the work
        // sentinel each limb sum fits Int64 even when the full total does not.
        // Ownership/traversal errors remain outside pagination and reduction.
        return "CALL { " + compiled.Source + "WHERE NOT valid RETURN " + zeros
            + ",false AS valid,false AS traversalCycle,false AS traversalLimit LIMIT 1" + traversal
            + " UNION ALL " + compiled.EligibleSource + "WHERE (" + compiled.DescendantPredicate
            + ") AND NOT(" + compiled.Cycle + " OR " + compiled.TraversalLimit + ") "
            + "WITH h,p" + keyValues + " ORDER BY " + order + " SKIP $numericOffset LIMIT $numericTake "
            + "WITH " + cell.State + " AS selectedState," + cell.Value + " AS selectedValue "
            + (wide ? "WITH *,CASE WHEN selectedState=2 THEN " + nativeParts + " ELSE " + zeroParts + " END AS numericParts " : "")
            + "WITH count(*) AS inputs,sum(CASE WHEN " + invalid + " THEN 1 ELSE 0 END) AS invalid,"
            + "sum(CASE WHEN selectedState=2 THEN 1 ELSE 0 END) AS present,"
            + reductions + " RETURN " + reduced + ",invalid=0 AS valid,false AS traversalCycle,false AS traversalLimit } "
            + "RETURN " + fields + ",valid,traversalCycle,traversalLimit";
    }
}
