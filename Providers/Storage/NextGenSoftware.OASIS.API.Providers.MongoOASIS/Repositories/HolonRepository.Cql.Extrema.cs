using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

internal sealed class MongoCqlExtremumValue
{
    internal bool HasValue { get; }
    internal object Value { get; }
    internal MongoCqlExtremumValue(bool hasValue, object value) { HasValue = hasValue; Value = value; }
}

public partial class HolonRepository
{
    internal Task<MongoCqlExtremumValue> ExecuteNativeCqlExtremumAsync(MongoCqlScalarCommand command,
        Type selectedType, CancellationToken cancellationToken)
        => ExecuteNativeCqlScalarAsync(command, document => ReadNativeCqlExtremum(document, selectedType, command.HasTraversalEnvelope),
            new MongoCqlExtremumValue(false, null), cancellationToken);

    private static MongoCqlExtremumValue ReadNativeCqlExtremum(BsonDocument document, Type selectedType, bool traverses)
    {
        document = UnwrapNativeSelectedScalar(document, traverses);
        if (document.ElementCount != 3 || !document.TryGetValue("invalid", out var invalid) || !(invalid.IsInt32 || invalid.IsInt64)
            || !document.TryGetValue("present", out var present) || !(present.IsInt32 || present.IsInt64)
            || !document.TryGetValue("cell", out var cell) || !cell.IsBsonDocument)
            throw new InvalidDataException("Native extremum requires a validated selected-value envelope.");
        if (invalid.ToInt64() != 0 || present.ToInt64() < 0)
            throw new InvalidDataException("Native extremum contains an invalid participating value.");
        var selected = cell.AsBsonDocument;
        if (selected.ElementCount != 2 || !selected.TryGetValue("s", out var state) || !state.IsInt32
            || !selected.TryGetValue("v", out var value))
            throw new InvalidDataException("Native extremum requires one exact typed cell.");
        if (present.ToInt64() == 0)
        {
            if ((state.AsInt32 != 0 && state.AsInt32 != 1) || !value.IsBsonNull)
                throw new InvalidDataException("Native empty extremum contains a selected value.");
            return new MongoCqlExtremumValue(false, null);
        }
        if (state.AsInt32 != 2 || value.IsBsonNull) throw new InvalidDataException("Native extremum did not select a present value.");
        return new MongoCqlExtremumValue(true, DecodeNativeProjectionCell(value, Nullable.GetUnderlyingType(selectedType) ?? selectedType));
    }
    private static BsonDocument UnwrapNativeSelectedScalar(BsonDocument document, bool traverses)
    {
        if (traverses)
        {
            if (document.ElementCount != 2 || !document.TryGetValue("error", out var error) || !error.IsInt32
                || !document.TryGetValue("row", out var row))
                throw new InvalidDataException("Native extremum requires its exact traversal envelope.");
            if (error.AsInt32 == 1) throw new InvalidDataException("MongoDB scalar traversal encountered a cycle or conflicting identity.");
            if (error.AsInt32 == 2) throw new MongoCqlLimitException();
            if (error.AsInt32 != 0 || !row.IsBsonDocument) throw new InvalidDataException("Native extremum traversal outcome is invalid.");
            document = row.AsBsonDocument;
        }
        return document;
    }

}
