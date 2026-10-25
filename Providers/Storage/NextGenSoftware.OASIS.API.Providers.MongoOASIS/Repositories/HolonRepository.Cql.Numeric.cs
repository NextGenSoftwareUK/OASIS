using System;
using System.IO;
using System.Linq;
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
    internal long[] DecimalParts { get; }
    internal MongoCqlNumericValue(long high, long low, long count) { High = high; Low = low; Count = count; }
    internal MongoCqlNumericValue(long[] parts, long count) { DecimalParts = (long[])parts.Clone(); Count = count; }
}

public partial class HolonRepository
{
    internal Task<MongoCqlNumericValue> ExecuteNativeCqlNumericAsync(MongoCqlScalarCommand command, Type inputType, CancellationToken cancellationToken)
    {
        var decimalInput = (Nullable.GetUnderlyingType(inputType) ?? inputType) == typeof(decimal);
        return ExecuteNativeCqlScalarAsync(command, doc => ReadNativeCqlNumeric(doc, command.HasTraversalEnvelope, decimalInput, Nullable.GetUnderlyingType(inputType) == null),
            decimalInput ? new MongoCqlNumericValue(new long[CosmicDecimalScalarResults.LimbCount], 0) : new MongoCqlNumericValue(0, 0, 0), cancellationToken);
    }

    internal static MongoCqlNumericValue ReadNativeCqlNumeric(BsonDocument document, bool traverses, bool decimalInput = false, bool required = false)
    {
        document = UnwrapNativeSelectedScalar(document, traverses);
        if (document.ElementCount != (decimalInput ? 4 : 5) || !document.TryGetValue("inputs", out var inputs) || !Integer(inputs)
            || !document.TryGetValue("invalid", out var invalid) || !Integer(invalid)
            || !document.TryGetValue("present", out var present) || !Integer(present))
            throw new InvalidDataException("Native numeric reduction requires its exact declared packet shape.");
        if (invalid.ToInt64() != 0 || inputs.ToInt64() < 0 || present.ToInt64() < 0 || present.ToInt64() > inputs.ToInt64() || required && present.ToInt64() != inputs.ToInt64())
            throw new InvalidDataException("Native numeric reduction contains invalid participating values/counts.");
        if (inputs.ToInt64() > CosmicNumericScalarResults.MaximumParticipatingValues) throw new MongoCqlLimitException();
        if (decimalInput)
        {
            if (!document.TryGetValue("parts", out var parts) || !parts.IsBsonArray
                || parts.AsBsonArray.Count != CosmicDecimalScalarResults.LimbCount || parts.AsBsonArray.Any(part => !part.IsDecimal128))
                throw new InvalidDataException("Decimal reduction requires seven exact Decimal128 limb sums.");
            return new MongoCqlNumericValue(parts.AsBsonArray.Select(Limb).ToArray(), present.ToInt64());
        }
        if (!document.TryGetValue("high", out var high) || !high.IsDecimal128
            || !document.TryGetValue("low", out var low) || !low.IsDecimal128)
            throw new InvalidDataException("Integer reduction requires two exact Decimal128 limbs.");
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
