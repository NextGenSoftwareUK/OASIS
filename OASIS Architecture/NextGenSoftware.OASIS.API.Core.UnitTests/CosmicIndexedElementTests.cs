using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Moq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicIndexedElementTests
{
    private sealed class Adapter : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => new(
            CosmicQueryFeatures.Paging | CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.BooleanConstant,
            new[] { typeof(Guid), typeof(bool) }, new[] { CosmicQueryComparisonOperator.Equal });
        public IHolon[] Rows = Enumerable.Range(0, 3).Select(index => (IHolon)new Holon
            { Id = Guid.NewGuid(), Name = "item " + index }).ToArray();
        public int Calls, Offset, Limit, Budget;
        public string? Failure;

        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            Offset = plan.Query.Offset;
            Limit = plan.Query.Limit ?? -1;
            Budget = maximumResults;
            return Task.FromResult(Failure != null
                ? new OASISResult<IReadOnlyList<IHolon>> { IsError = true, ErrorCode = Failure, Message = "native failure" }
                : new OASISResult<IReadOnlyList<IHolon>>(Rows.Skip(Offset).Take(Limit).ToArray()) { IsLoaded = true });
        }
    }

    private static HolonManager Manager()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }

    private static CosmicElementPlan<Holon> Plan(Adapter adapter, CosmicElementQuery<Holon> query)
    {
        var result = CosmicElementPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        Assert.False(result.IsError, result.Message);
        return result.Result;
    }

    [Theory]
    [InlineData(0, false, "item 0")]
    [InlineData(1, false, "item 1")]
    [InlineData(2, true, "item 2")]
    [InlineData(3, false, null)]
    [InlineData(3, true, null)]
    public async Task NativeIndexReturnsExactlyOneOrTheDeclaredOutOfRangeResult(int index, bool orDefault, string? name)
    {
        var adapter = new Adapter();
        var source = CosmicQuery<Holon>.Create();
        var query = (orDefault ? source.ElementAtOrDefault(index) : source.ElementAt(index)).Result;
        var result = await Manager().ExecuteElementQueryAsync(Plan(adapter, query), new[] { adapter });

        Assert.Equal(1, adapter.Calls);
        Assert.Equal(index, adapter.Offset);
        Assert.Equal(1, adapter.Limit);
        Assert.Equal(1, adapter.Budget);
        Assert.Equal(name == null && !orDefault, result.IsError);
        if (result.IsError)
        {
            Assert.Equal("CQL_INDEX_OUT_OF_RANGE", result.ErrorCode);
            Assert.False(result.IsLoaded);
            Assert.Null(result.Result);
        }
        else
        {
            Assert.True(result.IsLoaded);
            Assert.Equal(name, result.Result?.Name);
            if (result.Result != null) Assert.False(HolonChangeTracking.HasChanges(result.Result));
        }
    }

    [Fact]
    public async Task IndexIsRelativeToTheComposedWindow()
    {
        var adapter = new Adapter();
        var source = CosmicQuery<Holon>.Create().Skip(1).Take(2).Result;
        var plan = Plan(adapter, source.ElementAt(1).Result);
        Assert.Same(source, plan.Query.Source);
        Assert.Equal(1, source.Offset);
        Assert.Equal(2, source.Limit);
        var result = await Manager().ExecuteElementQueryAsync(plan, new[] { adapter });
        Assert.False(result.IsError, result.Message);
        Assert.Equal("item 2", result.Result.Name);
        Assert.Equal(2, adapter.Offset);
        Assert.Equal(1, adapter.Limit);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task NegativeDefaultIsKnownWithoutAProviderRead(int index)
    {
        var adapter = new Adapter { Failure = "MUST_NOT_READ" };
        var source = CosmicQuery<Holon>.Create();
        Assert.Equal("CQL_INDEX_OUT_OF_RANGE", source.ElementAt(index).ErrorCode);
        var result = await Manager().ExecuteElementQueryAsync(
            Plan(adapter, source.ElementAtOrDefault(index).Result), new[] { adapter });
        Assert.False(result.IsError, result.Message);
        Assert.True(result.IsLoaded);
        Assert.Null(result.Result);
        Assert.Equal(0, adapter.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndexOutsideAnExplicitTakeNeverReadsPastThatWindow(bool orDefault)
    {
        var adapter = new Adapter { Failure = "MUST_NOT_READ" };
        var source = CosmicQuery<Holon>.Create().Skip(1).Take(1).Result;
        var query = (orDefault ? source.ElementAtOrDefault(int.MaxValue) : source.ElementAt(int.MaxValue)).Result;
        var result = await Manager().ExecuteElementQueryAsync(Plan(adapter, query), new[] { adapter });
        Assert.Equal(!orDefault, result.IsError);
        if (!orDefault) Assert.Equal("CQL_INDEX_OUT_OF_RANGE", result.ErrorCode);
        else Assert.True(result.IsLoaded);
        Assert.Null(result.Result);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public void OffsetAdditionUsesTheExistingBoundRatherThanOverflowing()
    {
        var adapter = new Adapter();
        var source = CosmicQuery<Holon>.Create().Skip(CosmicQuery<Holon>.MaximumOffset).Result;
        var result = CosmicElementPlanner.Plan(source.ElementAt(1).Result, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public async Task KnownEmptyStillHonoursCancellation()
    {
        var adapter = new Adapter();
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        var result = await Manager().ExecuteElementQueryAsync(
            Plan(adapter, CosmicQuery<Holon>.Create().ElementAtOrDefault(-1).Result), new[] { adapter }, stop.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.False(result.IsLoaded);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public async Task DefaultDoesNotHideNativeFailureForAnUnknownSequenceLength()
    {
        var adapter = new Adapter { Failure = "NATIVE_INDEX_FAILURE" };
        var result = await Manager().ExecuteElementQueryAsync(
            Plan(adapter, CosmicQuery<Holon>.Create().ElementAtOrDefault(1).Result), new[] { adapter });
        Assert.Equal("NATIVE_INDEX_FAILURE", result.ErrorCode);
        Assert.False(result.IsLoaded);
        Assert.Null(result.Result);
    }

    [Fact]
    public void FailedCompositionIsPreservedByBothOverloads()
    {
        var source = new OASISResult<CosmicQuery<Holon>> { IsError = true, ErrorCode = "SOURCE_REJECTED" };
        Assert.Equal("SOURCE_REJECTED", source.ElementAt(0).ErrorCode);
        Assert.Equal("SOURCE_REJECTED", source.ElementAtOrDefault(0).ErrorCode);
    }

    [Fact]
    public async Task KnownEmptyStillChecksTheInheritedActorWithoutCreatingAnAttempt()
    {
        var adapter = new Adapter();
        var source = CosmicQuery<Holon>.Create()
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var operation = new UsageOutboxOperation
        {
            Id = "indexed-empty", UserId = Guid.NewGuid().ToString("D"),
            ExecutionOwner = "owner", Policy = new UsageEndpointPolicy()
        };
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var handle = new UsageExecutionHandle(operation, store.Object, new Web4UsageLedgerOptions(),
            new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        var policy = new PreparedStorageExecution(handle, "indexed-empty", new Dictionary<ProviderType, string>(), Array.Empty<ProviderType>());
        var result = await policy.RunAsync(() => Manager().ExecuteElementQueryAsync(
            Plan(adapter, source.ElementAtOrDefault(-1).Result), new[] { adapter }));
        Assert.Equal("CQL_ACTOR_MISMATCH", result.ErrorCode);
        Assert.False(result.IsLoaded);
        Assert.Equal(0, adapter.Calls);
        store.VerifyNoOtherCalls();
    }
}
