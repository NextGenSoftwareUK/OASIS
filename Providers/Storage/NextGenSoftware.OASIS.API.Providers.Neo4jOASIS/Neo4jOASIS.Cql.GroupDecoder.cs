using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed class NeoCqlGroupedException : Exception
{
    internal string Code { get; }
    internal NeoCqlGroupedException(string code, string message) : base(message) { Code = code; }
}

public partial class Neo4jOASIS
{
    internal static CosmicGroupedRow DecodeCqlGrouped<T, TKey, TResult>(IReadOnlyDictionary<string, object> packet,
        CosmicGroupedSelection<T, TKey, TResult> query) where T : IHolon
    {
        if (packet.Count != 7 || !packet.TryGetValue("inputs", out var rawInputs) || rawInputs is not long inputs
            || !packet.TryGetValue("valid", out var valid) || valid is not bool ok
            || !packet.TryGetValue("traversalCycle", out var cycle) || cycle is not bool hasCycle
            || !packet.TryGetValue("traversalLimit", out var limit) || limit is not bool hasLimit
            || !packet.TryGetValue("payloadLimit", out var payload) || payload is not bool payloadLimited
            || !packet.TryGetValue("keys", out var rawKeys) || rawKeys is not IReadOnlyList<object> keys
            || !packet.TryGetValue("aggregates", out var rawAggregates) || rawAggregates is not IReadOnlyList<object> aggregates)
            throw new InvalidDataException("Native grouped packet has an invalid envelope.");
        if (hasCycle) throw new InvalidDataException("Native grouped traversal encountered a cycle.");
        if (hasLimit) throw new NeoCqlLimitException("Native grouped traversal exceeds its visited-node budget.");
        if (!ok || inputs < 1) throw new InvalidDataException("Native grouped ownership or selected domain is invalid.");
        if (payloadLimited) throw new NeoCqlLimitException("Native grouped row exceeds its payload budget.");
        if (inputs > CosmicNumericScalarResults.MaximumParticipatingValues)
            throw new NeoCqlLimitException("Native grouped input exceeds its work bound.");

        var identity = ReadNativeProjectionRow(keys, query.Grouping.Key.Columns).Values.ToArray();
        var values = new object[query.Columns.Count];
        var position = 0;
        for (var index = 0; index < values.Length; index++)
        {
            var column = query.Columns[index];
            if (column.KeyIndex >= 0) { values[index] = identity[column.KeyIndex]; continue; }
            if (column.Selector == null)
            {
                values[index] = column.Operation == CosmicScalarOperation.Count ? (object)checked((int)inputs) : inputs;
                continue;
            }
            if (position >= aggregates.Count || aggregates[position++] is not IDictionary<string, object> aggregate
                || !aggregate.TryGetValue("present", out var rawPresent) || rawPresent is not long present
                || present < 0 || present > inputs)
                throw new InvalidDataException("Native grouped aggregate participation is invalid.");
            var required = column.Selector.ValueType.IsValueType && Nullable.GetUnderlyingType(column.Selector.ValueType) == null;
            if (required && present != inputs) throw new InvalidDataException("Native required aggregate has missing values.");
            if (column.Operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average)
            {
                if (aggregate.Count != 3 || !aggregate.TryGetValue("high", out var rawHigh) || rawHigh is not long high
                    || !aggregate.TryGetValue("low", out var rawLow) || rawLow is not long low)
                    throw new InvalidDataException("Native grouped numeric limbs must be exact Int64 values.");
                var normalized = CosmicNumericScalarResults.FromParts(column.Operation.Value, column.Selector.ValueType, high, low, present);
                if (normalized.IsError) throw new NeoCqlGroupedException(normalized.ErrorCode, normalized.Message);
                values[index] = normalized.Result;
            }
            else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max)
            {
                if (aggregate.Count != 2 || !aggregate.TryGetValue("value", out var value))
                    throw new InvalidDataException("Native grouped extremum shape is invalid.");
                if (present == 0)
                {
                    if (value != null) throw new InvalidDataException("An empty native extremum contains a value.");
                    values[index] = null;
                }
                else values[index] = DecodeNativeProjectionValue(value,
                    Nullable.GetUnderlyingType(column.Selector.ValueType) ?? column.Selector.ValueType);
            }
            else throw new InvalidDataException("Native grouped aggregate is unknown.");
        }
        if (position != aggregates.Count) throw new InvalidDataException("Native grouped packet has unexpected aggregate slots.");
        return new CosmicGroupedRow(inputs, identity, values);
    }
}
