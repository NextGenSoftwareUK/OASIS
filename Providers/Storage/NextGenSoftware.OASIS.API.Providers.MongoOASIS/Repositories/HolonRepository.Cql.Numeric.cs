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
    internal bool? PrefixOverflow { get; }
    internal long[] DecimalParts { get; }
    internal long[] FloatingParts { get; }
    internal MongoCqlNumericValue(long high, long low, long count) : this(high,low,count,null) { }
    internal MongoCqlNumericValue(long high,long low,long count,bool? prefixOverflow)
    { High=high; Low=low; Count=count; PrefixOverflow=prefixOverflow; }
    internal MongoCqlNumericValue(long[] parts, long count, bool floating = false)
    {
        if (floating) FloatingParts = (long[])parts.Clone(); else DecimalParts = (long[])parts.Clone();
        Count = count;
    }
}

public partial class HolonRepository
{
    internal Task<MongoCqlNumericValue> ExecuteNativeCqlNumericAsync(MongoCqlScalarCommand command, Type inputType, CancellationToken cancellationToken)
        => ExecuteNumericCore(command,inputType,false,cancellationToken);

    internal Task<MongoCqlNumericValue> ExecuteNativeCqlCheckedNumericAsync(MongoCqlScalarCommand command,Type inputType,CancellationToken cancellationToken)
        => ExecuteNumericCore(command,inputType,true,cancellationToken);

    private Task<MongoCqlNumericValue> ExecuteNumericCore(MongoCqlScalarCommand command,Type inputType,bool checkedPrefix,CancellationToken cancellationToken)
    {
        var type = Nullable.GetUnderlyingType(inputType) ?? inputType;
        var decimalInput = type == typeof(decimal);
        var floatingWidth = type == typeof(float) ? CosmicFloatingAggregateResults.SingleLimbCount
            : type == typeof(double) ? CosmicFloatingAggregateResults.DoubleLimbCount : 0;
        var empty = floatingWidth != 0 ? new MongoCqlNumericValue(new long[floatingWidth], 0, floating: true)
            : decimalInput ? new MongoCqlNumericValue(new long[CosmicDecimalScalarResults.LimbCount], 0) : new MongoCqlNumericValue(0, 0, 0, checkedPrefix ? false : null);
        return ExecuteNativeCqlScalarAsync(command, doc => checkedPrefix
            ? ReadNativeCqlCheckedNumeric(doc, command.HasTraversalEnvelope, Nullable.GetUnderlyingType(inputType) == null)
            : ReadNativeCqlNumeric(doc, command.HasTraversalEnvelope, decimalInput, Nullable.GetUnderlyingType(inputType) == null, floatingWidth), empty, cancellationToken);
    }

    internal static MongoCqlNumericValue ReadNativeCqlNumeric(BsonDocument document, bool traverses, bool decimalInput = false, bool required = false, int floatingWidth = 0)
        => ReadNumericCore(document,traverses,decimalInput,required,floatingWidth,false);

    internal static MongoCqlNumericValue ReadNativeCqlCheckedNumeric(BsonDocument document, bool traverses, bool required)
        => ReadNumericCore(document, traverses, false, required, 0, true);

    private static MongoCqlNumericValue ReadNumericCore(BsonDocument document,bool traverses,bool decimalInput,bool required,int floatingWidth,bool checkedPrefix)
    {
        if (checkedPrefix && (decimalInput || floatingWidth != 0)) throw new InvalidDataException("A checked Int64 packet is required.");
        if (floatingWidth != 0 && floatingWidth != CosmicFloatingAggregateResults.SingleLimbCount
            && floatingWidth != CosmicFloatingAggregateResults.DoubleLimbCount || floatingWidth != 0 && decimalInput)
            throw new InvalidDataException("A single declared numeric packet domain is required.");
        document = UnwrapNativeSelectedScalar(document, traverses);
        if (document.ElementCount != (decimalInput || floatingWidth != 0 ? 4 : checkedPrefix ? 6 : 5) || !document.TryGetValue("inputs", out var inputs) || !Integer(inputs)
            || !document.TryGetValue("invalid", out var invalid) || !Integer(invalid)
            || !document.TryGetValue("present", out var present) || !Integer(present))
            throw new InvalidDataException("Native numeric reduction requires its exact declared packet shape.");
        if (invalid.ToInt64() != 0 || inputs.ToInt64() < 0 || present.ToInt64() < 0 || present.ToInt64() > inputs.ToInt64() || required && present.ToInt64() != inputs.ToInt64())
            throw new InvalidDataException("Native numeric reduction contains invalid participating values/counts.");
        if (inputs.ToInt64() > CosmicNumericScalarResults.MaximumParticipatingValues) throw new MongoCqlLimitException();
        if (floatingWidth != 0)
        {
            if (!document.TryGetValue("parts", out var words) || !words.IsBsonArray
                || words.AsBsonArray.Count != floatingWidth || words.AsBsonArray.Any(part => !part.IsInt64))
                throw new InvalidDataException("Floating reduction requires its declared exact Int64 limb sums.");
            return new MongoCqlNumericValue(words.AsBsonArray.Select(part => part.AsInt64).ToArray(), present.ToInt64(), floating: true);
        }
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
        bool? overflow = null;
        if (checkedPrefix)
        {
            if (!document.TryGetValue("prefixOverflow",out var flag) || !flag.IsBoolean)
                throw new InvalidDataException("Checked Int64 reduction requires its Boolean prefix acknowledgement.");
            overflow=flag.AsBoolean;
        }
        return new MongoCqlNumericValue(Limb(high), Limb(low), present.ToInt64(),overflow);
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
