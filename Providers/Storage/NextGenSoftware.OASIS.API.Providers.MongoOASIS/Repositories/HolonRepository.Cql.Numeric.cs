using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

internal sealed class MongoCqlNumericValue
{
    internal long High { get; }
    internal long Low { get; }
    internal long Count { get; }
    internal MongoCqlNumericValue(long high, long low, long count) { High = high; Low = low; Count = count; }
}

public partial class HolonRepository
{
    internal Task<MongoCqlNumericValue> ExecuteNativeCqlNumericAsync(MongoCqlScalarCommand command, CancellationToken cancellationToken)
        => ExecuteNativeCqlScalarAsync(command, doc => ReadNativeCqlNumeric(doc, command.HasTraversalEnvelope),
            new MongoCqlNumericValue(0, 0, 0), cancellationToken);

    internal static MongoCqlNumericValue ReadNativeCqlNumeric(BsonDocument document, bool traverses)
    {
        document = UnwrapNativeSelectedScalar(document, traverses);
        if (document.ElementCount != 5 || !document.TryGetValue("inputs", out var inputs) || !Integer(inputs)
            || !document.TryGetValue("invalid", out var invalid) || !Integer(invalid)
            || !document.TryGetValue("present", out var present) || !Integer(present)
            || !document.TryGetValue("high", out var high) || !high.IsDecimal128
            || !document.TryGetValue("low", out var low) || !low.IsDecimal128)
            throw new InvalidDataException("Native numeric reduction requires an exact five-field packet.");
        if (invalid.ToInt64() != 0 || inputs.ToInt64() < 0 || present.ToInt64() < 0 || present.ToInt64() > inputs.ToInt64())
            throw new InvalidDataException("Native numeric reduction contains invalid participating values/counts.");
        if (inputs.ToInt64() > CosmicNumericScalarResults.MaximumParticipatingValues) throw new MongoCqlLimitException();
        return new MongoCqlNumericValue(Limb(high), Limb(low), present.ToInt64());
    }

    private static bool Integer(BsonValue value) => value.IsInt32 || value.IsInt64;
    private static long Limb(BsonValue value)
    {
        try
        {
            var native = value.AsDecimal128;
            if (Decimal128.IsNaN(native) || Decimal128.IsInfinity(native))
                throw new InvalidDataException("Native numeric limb must be finite.");
            var exact = Decimal128.ToDecimal(native);
            if (decimal.Truncate(exact) != exact || exact < long.MinValue || exact > long.MaxValue)
                throw new InvalidDataException("Native numeric limb is not an exact Int64.");
            var integer = (long)exact;
            // Decimal conversion can round a 34-digit fractional value. Compare
            // against the original Decimal128 before accepting the Int64 limb.
            if (Decimal128.Compare(native, new Decimal128(integer)) != 0)
                throw new InvalidDataException("Native numeric limb cannot lose fractional precision.");
            return integer;
        }
        catch (OverflowException cause) { throw new InvalidDataException("Native numeric limb exceeds its exact range.", cause); }
    }
}
