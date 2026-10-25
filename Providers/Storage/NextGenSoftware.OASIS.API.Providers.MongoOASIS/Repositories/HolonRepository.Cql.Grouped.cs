using NextGenSoftware.OASIS.Common;
using System;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

internal sealed class MongoCqlGroupedException : Exception
{
    internal string Code { get; }
    internal MongoCqlGroupedException(string code, string message) : base(message) { Code = code; }
}

public partial class HolonRepository
{
    internal static CosmicGroupedRow ReadNativeCqlGrouped<T, TKey, TResult>(BsonDocument document,
        CosmicGroupedSelection<T, TKey, TResult> query, bool traverses = false) where T : IHolon
    {
        document = UnwrapNativeSelectedScalar(document, traverses);
        long IntegerField(string name)
        {
            if (!document.TryGetValue(name, out var value) || !Integer(value))
                throw new InvalidDataException("Native group requires an exact integer " + name + ".");
            return value.ToInt64();
        }

        var inputs = IntegerField("inputs");
        var work = IntegerField("work");
        if (inputs < 1 || work < inputs || IntegerField("invalid") != 0)
            throw new InvalidDataException("Native group participation/domain proof is invalid.");
        if (work > CosmicNumericScalarResults.MaximumParticipatingValues) throw new MongoCqlLimitException();

        var count = query.Grouping.Key.Columns.Count;
        if (!document.TryGetValue("_id", out var identity) || !identity.IsBsonDocument
            || identity.AsBsonDocument.ElementCount != count)
            throw new InvalidDataException("Native group identity does not match every admitted key.");
        var keyCells = new BsonArray();
        for (var index = 0; index < count; index++)
        {
            var name = "k" + index;
            if (!identity.AsBsonDocument.Contains(name) || !document.TryGetValue(name, out var key))
                throw new InvalidDataException("Native group key is missing.");
            keyCells.Add(key);
        }
        // Reuse the projection owner's exact domain and null-state decoder.
        var keys = ReadNativeCqlProjectionRow(new BsonDocument("cells", keyCells), query.Grouping.Key.Columns).Values.ToArray();
        for (var index = 0; index < keys.Length; index++)
            ValidateNativeComparable(identity.AsBsonDocument["k" + index], keys[index], query.Grouping.Key.Columns[index].Path.ValueType);
        var values = new object[query.Columns.Count];
        var expectedFields = 5 + count;
        for (var index = 0; index < values.Length; index++)
        {
            var column = query.Columns[index];
            if (column.KeyIndex >= 0) { values[index] = keys[column.KeyIndex]; continue; }
            if (column.Selector == null)
            {
                values[index] = column.Operation == CosmicScalarOperation.Count ? (object)checked((int)inputs) : inputs;
                continue;
            }

            var name = "a" + index;
            var present = IntegerField(name + "present");
            var coverage = CosmicGroupedResults.ValidateParticipation(column.Selector.ValueType, inputs, present);
            if (coverage.IsError) throw new InvalidDataException(coverage.Message);
            if (column.Operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average)
            {
                OASISResult<object> normalized;
                if ((Nullable.GetUnderlyingType(column.Selector.ValueType) ?? column.Selector.ValueType) == typeof(decimal))
                {
                    expectedFields += CosmicDecimalScalarResults.LimbCount + 1;
                    var parts = new long[CosmicDecimalScalarResults.LimbCount];
                    for (var part = 0; part < parts.Length; part++)
                    {
                        if (!document.TryGetValue(name + "d" + part, out var word) || !word.IsDecimal128)
                            throw new InvalidDataException("Native Decimal group requires seven exact Decimal128 limb sums.");
                        parts[part] = Limb(word);
                    }
                    normalized = CosmicDecimalScalarResults.FromParts(column.Operation.Value, column.Selector.ValueType, parts, present);
                }
                else
                {
                    expectedFields += 3;
                    if (!document.TryGetValue(name + "high", out var high) || !high.IsDecimal128
                        || !document.TryGetValue(name + "low", out var low) || !low.IsDecimal128)
                        throw new InvalidDataException("Native group numeric limbs must be exact Decimal128 values.");
                    normalized = CosmicNumericScalarResults.FromParts(column.Operation.Value,
                        column.Selector.ValueType, Limb(high), Limb(low), present);
                }
                if (normalized.IsError) throw new MongoCqlGroupedException(normalized.ErrorCode, normalized.Message);
                values[index] = normalized.Result;
            }
            else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max)
            {
                expectedFields += 2;
                if (!document.TryGetValue(name, out var selected)) throw new InvalidDataException("Native group extremum is missing.");
                if (present == 0)
                {
                    if (!selected.IsBsonNull) throw new InvalidDataException("Empty grouped extremum contains a value.");
                    values[index] = null;
                }
                else
                {
                    if (!selected.IsBsonDocument || selected.AsBsonDocument.ElementCount != 2
                        || !selected.AsBsonDocument.TryGetValue("k", out var comparable) || !selected.AsBsonDocument.TryGetValue("v", out var value))
                        throw new InvalidDataException("Native group extremum packet is invalid.");
                    values[index] = DecodeNativeProjectionCell(value,
                        Nullable.GetUnderlyingType(column.Selector.ValueType) ?? column.Selector.ValueType);
                    ValidateNativeComparable(comparable, values[index], column.Selector.ValueType);
                }
            }
            else throw new InvalidDataException("Native group aggregate is unknown.");
        }
        if (document.ElementCount != expectedFields) throw new InvalidDataException("Native group has unexpected fields.");
        if (!document.TryGetValue("resultMember", out var member) || !member.IsBoolean)
            throw new InvalidDataException("Native group page membership must be Boolean.");
        return new CosmicGroupedRow(inputs, member.AsBoolean, keys, values);
    }

    // Comparable keys drive native grouping, filters and paging; typed cells
    // drive publication. They must describe the same logical scalar, including
    // on rows excluded by result filtering/paging. Payload bit identity is a
    // separate contract: equal Decimal scales and signed zeros share a key.
    private static void ValidateNativeComparable(BsonValue comparable, object value, Type declared)
    {
        if (value == null)
        {
            if (!comparable.IsBsonNull) throw new InvalidDataException("Native comparable identity contradicts a null typed value.");
            return;
        }
        var type = Nullable.GetUnderlyingType(declared) ?? declared;
        if (type == typeof(Guid))
        {
            // ComparableGuid emits exactly lowercase N text across admitted
            // stored UUID/text representations. Do not guess alternate formats.
            if (!comparable.IsString || comparable.AsString != ((Guid)value).ToString("N"))
                throw new InvalidDataException("Native comparable GUID contradicts its normalized typed identity.");
            return;
        }
        if (comparable.IsBsonNull || !Equals(DecodeNativeProjectionCell(comparable, type), value))
            throw new InvalidDataException("Native comparable identity contradicts its typed scalar value.");
    }
}
