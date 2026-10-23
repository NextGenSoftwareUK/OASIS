using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicMinMaxTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicProjectionCapabilities ReadLong => new(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(long), typeof(long?) });
    private static CosmicScalarCapabilities Caps(CosmicProjectionCapabilities? reads = null) => new(
        new[] { CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any },
        new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, reads ?? ReadLong),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, reads ?? ReadLong) });
    private static int Calls;
    private static string Unsafe(string value) { Calls++; return value; }

    [Fact]
    public void ExtremaRetainTheExactSourceAccessGraphWindowAndTypedPath()
    {
        var actor = Guid.NewGuid();
        var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 2).Result).Skip(2).Take(3).Result;
        var min = source.Min(h => CosmicQueryMetadata.Value<long?>(h, "nested", "stock"));
        var max = source.Max(h => h.Name);
        Assert.False(min.IsError, min.Message); Assert.False(max.IsError, max.Message);
        Assert.Same(source, min.Result.Source); Assert.Same(source, max.Result.Source);
        Assert.Equal(CosmicScalarOperation.Min, min.Result.Operation);
        Assert.Equal(typeof(long?), min.Result.Selector.ValueType);
        Assert.Equal(new[] { "nested", "stock" }, min.Result.Selector.Segments);
        Assert.Equal(typeof(string), max.Result.Selector.ValueType);
    }

    [Fact]
    public void CountPermissionsAndBorrowedReadDomainsCannotGrantExtrema()
    {
        var query = CosmicQuery<Holon>.Create().Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var countOnly = new CosmicScalarCapabilities(CosmicScalarOperation.Count);
        var denied = CosmicScalarPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicScalarTarget(ProviderType.MongoDBOASIS, Rows, countOnly) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", denied.ErrorCode);
        var plan = CosmicScalarPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicScalarTarget(ProviderType.MongoDBOASIS, Rows, Caps()) }).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicScalarPlanner.Admit(plan, ProviderType.MongoDBOASIS, Rows, countOnly).ErrorCode);
        var wrong = Caps(new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(int) }));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicScalarPlanner.Admit(plan, ProviderType.MongoDBOASIS, Rows, wrong).ErrorCode);
        Assert.Throws<ArgumentException>(() => new CosmicScalarCapabilities(CosmicScalarOperation.Min));
    }

    [Fact]
    public void ExpressionsCannotInvokeCodeOrDiscardAConversion()
    {
        Calls = 0;
        Assert.True(CosmicQuery<Holon>.Create().Min(h => Unsafe(h.Name)).IsError);
        Assert.True(CosmicQuery<Holon>.Create().Max(h => (long)h.Version).IsError);
        Assert.True(CosmicQuery<Holon>.Create().Min(h => 3L).IsError);
        Assert.Equal(0, Calls);
        var failed = CosmicQuery<Holon>.Create().Where(h => Unsafe(h.Name) == "x");
        Assert.Equal(failed.ErrorCode, failed.Min(h => h.Name).ErrorCode);
    }

    [Theory]
    [InlineData(long.MinValue)] [InlineData(-9007199254740993L)] [InlineData(9007199254740993L)] [InlineData(long.MaxValue)]
    public void ExactSelectedInt64RetainsNegativeValuesAndAllBits(long value)
    {
        var query = CosmicQuery<Holon>.Create().Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var result = CosmicSelectedScalarResults.FromValue(query, value, true);
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded); Assert.Equal(value, result.Result);
        Assert.Equal("CQL_PLAN_INVALID", CosmicScalarResults.FromCount(query, 1).ErrorCode);
    }

    [Fact]
    public void EmptyRequiredNullableAndStringDomainsRemainDistinct()
    {
        var source = CosmicQuery<Holon>.Create();
        var required = source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var optional = source.Max(h => CosmicQueryMetadata.Value<long?>(h, "stock")).Result;
        var text = source.Min(h => h.Name).Result;
        Assert.Equal("CQL_EMPTY_SEQUENCE", CosmicSelectedScalarResults.FromValue(required, null, false).ErrorCode);
        var nullable = CosmicSelectedScalarResults.FromValue(optional, null, false);
        Assert.False(nullable.IsError); Assert.True(nullable.IsLoaded); Assert.Null(nullable.Result);
        Assert.Null(CosmicSelectedScalarResults.FromValue(text, null, false).Result);
        Assert.Equal("CQL_DATA_INVALID", CosmicSelectedScalarResults.FromValue(optional, null, true).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicSelectedScalarResults.FromValue(optional, 1L, false).ErrorCode);
    }

    [Fact]
    public void NativeSelectedValuesCannotCoerceWrongTypesNonFiniteDatesOrInvalidStrings()
    {
        var source = CosmicQuery<Holon>.Create();
        var wide = source.Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        Assert.Equal("CQL_DATA_INVALID", CosmicSelectedScalarResults.FromValue(wide, "7", true).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicSelectedScalarResults.FromValue(wide, 7d, true).ErrorCode);
        var real = source.Max(h => CosmicQueryMetadata.Value<double>(h, "rating")).Result;
        Assert.Equal("CQL_DATA_INVALID", CosmicSelectedScalarResults.FromValue(real, double.NaN, true).ErrorCode);
        var date = source.Min(h => CosmicQueryMetadata.Value<DateTime>(h, "when")).Result;
        Assert.Equal("CQL_DATA_INVALID", CosmicSelectedScalarResults.FromValue(date, new DateTime(1, DateTimeKind.Unspecified), true).ErrorCode);
        var text = source.Max(h => h.Name).Result;
        Assert.Equal("CQL_DATA_INVALID", CosmicSelectedScalarResults.FromValue(text, "\ud800", true).ErrorCode);
    }

    private sealed class Selected(ProviderType provider) : ICosmicQueryScalarStorageProvider
    {
        public int Calls;
        public ProviderType QueryProviderType => provider;
        public CosmicQueryCapabilities QueryCapabilities => Rows;
        public CosmicScalarCapabilities ScalarCapabilities { get; set; } = Caps();
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("Min/Max must not enumerate entities.");
        public Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            return Task.FromResult(CosmicSelectedScalarResults.FromValue(plan.Query, -9007199254740993L, true));
        }
    }

    [Fact]
    public async Task ExistingScalarExecutorPublishesLegitimateNegativeExtrema()
    {
        var adapter = new Selected(ProviderType.MongoDBOASIS);
        var query = CosmicQuery<Holon>.Create().Min(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var plan = CosmicScalarPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicScalarTarget(adapter.QueryProviderType, Rows, adapter.ScalarCapabilities) }).Result;
        var result = await CosmicScalarExecutor.ExecuteAsync(plan, new ICosmicQueryStorageProvider[] { adapter });
        Assert.False(result.IsError, result.Message); Assert.Equal(-9007199254740993L, result.Result.Providers.Single().Value);
        Assert.Equal(1, adapter.Calls);
    }

    [Fact]
    public async Task LaterActualReadRejectionPrecedesEveryEarlierDispatch()
    {
        var first = new Selected(ProviderType.MongoDBOASIS); var second = new Selected(ProviderType.Neo4jOASIS);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(first.QueryProviderType, second.QueryProviderType).Result)
            .Max(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var plan = CosmicScalarPlanner.Plan(query, first.QueryProviderType, new[]
        { new CosmicScalarTarget(first.QueryProviderType, Rows, Caps()), new CosmicScalarTarget(second.QueryProviderType, Rows, Caps()) }).Result;
        second.ScalarCapabilities = new CosmicScalarCapabilities(CosmicScalarOperation.Count);
        var result = await CosmicScalarExecutor.ExecuteAsync(plan, new ICosmicQueryStorageProvider[] { first, second });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode); Assert.Null(result.Result);
        Assert.Equal(0, first.Calls); Assert.Equal(0, second.Calls);
    }
}
