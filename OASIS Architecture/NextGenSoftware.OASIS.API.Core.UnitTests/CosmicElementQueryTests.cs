using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicElementQueryTests
{
    public sealed class MenuItem : Holon
    {
        [CustomOASISProperty] public int Stock { get; set; }
    }

    public sealed class ThrowingItem : Holon
    {
        public static int SetterCalls;
        [CustomOASISProperty] public int Stock
        {
            get => 0;
            set { SetterCalls++; throw new InvalidOperationException("application setter"); }
        }
    }

    // Apply the admitted native window, so these tests distinguish a terminal
    // over its source from a terminal which silently replaces Skip/Take.
    private sealed class Adapter : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => new(
            CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
        public IHolon[] Rows = Array.Empty<IHolon>();
        public int Calls, Budget;
        public string? Failure;
        public bool Warning;

        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
            CosmicQueryPlan<T> plan, int maximumResults = 100,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            Budget = maximumResults;
            if (Failure != null)
                return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>
                    { IsError = true, ErrorCode = Failure, Message = "native failure" });

            IReadOnlyList<IHolon> selected = Rows.Skip(plan.Query.Offset)
                .Take(plan.Query.Limit ?? Rows.Length).ToArray();
            return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(selected)
                { IsLoaded = true, IsWarning = Warning });
        }
    }

    private static HolonManager Manager(string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA
            { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }

    private static CosmicQuery<MenuItem> Source() => CosmicQuery<MenuItem>.Create();

    private static CosmicElementPlan<MenuItem> Plan(
        Adapter adapter, CosmicElementQuery<MenuItem> query)
    {
        var result = CosmicElementPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        Assert.False(result.IsError, result.Message);
        return result.Result;
    }

    [Theory]
    [InlineData(false, false, 0, "CQL_SEQUENCE_EMPTY")]
    [InlineData(false, true, 0, null)]
    [InlineData(false, false, 3, null)]
    [InlineData(false, true, 3, null)]
    [InlineData(true, false, 0, "CQL_SEQUENCE_EMPTY")]
    [InlineData(true, true, 0, null)]
    [InlineData(true, false, 1, null)]
    [InlineData(true, true, 1, null)]
    [InlineData(true, false, 3, "CQL_SEQUENCE_MULTIPLE")]
    [InlineData(true, true, 3, "CQL_SEQUENCE_MULTIPLE")]
    public async Task NativeCardinalityDeterminesValueDefaultOrExplicitError(
        bool single, bool orDefault, int count, string? error)
    {
        var adapter = new Adapter
        {
            Rows = Enumerable.Range(0, count).Select(index => (IHolon)new Holon
            {
                Id = Guid.NewGuid(), Name = "item " + index,
                MetaData = new() { ["Stock"] = 7 }
            }).ToArray()
        };
        var intent = single
            ? (orDefault ? Source().SingleOrDefault() : Source().Single())
            : (orDefault ? Source().FirstOrDefault() : Source().First());
        Assert.False(intent.IsError);

        var result = await Manager().ExecuteElementQueryAsync(Plan(adapter, intent.Result), new[] { adapter });

        Assert.Equal(1, adapter.Calls);
        Assert.Equal(single ? 2 : 1, adapter.Budget);
        Assert.Equal(error != null, result.IsError);
        if (error != null)
        {
            Assert.Equal(error, result.ErrorCode);
            Assert.False(result.IsLoaded);
            Assert.Null(result.Result);
        }
        else
        {
            Assert.True(result.IsLoaded);
            if (count == 0) Assert.Null(result.Result);
            else
            {
                Assert.Equal(adapter.Rows[0].Id, result.Result.Id);
                Assert.Equal(7, result.Result.Stock);
                Assert.NotSame(adapter.Rows[0], result.Result);
                Assert.False(HolonChangeTracking.HasChanges(result.Result));
            }
        }
    }

    [Fact]
    public void TerminalPreservesTheOriginalWindowWithoutMutatingItsSource()
    {
        var source = Source().Skip(4).Take(8).Result;
        var plan = Plan(new Adapter(), source.First().Result);
        Assert.Same(source, plan.Query.Source);
        Assert.Equal(4, plan.Rows.Query.Offset);
        Assert.Equal(1, plan.Rows.Query.Limit);
        Assert.Equal(8, source.Limit);
        Assert.Equal(0, Plan(new Adapter(), source.Take(0).SingleOrDefault().Result).Rows.Query.Limit);
        Assert.Equal(1, Plan(new Adapter(), source.Take(1).Single().Result).Rows.Query.Limit);
    }

    [Fact]
    public void UnsupportedPagingAndAmbiguousMultiProviderSequencesFailBeforeIo()
    {
        var provider = ProviderType.SQLLiteDBOASIS;
        var noPaging = new CosmicQueryCapabilities(CosmicQueryFeatures.None,
            Array.Empty<CosmicQueryFieldCapability>());
        var rejected = CosmicElementPlanner.Plan(Source().First().Result, provider,
            new[] { new CosmicQueryTarget(provider, noPaging) });
        Assert.True(rejected.IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", rejected.ErrorCode);

        var adapter = new Adapter();
        var all = CosmicQuery<MenuItem>.Create(CosmicQueryScope.AllEligible).First().Result;
        var multiple = CosmicElementPlanner.Plan(all, provider, new[]
        {
            new CosmicQueryTarget(provider, adapter.QueryCapabilities),
            new CosmicQueryTarget(ProviderType.MongoDBOASIS, adapter.QueryCapabilities)
        });
        Assert.True(multiple.IsError);
        Assert.Equal(0, adapter.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrDefaultNeverHidesNativeFailureOrIncompleteResults(bool warning)
    {
        var adapter = new Adapter { Warning = warning, Failure = warning ? null : "TEST_NATIVE_FAILURE" };
        var result = await Manager().ExecuteElementQueryAsync(
            Plan(adapter, Source().FirstOrDefault().Result), new[] { adapter });
        Assert.True(result.IsError);
        Assert.False(result.IsLoaded);
        Assert.Null(result.Result);
        Assert.Equal(warning ? "CQL_PROVIDER_RESULT_INVALID" : "TEST_NATIVE_FAILURE", result.ErrorCode);
    }

    [Fact]
    public async Task CancellationAndLegacyModeDoNotBecomeEmptyDefaults()
    {
        var adapter = new Adapter();
        var plan = Plan(adapter, Source().FirstOrDefault().Result);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await Manager().ExecuteElementQueryAsync(plan, new[] { adapter }, cancelled.Token);
        Assert.True(result.IsError);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        var legacy = await Manager("V1").ExecuteElementQueryAsync(plan, new[] { adapter });
        Assert.Equal("CQL_V2_REQUIRED", legacy.ErrorCode);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public void FailedSourceCompositionRemainsAnErrorRatherThanBecomingANewQuery()
    {
        var failed = new OASISResult<CosmicQuery<MenuItem>>
            { IsError = true, ErrorCode = "SOURCE_REJECTED", Message = "source failed" };
        Assert.Equal("SOURCE_REJECTED", failed.First().ErrorCode);
        Assert.Equal("SOURCE_REJECTED", failed.FirstOrDefault().ErrorCode);
        Assert.Equal("SOURCE_REJECTED", failed.Single().ErrorCode);
        Assert.Equal("SOURCE_REJECTED", failed.SingleOrDefault().ErrorCode);
    }

    [Fact]
    public async Task AmbiguousSingleDoesNotInvokeApplicationMaterialization()
    {
        ThrowingItem.SetterCalls = 0;
        var adapter = new Adapter
        {
            Rows = Enumerable.Range(0, 2).Select(_ => (IHolon)new Holon
                { Id = Guid.NewGuid(), MetaData = new() { ["Stock"] = 7 } }).ToArray()
        };
        var plan = CosmicElementPlanner.Plan(CosmicQuery<ThrowingItem>.Create().Single().Result,
            adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }).Result;
        var result = await Manager().ExecuteElementQueryAsync(plan, new[] { adapter });
        Assert.Equal("CQL_SEQUENCE_MULTIPLE", result.ErrorCode);
        Assert.Equal(0, ThrowingItem.SetterCalls);
    }
}
