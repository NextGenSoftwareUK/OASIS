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
            var required = column.Selector.ValueType.IsValueType && Nullable.GetUnderlyingType(column.Selector.ValueType) == null;
            if (present < 0 || present > inputs || required && present != inputs)
                throw new InvalidDataException("Native group selected participation is invalid.");
            if (column.Operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average)
            {
                expectedFields += 3;
                if (!document.TryGetValue(name + "high", out var high) || !high.IsDecimal128
                    || !document.TryGetValue(name + "low", out var low) || !low.IsDecimal128)
                    throw new InvalidDataException("Native group numeric limbs must be exact Decimal128 values.");
                var normalized = CosmicNumericScalarResults.FromParts(column.Operation.Value,
                    column.Selector.ValueType, Limb(high), Limb(low), present);
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
                        || !selected.AsBsonDocument.Contains("k") || !selected.AsBsonDocument.TryGetValue("v", out var value))
                        throw new InvalidDataException("Native group extremum packet is invalid.");
                    values[index] = DecodeNativeProjectionCell(value,
                        Nullable.GetUnderlyingType(column.Selector.ValueType) ?? column.Selector.ValueType);
                }
            }
            else throw new InvalidDataException("Native group aggregate is unknown.");
        }
        if (document.ElementCount != expectedFields) throw new InvalidDataException("Native group has unexpected fields.");
        if (!document.TryGetValue("resultMember", out var member) || !member.IsBoolean)
            throw new InvalidDataException("Native group page membership must be Boolean.");
        return new CosmicGroupedRow(inputs, member.AsBoolean, keys, values);
    }
}
