using System.Globalization;
using System.Text.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicMixedNumericAggregateTests
{
    private static readonly Guid Actor = Guid.Parse("f50ca994-7abe-48a8-8468-9b009bb3449c");
    private sealed record Fixture(CosmicProjectionQuery<Holon, CosmicProjectionRecord> Query, Guid Mapping,
        IReadOnlyList<CosmicProjectionRecord> Stored, IReadOnlyList<VirtualHolonProjectionRead> Live);

    private static async Task<Fixture> Create(Type type, bool nullable, object[] stored, object[] native,
        int overlap = 0, int readLimit = 100, bool computed = false, Guid? sourceActor = null)
    {
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, Actor, ProviderType.MongoDBOASIS, "people",
            VirtualHolonMapping.SandboxConnectionName, "native-aggregate://fixture",
            new Dictionary<string, string> { ["tier"] = "nativeTier", ["amount"] = "nativeAmount" });
        var token = computed || type == typeof(string) ? "String" : type == typeof(int) ? "Int32"
            : type == typeof(long) ? "Int64" : "Double";
        var function = computed ? NativeRecordFunction.Utf16Length : NativeRecordFunction.None;
        var selected = new[] { new NativeProjectionColumn("amount", "Value", computed ? typeof(string) : type, nullable, function) };
        var reader = new Mock<INativeDataProjectionQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(value => value.NativeDataProviderType).Returns(ProviderType.MongoDBOASIS);
        reader.Setup(value => value.DescribeVirtualQueryConnection(null)).Returns(mapping.ConnectionIdentity);
        IReadOnlyList<NativeProjectionRow> rows = native.Select((value, index) =>
            new NativeProjectionRow("people", "n" + index, new[] {
                value == null ? new NativeProjectionCell(NativeProjectionValueState.Null)
                    : new NativeProjectionCell(NativeProjectionValueState.Present, value) })).ToArray();
        reader.Setup(value => value.QueryNativeProjectionRecordsAsync("people",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            readLimit, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<NativeProjectionRow>>(rows) { IsLoaded = true });
        var read = await mapping.QueryProjectionAsync(Actor, reader.Object, null,
            new[] { new NativeRecordFilter("tier", CosmicQueryComparisonOperator.Equal, "gold") }, selected, readLimit);
        Assert.False(read.IsError, read.Message);
        var request = new CqlProjectionV1Request
        {
            VirtualMappingId = mapping.Id, OrderBy = "Id", Take = 1,
            Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "tier" }, Scalar = CqlV1Scalar.String,
                Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement("gold") } },
            Columns = new[] { new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                new CqlProjectionV1Column { MetadataPath = new[] { "amount" }, Alias = "Value",
                    ValueType = token, Nullable = nullable, Function = computed ? CqlV1Function.Utf16Length : null } }
        };
        var actor = sourceActor ?? Actor;
        var query = CqlV1Compiler.CompileProjection(request, actor, ProviderType.MongoDBOASIS,
            CosmicQueryReadAccess.OwnerOrPublic(actor).Result, 100);
        var storedRows = stored.Select((value, index) => new CosmicProjectionRow(
            index < overlap ? read.Result[index].Id : Guid.NewGuid(), value)).ToArray();
        var captured = CosmicProjectionResults.Materialize(query, storedRows);
        Assert.False(captured.IsError, captured.Message);
        return new(query, mapping.Id, captured.Result, read.Result);
    }

    public static IEnumerable<object[]> NumericCases()
    {
        foreach (var type in new[] { typeof(int), typeof(long), typeof(double) })
            foreach (var operation in new[] { CosmicScalarOperation.Min, CosmicScalarOperation.Max,
                CosmicScalarOperation.Sum, CosmicScalarOperation.Average })
                yield return new object[] { type, operation };
    }

    [Theory]
    [MemberData(nameof(NumericCases))]
    public async Task ReductionUsesTheLiveWinnerSkipsNullsAndRetainsTheExactOutputDomain(Type type, CosmicScalarOperation operation)
    {
        object Number(int value) => Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        var fixture = await Create(type, true, new[] { Number(100), Number(3), null! },
            new[] { Number(2), Number(4), null! }, overlap: 1);
        var result = CosmicMixedProjectionResults.Aggregate(fixture.Query, Actor, fixture.Mapping,
            fixture.Stored, fixture.Live, operation);
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
        Assert.Equal(3, result.Result.ParticipatingCount);
        var expected = operation switch { CosmicScalarOperation.Min => 2, CosmicScalarOperation.Max => 4,
            CosmicScalarOperation.Sum => 9, _ => 3 };
        var output = operation == CosmicScalarOperation.Average && type != typeof(double) ? typeof(decimal) : type;
        Assert.Equal(output, Nullable.GetUnderlyingType(result.Result.ValueType));
        Assert.Equal(Convert.ChangeType(expected, output, CultureInfo.InvariantCulture), result.Result.Value);
    }

    public static IEnumerable<object[]> EmptyCases()
    {
        foreach (var pair in NumericCases())
            foreach (var nullable in new[] { false, true }) yield return new[] { pair[0], pair[1], (object)nullable };
    }

    [Theory]
    [MemberData(nameof(EmptyCases))]
    public async Task EmptyReductionUsesThePublishedZeroNullOrEmptySequenceContract(Type type, CosmicScalarOperation operation, bool nullable)
    {
        var fixture = await Create(type, nullable, Array.Empty<object>(), Array.Empty<object>());
        var result = CosmicMixedProjectionResults.Aggregate(fixture.Query, Actor, fixture.Mapping,
            fixture.Stored, fixture.Live, operation);
        if (operation != CosmicScalarOperation.Sum && !nullable)
        {
            Assert.True(result.IsError); Assert.Equal("CQL_EMPTY_SEQUENCE", result.ErrorCode); return;
        }
        Assert.False(result.IsError, result.Message);
        Assert.Equal(0, result.Result.ParticipatingCount);
        if (operation == CosmicScalarOperation.Sum)
            Assert.Equal(Convert.ChangeType(0, type, CultureInfo.InvariantCulture), result.Result.Value);
        else Assert.Null(result.Result.Value);
    }

    [Theory]
    [MemberData(nameof(NumericCases))]
    public async Task AllNullInputsDoNotBecomeParticipatingNumericValues(Type type, CosmicScalarOperation operation)
    {
        var fixture = await Create(type, true, new object[] { null! }, new object[] { null! });
        var result = CosmicMixedProjectionResults.Aggregate(fixture.Query, Actor, fixture.Mapping,
            fixture.Stored, fixture.Live, operation);
        Assert.False(result.IsError, result.Message); Assert.Equal(0, result.Result.ParticipatingCount);
        if (operation == CosmicScalarOperation.Sum)
            Assert.Equal(Convert.ChangeType(0, type, CultureInfo.InvariantCulture), result.Result.Value);
        else Assert.Null(result.Result.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IntegerPacketsPreserveWideValuesAndCancellationBeforeFinalOverflowChecks(bool wide)
    {
        var type = wide ? typeof(long) : typeof(int);
        var values = wide ? new object[] { long.MaxValue, 1L, -long.MaxValue }
            : new object[] { int.MaxValue, 1, -int.MaxValue };
        var fixture = await Create(type, false, values, Array.Empty<object>());
        var result = CosmicMixedProjectionResults.Aggregate(fixture.Query, Actor, fixture.Mapping,
            fixture.Stored, fixture.Live, CosmicScalarOperation.Sum);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(wide ? (object)1L : 1, result.Result.Value);
        var overflow = await Create(type, false, wide ? new object[] { long.MaxValue, 1L }
            : new object[] { int.MaxValue, 1 }, Array.Empty<object>());
        Assert.Equal("CQL_SCALAR_OVERFLOW", CosmicMixedProjectionResults.Aggregate(overflow.Query, Actor,
            overflow.Mapping, overflow.Stored, overflow.Live, CosmicScalarOperation.Sum).ErrorCode);
    }

    [Fact]
    public async Task Int64AverageNeverPassesThroughBinary64()
    {
        var fixture = await Create(typeof(long), false, new object[] { 9007199254740993L, 9007199254740995L }, Array.Empty<object>());
        var result = CosmicMixedProjectionResults.Aggregate(fixture.Query, Actor, fixture.Mapping,
            fixture.Stored, fixture.Live, CosmicScalarOperation.Average);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(typeof(decimal), result.Result.ValueType);
        Assert.Equal(9007199254740994m, result.Result.Value);
    }

    [Fact]
    public async Task FloatingPacketsRoundOnceAcrossCancellationOverflowAndSubnormalMeans()
    {
        foreach (var entry in new[] {
            (Values:new[] { 1e16, 1d, -1e16 }, Op:CosmicScalarOperation.Sum, Expected:1d),
            (Values:new[] { double.MaxValue, double.MaxValue, -double.MaxValue }, Op:CosmicScalarOperation.Sum, Expected:double.MaxValue),
            (Values:new[] { double.MaxValue, double.MaxValue }, Op:CosmicScalarOperation.Average, Expected:double.MaxValue),
            (Values:new[] { double.Epsilon, 0d }, Op:CosmicScalarOperation.Average, Expected:0d)
        })
        {
            var fixture = await Create(typeof(double), false, entry.Values.Cast<object>().ToArray(), Array.Empty<object>());
            var result = CosmicMixedProjectionResults.Aggregate(fixture.Query, Actor, fixture.Mapping,
                fixture.Stored, fixture.Live, entry.Op);
            Assert.False(result.IsError, result.Message);
            Assert.Equal(entry.Expected, Assert.IsType<double>(result.Result.Value));
        }
        var overflow = await Create(typeof(double), false, new object[] { double.MaxValue, double.MaxValue }, Array.Empty<object>());
        Assert.Equal("CQL_SCALAR_OVERFLOW", CosmicMixedProjectionResults.Aggregate(overflow.Query, Actor, overflow.Mapping,
            overflow.Stored, overflow.Live, CosmicScalarOperation.Sum).ErrorCode);
    }

    [Theory]
    [InlineData(CosmicScalarOperation.Min)]
    [InlineData(CosmicScalarOperation.Max)]
    [InlineData(CosmicScalarOperation.Sum)]
    [InlineData(CosmicScalarOperation.Average)]
    public async Task NativeComputedLengthIsReducedAsItsIntegerOutput(CosmicScalarOperation operation)
    {
        var fixture = await Create(typeof(int), true, new object[] { 100, 3 }, new object[] { 2, 4 }, 1, computed: true);
        var result = CosmicMixedProjectionResults.Aggregate(fixture.Query, Actor, fixture.Mapping,
            fixture.Stored, fixture.Live, operation);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(operation == CosmicScalarOperation.Average ? (object)3m
            : operation == CosmicScalarOperation.Min ? 2 : operation == CosmicScalarOperation.Max ? 4 : 9, result.Result.Value);
    }

    [Theory]
    [InlineData("actor")]
    [InlineData("mapping")]
    [InlineData("duplicate")]
    [InlineData("full")]
    [InlineData("shortened")]
    [InlineData("payload")]
    [InlineData("cancelled")]
    [InlineData("domain")]
    public async Task NumericReductionStillRequiresTheWholeAdmittedInput(string invalid)
    {
        var type = invalid == "domain" ? typeof(string) : typeof(int);
        var stored = invalid == "domain" ? new object[] { "x" } : new object[] { 3 };
        var native = invalid == "full" ? Enumerable.Repeat((object)2, 100).ToArray()
            : invalid == "domain" ? new object[] { "y" } : new object[] { 2 };
        var fixture = await Create(type, true, stored, native, readLimit: invalid == "shortened" ? 1 : 100,
            sourceActor: invalid == "actor" ? Guid.NewGuid() : null);
        var live = invalid == "duplicate" ? new[] { fixture.Live[0], fixture.Live[0] } : fixture.Live;
        using var cancellation = new CancellationTokenSource();
        if (invalid == "cancelled") cancellation.Cancel();
        var result = CosmicMixedProjectionResults.Aggregate(fixture.Query, Actor,
            invalid == "mapping" ? Guid.NewGuid() : fixture.Mapping, fixture.Stored, live,
            CosmicScalarOperation.Min, invalid == "payload" ? 1 : CosmicMixedProjectionResults.MaximumInputBytes, cancellation.Token);
        Assert.True(result.IsError);
        Assert.Equal(invalid switch { "actor" => "CQL_ACCESS_INVALID", "full" or "shortened" => "CQL_VIRTUAL_SOURCE_LIMIT",
            "domain" => "CQL_CAPABILITY_UNSUPPORTED", "payload" => "CQL_RESULT_LIMIT",
            "cancelled" => "CQL_QUERY_CANCELLED", _ => "CQL_PROVIDER_RESULT_INVALID" }, result.ErrorCode);
    }
}
