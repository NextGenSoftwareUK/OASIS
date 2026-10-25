using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicUnsignedNumericAggregateTests
{
    [Fact]
    public void UnsignedSumCapturePreservesSourceAndNullableDomain()
    {
        var actor = Guid.NewGuid();
        var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .OrderBy(h => h.Name).Skip(2).Take(4).Result;
        var sum = source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "counter"));
        var nullable = source.Sum(h => CosmicQueryMetadata.Value<ulong?>(h, "counter"));

        Assert.False(sum.IsError, sum.Message);
        Assert.False(nullable.IsError, nullable.Message);
        Assert.Same(source, sum.Result.Source);
        Assert.Same(source, nullable.Result.Source);
        Assert.Equal(typeof(ulong?), nullable.Result.Selector.ValueType);
        Assert.Equal(actor, sum.Result.Source.ReadAccess.ActorId);
    }

    [Theory]
    [InlineData(0L, 0L, 0L, 0UL)]
    [InlineData(1L, 0L, 1L, 4294967296UL)]
    [InlineData(2097152L, 1L, 1L, 9007199254740993UL)]
    [InlineData(2147483648L, 0L, 1L, 9223372036854775808UL)]
    [InlineData(4294967295L, 4294967295L, 1L, ulong.MaxValue)]
    [InlineData(0L, 8589934590L, 2L, 8589934590UL)]
    public void SumRetainsTheFullUnsignedDomain(long high, long low, long count, ulong expected)
    {
        var result = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(ulong), high, low, count);
        Assert.False(result.IsError, result.Message);
        Assert.True(result.IsLoaded);
        Assert.Equal(expected, Assert.IsType<ulong>(result.Result));
    }

    [Theory]
    [InlineData(4294967295L, 4294967295L, 2L, "9223372036854775807.5")]
    [InlineData(8589934590L, 8589934590L, 2L, "18446744073709551615")]
    [InlineData(0L, 1L, 3L, "0.3333333333333333333333333333")]
    public void AverageUsesAnExactNumeratorWithoutSignedOrBinary64Narrowing(long high, long low, long count, string expected)
    {
        var result = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Average, typeof(ulong), high, low, count);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), Assert.IsType<decimal>(result.Result));
    }

    [Fact]
    public void MaximumWorkNumeratorFitsDecimalWhileFinalUnsignedSumStillChecksOverflow()
    {
        const long count = int.MaxValue;
        var limb = checked(4294967295L * count);
        var average = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Average, typeof(ulong), limb, limb, count);
        Assert.False(average.IsError, average.Message);
        Assert.Equal((decimal)ulong.MaxValue, Assert.IsType<decimal>(average.Result));
        var sum = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(ulong), limb, limb, count);
        Assert.Equal("CQL_SCALAR_OVERFLOW", sum.ErrorCode);
        Assert.False(sum.IsLoaded);
    }

    [Theory]
    [InlineData(CosmicScalarOperation.Sum, -1L, 0L, 1L)]
    [InlineData(CosmicScalarOperation.Average, 0L, -1L, 1L)]
    [InlineData(CosmicScalarOperation.Sum, 4294967296L, 0L, 1L)]
    [InlineData(CosmicScalarOperation.Average, 0L, 4294967296L, 1L)]
    [InlineData(CosmicScalarOperation.Sum, 1L, 0L, 0L)]
    [InlineData(CosmicScalarOperation.Average, 0L, 1L, 0L)]
    [InlineData(CosmicScalarOperation.Sum, 0L, 0L, -1L)]
    public void ImpossibleUnsignedPacketsReturnNoLoadedValue(CosmicScalarOperation operation, long high, long low, long count)
    {
        var result = CosmicNumericScalarResults.FromParts(operation, typeof(ulong), high, low, count);
        Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
        Assert.False(result.IsLoaded);
    }

    [Fact]
    public void EmptyNullableSumAndAverageKeepTheirDistinctContracts()
    {
        var sum = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Sum, typeof(ulong?), 0, 0, 0);
        Assert.False(sum.IsError, sum.Message);
        Assert.Equal(0UL, Assert.IsType<ulong>(sum.Result));
        var optional = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Average, typeof(ulong?), 0, 0, 0);
        Assert.False(optional.IsError, optional.Message);
        Assert.True(optional.IsLoaded);
        Assert.Null(optional.Result);
        Assert.Equal("CQL_EMPTY_SEQUENCE", CosmicNumericScalarResults.FromParts(
            CosmicScalarOperation.Average, typeof(ulong), 0, 0, 0).ErrorCode);
    }

    [Fact]
    public void UnsignedWorkExcessIsNotTruncated()
    {
        var result = CosmicNumericScalarResults.FromParts(CosmicScalarOperation.Average, typeof(ulong), 0, 0, (long)int.MaxValue + 1);
        Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode);
        Assert.False(result.IsLoaded);
    }

    [Fact]
    public void ExistingNumericProfilesDoNotGainUnsignedArithmeticFromReadPermissions()
    {
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),
            new[] { typeof(long), typeof(long?), typeof(ulong), typeof(ulong?) });
        var capabilities = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, reads) });
        var query = CosmicQuery<Holon>.Create().Sum(h => CosmicQueryMetadata.Value<ulong>(h, "counter"));
        Assert.False(query.IsError, query.Message);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", capabilities.Admit(query.Result).ErrorCode);
    }

    private static CosmicProjectionCapabilities AllReads => new(Array.Empty<CosmicProjectionFieldCapability>(),
        new[] { typeof(long), typeof(long?), typeof(ulong), typeof(ulong?) });

    [Fact]
    public void ExplicitInputPermissionsAreImmutableAndCannotGrantUnreadablePaths()
    {
        var inputs = new[] { typeof(ulong), typeof(ulong?) };
        var permission = new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, AllReads, inputs);
        inputs[0] = typeof(long);
        Assert.Equal(typeof(ulong), permission.InputTypes[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<Type>)permission.InputTypes)[0] = typeof(long));

        var profile = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[] { permission });
        var unsigned = CosmicQuery<Holon>.Create().Sum(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result;
        var signed = CosmicQuery<Holon>.Create().Sum(h => CosmicQueryMetadata.Value<long>(h, "counter")).Result;
        Assert.False(profile.Admit(unsigned).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", profile.Admit(signed).ErrorCode);

        var noUnsignedReads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(long) });
        var missingRead = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
            { new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, noUnsignedReads, new[] { typeof(ulong) }) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", missingRead.Admit(unsigned).ErrorCode);
    }

    [Fact]
    public void TypedAverageOverloadsRetainUnsignedInputsAndDecimalOutputs()
    {
        var source = CosmicQuery<Holon>.Create();
        var average = source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "counter"));
        var nullable = source.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "counter"));
        var wrapped = source.Where(h => h.IsActive).Average(h => CosmicQueryMetadata.Value<ulong>(h, "counter"));
        var wrappedNullable = source.Where(h => h.IsActive).Average(h => CosmicQueryMetadata.Value<ulong?>(h, "counter"));
        Assert.False(average.IsError, average.Message);
        Assert.False(nullable.IsError, nullable.Message);
        Assert.False(wrapped.IsError, wrapped.Message);
        Assert.False(wrappedNullable.IsError, wrappedNullable.Message);
        Assert.Equal(typeof(ulong), average.Result.Selector.ValueType);
        Assert.Equal(typeof(ulong?), nullable.Result.Selector.ValueType);
        Assert.Equal(4294967296m, CosmicNumericScalarResults.FromParts(average.Result, 1, 0, 1).Result);
        var empty = CosmicNumericScalarResults.FromParts(nullable.Result, 0, 0, 0);
        Assert.False(empty.IsError, empty.Message);
        Assert.Null(empty.Result);
    }

    [Fact]
    public void GroupedCaptureAndAdmissionShareTheUnsignedOperationPermission()
    {
        var grouping = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Result;
        var selected = grouping.Select(g => new
        {
            Sum = g.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "counter")),
            Mean = g.Average(h => CosmicQueryMetadata.Value<ulong>(h, "counter")),
            OptionalMean = g.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "counter"))
        });
        Assert.False(selected.IsError, selected.Message);
        Assert.Equal(new[] { typeof(ulong), typeof(decimal), typeof(decimal?) }, selected.Result.Columns.Select(c => c.ValueType));

        var keys = new CosmicProjectionCapabilities(new[]
            { new CosmicProjectionFieldCapability(nameof(Holon.Name), typeof(string)) }, Array.Empty<Type>());
        var legacy = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, AllReads),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, AllReads)
        });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", new CosmicGroupedCapabilities(keys, legacy).Admit(selected.Result).ErrorCode);
        var enrolled = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, AllReads, new[] { typeof(ulong) }),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, AllReads, new[] { typeof(ulong), typeof(ulong?) })
        });
        Assert.False(new CosmicGroupedCapabilities(keys, enrolled).Admit(selected.Result).IsError);
    }

    [Theory]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(double))]
    [InlineData(typeof(float))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(string))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(object))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(long[]))]
    public void NumericInputDeclarationsRejectUnsupportedDomains(Type type)
        => Assert.Throws<ArgumentException>(() => new CosmicScalarSelectionCapability(
            CosmicScalarOperation.Sum, AllReads, new[] { type }));

    [Fact]
    public void NumericInputDeclarationsRejectMissingDuplicateAndUnboundedSnapshots()
    {
        Assert.Throws<ArgumentException>(() => new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, AllReads, null!));
        Assert.Throws<ArgumentException>(() => new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, AllReads, Array.Empty<Type>()));
        Assert.Throws<ArgumentException>(() => new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, AllReads,
            new[] { typeof(ulong), typeof(ulong) }));
        Assert.Throws<ArgumentException>(() => new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, AllReads,
            Enumerable.Repeat(typeof(ulong), CosmicProjectionCapabilities.MaximumMetadataDomains + 1).ToArray()));
    }

    [Fact]
    public void MinMaxKeepExistingReadPermissionUnlessAnExplicitInputRestrictionIsRequested()
    {
        var query = CosmicQuery<Holon>.Create().Min(h => CosmicQueryMetadata.Value<long>(h, "counter")).Result;
        var original = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
            { new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, AllReads) });
        Assert.False(original.Admit(query).IsError);
        var restricted = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(), new[]
            { new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, AllReads, new[] { typeof(ulong) }) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", restricted.Admit(query).ErrorCode);
    }
}
