using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicLinqTests
{
    private sealed class Adapter : ICosmicQueryStorageProvider, ICosmicQueryScalarStorageProvider
    {
        public int Calls;
        public int ScalarCalls;
        public long NativeCount = 2;
        public bool DuplicateRows;
        private static readonly CosmicProjectionCapabilities NumericReads = new(
            new[] { new CosmicProjectionFieldCapability("Version",typeof(int)),new CosmicProjectionFieldCapability("Name",typeof(string)) },
            Array.Empty<Type>());
        public CosmicScalarCapabilities ScalarCapabilities => new(
            new[] { CosmicScalarOperation.Count,CosmicScalarOperation.LongCount,CosmicScalarOperation.Any,CosmicScalarOperation.All },
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum,NumericReads),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Average,NumericReads),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Min,NumericReads),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Max,NumericReads) });
        public Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan, CancellationToken cancellationToken = default) where T : IHolon
        {
            ScalarCalls++; Seen = (CosmicQuery<Holon>)(object)plan.Query.Source;
            return Task.FromResult(Fault == null ? plan.Query.Selector == null ? CosmicScalarResults.FromCount(plan.Query, NativeCount)
                : CosmicSelectedScalarResults.FromValue(plan.Query, typeof(TResult)==typeof(string) ? (object)"coffee" : 42, true)
                : new OASISResult<TResult> { IsError = true, ErrorCount = 1, ErrorCode = Fault, Message = "Owned scalar fault." });
        }
        public CosmicQuery<Holon>? Seen;
        public string? Fault;
        public bool BrokenCapabilities;
        public ProviderType Identity = ProviderType.SQLLiteDBOASIS;
        public ProviderType QueryProviderType => Identity;
        public CosmicQueryFeatures Features = CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Paging | CosmicQueryFeatures.TailPaging;
        public CosmicQueryCapabilities QueryCapabilities => BrokenCapabilities ? throw new InvalidOperationException("Owned capability fault") : new(Features,
            new[] { new CosmicQueryFieldCapability("Name", typeof(string),
                new[] { CosmicQueryComparisonOperator.Equal }, Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true) });
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            Seen = (object)plan.Query as CosmicQuery<Holon>;
            return Task.FromResult(Fault == null
                ? new OASISResult<IReadOnlyList<IHolon>>(plan.Query.Limit == 0 ? Array.Empty<IHolon>() : Enumerable.Range(0, DuplicateRows ? 2 : 1).Select(_ => (IHolon)new Holon { Id = Guid.NewGuid(), Name = "coffee" }).ToArray())
                : new OASISResult<IReadOnlyList<IHolon>> { IsError = true, ErrorCount = 1, ErrorCode = Fault, Message = "Owned native fault." });
        }
    }

    private static HolonManager Manager(string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }

    private static IQueryable<Holon> Root(Adapter adapter, HolonManager? manager = null)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result);
        var result = CosmicLinq.Create(manager ?? Manager(), adapter, source);
        Assert.False(result.IsError, result.Message);
        return result.Result;
    }

    [Fact]
    public async Task StandardQueryableOperatorsBindPerExecutionAndDeferNativeIo()
    {
        var adapter = new Adapter(); var root = Root(adapter); var name = "coffee";
        var query = root.Where(row => row.Name == name).OrderBy(row => row.Name).ThenByDescending(row => row.Name).Skip(2).Take(3);
        name = "changed";
        Assert.Equal(0, adapter.Calls);
        Assert.IsAssignableFrom<IOrderedQueryable<Holon>>(query);
        var result = await query.ToCosmicListAsync();
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.True(result.IsLoaded); Assert.Single(result.Result); Assert.Equal(1, adapter.Calls);
        Assert.Equal(2, adapter.Seen!.Offset); Assert.Equal(3, adapter.Seen.Limit);
        Assert.Equal(2, adapter.Seen.Orderings.Count);
        Assert.Equal("changed", Assert.IsType<CosmicQueryComparison>(Assert.Single(adapter.Seen.Predicates)).Literal.Value);
        var original = await root.ToCosmicListAsync();
        Assert.False(original.IsError, original.Message);
        Assert.Empty(adapter.Seen!.Predicates); Assert.Equal(0, adapter.Seen.Offset);
    }

    [Fact]
    public void StandardEnumerationUsesNativeRowsAndExposesRealErrors()
    {
        var adapter = new Adapter(); var query = Root(adapter).Where(row => row.Name == "coffee");
        Assert.Equal("coffee", Assert.Single(query.ToList()).Name); Assert.Equal(1, adapter.Calls);
        adapter.Fault = "OWNED_NATIVE_ERROR";
        Assert.Equal("OWNED_NATIVE_ERROR", Assert.Throws<CosmicLinqException>(() => query.ToList()).Code);
    }

    [Fact]
    public async Task AsyncCancellationCapabilityDriftAndIdentityDriftStopBeforeIo()
    {
        var adapter = new Adapter(); var query = Root(adapter).OrderBy(row => row.Name);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED", (await query.ToCosmicListAsync(cancellation.Token)).ErrorCode);
        adapter.Features = CosmicQueryFeatures.None;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", (await query.ToCosmicListAsync()).ErrorCode);
        adapter.Identity = ProviderType.MongoDBOASIS;
        Assert.Equal("CQL_EXECUTION_INVALID", (await query.ToCosmicListAsync()).ErrorCode);
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public void UnsupportedOperatorsNeverDispatchOrInvokeClientCode()
    {
        var adapter = new Adapter(); var query = Root(adapter);
        Assert.Throws<CosmicLinqException>(() => query.Where((row, index) => index > 0));
        Assert.Throws<CosmicLinqException>(() => query.Select(row => row.Name.ToUpperInvariant()));
        Assert.Throws<CosmicLinqException>(() => query.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase));
        Assert.Throws<CosmicLinqException>(() => query.Reverse());
        Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public async Task ForeignLocalSourcesAndLegacyModeCannotBecomeClientFallbacks()
    {
        Assert.Equal("CQL_QUERY_PROVIDER_INVALID", (await Array.Empty<Holon>().AsQueryable().ToCosmicListAsync()).ErrorCode);
        var adapter = new Adapter();
        Assert.Equal("CQL_V2_REQUIRED", (await Root(adapter, Manager(HyperDriveModes.Legacy)).ToCosmicListAsync()).ErrorCode);
        Assert.Equal(0, adapter.Calls);
        Assert.Equal("CQL_SCOPE_INVALID", CosmicLinq.Create(Manager(), adapter, CosmicQuery<Holon>.Create()).ErrorCode);
    }

    [Fact]
    public void PostWindowFilteringPreservesTheExistingExplicitCompositionError()
    {
        var adapter = new Adapter();
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",
            Assert.Throws<CosmicLinqException>(() => Root(adapter).Take(1).Where(row => row.Name == "coffee")).Code);
        Assert.Equal(0, adapter.Calls);
    }
    [Fact]
    public async Task NegativeStandardPagingHasLinqSemanticsWithoutChangingCqlBuilders()
    {
        var adapter = new Adapter();
        var result = await Root(adapter).Skip(-2).Take(-3).ToCosmicListAsync();
        Assert.False(result.IsError, result.Message);
        Assert.Empty(result.Result); Assert.Equal(0, adapter.Seen!.Offset); Assert.Equal(0, adapter.Seen.Limit);
    }

    [Fact]
    public async Task BrokenProviderContractsRemainExplicitAsyncErrors()
    {
        var adapter = new Adapter(); var query = Root(adapter);
        adapter.BrokenCapabilities = true;
        var result = await query.ToCosmicListAsync();
        Assert.Equal("CQL_EXECUTION_FAILED", result.ErrorCode);
        Assert.Contains("Owned capability fault", result.Exception.Message);
        Assert.Null(result.Result); Assert.False(result.IsLoaded); Assert.Equal(0, adapter.Calls);
    }
    [Fact]
    public void StandardCardinalityAndAllUseNativeScalarExecutionWithoutRows()
    {
        var adapter = new Adapter(); var root = Root(adapter);
        Assert.Equal(2, root.Skip(1).Take(3).Count());
        Assert.Equal(1, adapter.Seen!.Offset); Assert.Equal(3, adapter.Seen.Limit);
        Assert.Equal(2L, root.LongCount()); Assert.True(root.Any());
        Assert.False(root.Take(1).All(row => row.Name == "coffee"));
        Assert.Equal(1, adapter.Seen!.Limit);
        adapter.NativeCount = 0;
        Assert.True(root.All(row => row.Name == "coffee"));
        Assert.Equal(5, adapter.ScalarCalls); Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public void TerminalPredicatesNeverMoveAheadOfAnExistingWindow()
    {
        var adapter = new Adapter(); var root = Root(adapter);
        Assert.Equal(2, root.Count(row => row.Name == "coffee"));
        Assert.Single(adapter.Seen!.Predicates);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",
            Assert.Throws<CosmicLinqException>(() => root.Take(1).Count(row => row.Name == "coffee")).Code);
        Assert.Equal(1, adapter.ScalarCalls); Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public void ScalarProviderFailuresPreserveTheirErrorAndNeverFetchRows()
    {
        var adapter = new Adapter { Fault = "OWNED_SCALAR_ERROR" };
        Assert.Equal("OWNED_SCALAR_ERROR", Assert.Throws<CosmicLinqException>(() => Root(adapter).Count()).Code);
        Assert.Equal(1, adapter.ScalarCalls); Assert.Equal(0, adapter.Calls);
    }
    public sealed class ProjectionDto
    {
        public static int Constructions;
        public ProjectionDto() { Constructions++; }
        public string Name { get; set; } = "";
    }

    [Fact]
    public async Task SelectNeverInvokesConstructorsOrFetchesEntityRowsWithoutProjectionCapability()
    {
        ProjectionDto.Constructions = 0;
        var adapter = new Adapter();
        var selected = Root(adapter).Select(row => new ProjectionDto { Name = row.Name });
        Assert.Equal(0, ProjectionDto.Constructions); Assert.Equal(0, adapter.Calls);
        var result = await selected.ToCosmicListAsync();
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
        Assert.Null(result.Result); Assert.False(result.IsLoaded);
        Assert.Equal(0, ProjectionDto.Constructions); Assert.Equal(0, adapter.Calls);
        Assert.Equal(0, adapter.ScalarCalls);
    }

    [Fact]
    public async Task SelectedCancellationAndUnsupportedCallbacksRemainExplicit()
    {
        var adapter = new Adapter(); var root = Root(adapter);
        Assert.Throws<CosmicLinqException>(() => root.Select((row, index) => row.Name));
        var selected = root.Select(row => row.Name).Where(value => value == "coffee").Take(1);
        Assert.Throws<CosmicLinqException>(() => selected.Select(value => value.ToUpperInvariant()));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED", (await selected.ToCosmicListAsync(cancellation.Token)).ErrorCode);
        Assert.Equal(0, adapter.Calls); Assert.Equal(0, adapter.ScalarCalls);
    }
    [Fact]
    public void EntityElementTerminalsReuseNativeCardinalityAndTypedHydration()
    {
        var adapter = new Adapter(); var root = Root(adapter);
        Assert.Equal("coffee", root.First().Name);
        Assert.Equal("coffee", root.Single().Name);
        Assert.Equal("coffee", root.OrderBy(row => row.Name).Last().Name);
        Assert.Equal("coffee", root.ElementAt(0).Name);
        Assert.Equal("coffee", root.OrderBy(row => row.Name).ElementAt(^1).Name);
        Assert.Equal(5, adapter.Calls); Assert.Equal(0, adapter.ScalarCalls);
    }

    [Fact]
    public void StandardDefaultArgumentsRequireSuccessfulEmptyProof()
    {
        var adapter = new Adapter(); var empty = Root(adapter).Take(0);
        var supplied = new Holon { Name = "caller default" };
        Assert.Null(empty.FirstOrDefault());
        Assert.Same(supplied, empty.FirstOrDefault(supplied));
        Assert.Same(supplied, empty.SingleOrDefault(supplied));
        Assert.Null(empty.ElementAtOrDefault(-1));
        Assert.Equal(0, adapter.ScalarCalls);
    }

    [Fact]
    public void DefaultArgumentsNeverMaskNativeFailuresOrAmbiguousCardinality()
    {
        var adapter = new Adapter { Fault = "OWNED_ELEMENT_ERROR" };
        var root = Root(adapter); var supplied = new Holon();
        Assert.Equal("OWNED_ELEMENT_ERROR", Assert.Throws<CosmicLinqException>(() => root.FirstOrDefault(supplied)).Code);
        adapter.Fault = null; adapter.DuplicateRows = true;
        Assert.Equal("CQL_SEQUENCE_MULTIPLE", Assert.Throws<CosmicLinqException>(() => root.SingleOrDefault(supplied)).Code);
    }

    public sealed class ConstructedHolon : Holon
    {
        public static int Constructions;
        public ConstructedHolon() { Constructions++; }
    }

    [Fact]
    public void AmbiguousSingleNeverInvokesAnApplicationConstructor()
    {
        ConstructedHolon.Constructions = 0;
        var adapter = new Adapter { DuplicateRows = true };
        var source = CosmicQuery<ConstructedHolon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result);
        var query = CosmicLinq.Create(Manager(), adapter, source).Result;
        Assert.Equal("CQL_SEQUENCE_MULTIPLE", Assert.Throws<CosmicLinqException>(() => query.Single()).Code);
        Assert.Equal(0, ConstructedHolon.Constructions);
    }

    [Fact]
    public void ElementPredicatesDoNotMoveAheadOfAWindowAndLastNeedsOrdering()
    {
        var adapter = new Adapter(); var root = Root(adapter);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => root.Take(1).First(row => row.Name == "coffee")).Code);
        Assert.Equal("CQL_ORDER_REQUIRED", Assert.Throws<CosmicLinqException>(() => root.Last()).Code);
        Assert.Equal(0, adapter.Calls);
    }
    [Fact]
    public void StandardValueAggregatesUseTheNativeScalarContractWithoutRows()
    {
        var adapter = new Adapter(); var root = Root(adapter);
        Assert.Equal(42, root.Sum(row => row.Version));
        Assert.Equal("coffee", root.Min(row => row.Name));
        Assert.Equal(42, root.Select(row => row.Version).Sum());
        Assert.Equal(3, adapter.ScalarCalls); Assert.Equal(0, adapter.Calls);
    }

    [Fact]
    public void AggregateDomainMismatchesAndComputedSelectorsNeverBecomeClientCasts()
    {
        var adapter = new Adapter(); var root = Root(adapter);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>root.Average(row=>row.Version)).Code);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>root.Select(row=>row.Version).Average()).Code);
        Assert.Throws<CosmicLinqException>(()=>root.Sum(row=>row.Version+1));
        Assert.Throws<CosmicLinqException>(()=>root.Select(row=>row.Version).Take(1).Sum());
        Assert.Equal(0,adapter.ScalarCalls);Assert.Equal(0,adapter.Calls);
    }
}