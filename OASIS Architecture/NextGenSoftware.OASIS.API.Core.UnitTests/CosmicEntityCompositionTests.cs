using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityCompositionTests
{
    private sealed class Native : ICosmicQueryEntityCompositionStorageProvider
    {
        public IHolon[]? Data;
        public ProviderType Identity = ProviderType.SQLLiteDBOASIS;
        public int RowCalls;
        public int StageCalls;
        public bool Enabled = true;
        public bool ReverseEnabled;
        public bool Incomplete;
        public string? Failure;
        public CosmicEntityCompositionPlan<Holon>? Seen;
        public Holon Value = new() { Id = Guid.NewGuid(), Name = "native" };
        public ProviderType QueryProviderType => Identity;
        public CosmicQueryCapabilities QueryCapabilities => Profile();
        public static CosmicQueryCapabilities Profile() => new(CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Paging,
            new[] { new CosmicQueryFieldCapability("Name", typeof(string), new[] { CosmicQueryComparisonOperator.Equal },
                Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true) });
        public CosmicEntityCompositionCapabilities EntityCompositionCapabilities => new(Enabled
            ? CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering | CosmicEntityStageFeatures.Paging | (ReverseEnabled ? CosmicEntityStageFeatures.Reverse : CosmicEntityStageFeatures.None) : CosmicEntityStageFeatures.None, Profile());
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        { RowCalls++; throw new InvalidOperationException("Entity composition cannot substitute a flat row read."); }
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            StageCalls++; if (typeof(T) == typeof(Holon)) Seen = (CosmicEntityCompositionPlan<Holon>)(object)plan;
            if (Data != null && Failure == null)
            {
                IEnumerable<IHolon> rows = Data;
                foreach (var predicate in plan.Query.Source.Predicates)
                    rows = rows.Where(row => CosmicSelectedPredicateEvaluation.Evaluate(predicate, _ => row.Name, cancellationToken));
                foreach (var ordering in plan.Query.Source.Orderings.Reverse())
                    rows = ordering.Descending ? rows.OrderByDescending(row => row.Name, StringComparer.Ordinal) : rows.OrderBy(row => row.Name, StringComparer.Ordinal);
                rows = rows.Skip(plan.Query.Source.Offset);
                if (plan.Query.Source.Limit.HasValue) rows = rows.Take(plan.Query.Source.Limit.Value);
                foreach (var stage in plan.Query.Stages)
                {
                    if (stage is CosmicEntityFilterStage<T> filter)
                        rows = rows.Where(row => filter.Predicate.Predicates.All(predicate =>
                            CosmicSelectedPredicateEvaluation.Evaluate(predicate, _ => row.Name, cancellationToken)));
                    else if (stage is CosmicEntityOrderStage order)
                        foreach (var key in order.Orderings.Reverse())
                            rows = key.Descending ? rows.OrderByDescending(row => row.Name, StringComparer.Ordinal) : rows.OrderBy(row => row.Name, StringComparer.Ordinal);
                    else if (stage is CosmicEntityReverseStage) rows=rows.Reverse();
                    else if (stage is CosmicEntityWindowStage page)
                        rows = page.Kind == CosmicSelectedWindowKind.Skip ? rows.Skip(page.Count) : rows.Take(page.Count);
                }
                return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(rows.ToArray()) { IsLoaded = !Incomplete });
            }
            return Task.FromResult(Failure == null ? new OASISResult<IReadOnlyList<IHolon>>(new IHolon[] { Value }) { IsLoaded = !Incomplete }
                : new OASISResult<IReadOnlyList<IHolon>> { IsError = true, ErrorCount = 1, ErrorCode = Failure, Message = "Native stage fault" });
        }
    }
    private sealed class Flat : ICosmicQueryStorageProvider
    {
        public int Calls;
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => Native.Profile();
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        { Calls++; throw new InvalidOperationException("A flat contract must not receive staged execution."); }
    }
    private static HolonManager Manager(string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }
    private static CosmicQuery<Holon> Source() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result);
    private static CosmicEntityCompositionPlan<Holon> Plan(Native native, CosmicEntityCompositionQuery<Holon> query)
    {
        var result = CosmicEntityCompositionPlanner.Plan(query, native.QueryProviderType,
            new CosmicQueryTarget(native.QueryProviderType, native.QueryCapabilities), native.EntityCompositionCapabilities);
        Assert.False(result.IsError, result.Message);
        return result.Result;
    }

    [Fact]
    public void SourceWindowAndEveryLaterOperatorRemainImmutableAndOrdered()
    {
        var source = Source().OrderBy(row => row.Name).Skip(2).Take(3).Result;
        var desired = "before";
        var filtered = source.Compose().Result.Where(row => row.Name == desired).Result;
        desired = "after";
        var ordered = filtered.OrderByDescending(row => row.Name).Result.ThenBy(row => row.Name).Result;
        var query = ordered.Take(2).Result.Where(row => row.Name == "later").Result;
        Assert.Same(source, query.Source);
        Assert.Equal(2, source.Offset); Assert.Equal(3, source.Limit);
        Assert.Equal(4, query.Stages.Count);
        Assert.Equal("before", Assert.IsType<CosmicQueryComparison>(Assert.Single(Assert.IsType<CosmicEntityFilterStage<Holon>>(query.Stages[0]).Predicate.Predicates)).Literal.Value);
        var sort = Assert.IsType<CosmicEntityOrderStage>(query.Stages[1]);
        Assert.Equal(2, sort.Orderings.Count); Assert.True(sort.Orderings[0].Descending);
        Assert.IsType<CosmicEntityWindowStage>(query.Stages[2]);
        Assert.IsType<CosmicEntityFilterStage<Holon>>(query.Stages[3]);
        Assert.Single(filtered.Stages);
    }

    [Fact]
    public async Task ExecutionSharesPhysicalOwnerAndReturnsDetachedRowsWithoutFlatFallback()
    {
        var native = new Native();
        var query = Source().Take(3).Result.Compose().Result.Where(row => row.Name == "native").Result.Take(1).Result;
        var plan = Plan(native, query);
        var result = await Manager().ExecuteEntityCompositionQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
        Assert.Equal(1, native.StageCalls); Assert.Equal(0, native.RowCalls);
        Assert.Same(plan.Options, native.Seen!.Options);
        Assert.Same(query, native.Seen.Query);
        var copy = Assert.Single(Assert.Single(result.Result.Providers).Rows);
        Assert.NotSame(native.Value, copy);
        copy.Name = "caller edit"; Assert.Equal("native", native.Value.Name);
        native.Failure = "CQL_QUERY_LIMIT";
        var failed = await Manager().ExecuteEntityCompositionQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_QUERY_LIMIT", failed.ErrorCode); Assert.Equal(0, native.RowCalls);
    }

    [Fact]
    public async Task IndependentStageAdmissionIsReadAgainBeforeIoAndLegacyIsRejected()
    {
        var native = new Native();
        var plan = Plan(native, Source().Take(2).Result.Compose().Result.Where(row => row.Name == "native").Result);
        native.Enabled = false;
        var rejected = await Manager().ExecuteEntityCompositionQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", rejected.ErrorCode); Assert.Equal(0, native.StageCalls);
        native.Enabled = true;
        var legacy = await Manager("legacy").ExecuteEntityCompositionQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_V2_REQUIRED", legacy.ErrorCode); Assert.Equal(0, native.StageCalls);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var interrupted = await Manager().ExecuteEntityCompositionQueryAsync(plan, new ICosmicQueryStorageProvider[] { native }, cancellationToken: cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", interrupted.ErrorCode); Assert.Equal(0, native.StageCalls);
    }

    [Fact]
    public void StageBoundsThenByAndExactDomainsFailBeforeExecution()
    {
        var query = Source().Take(2).Result.Compose().Result;
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED", query.ThenBy(row => row.Name).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT", query.Take(10001).ErrorCode);
        var native = new Native();
        var unsupported = query.OrderBy(row => row.Version).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicEntityCompositionPlanner.Plan(unsupported, native.QueryProviderType,
            new CosmicQueryTarget(native.QueryProviderType, native.QueryCapabilities), native.EntityCompositionCapabilities).ErrorCode);
        for (var index = 0; index < 32; index++) query = query.Take(1).Result;
        Assert.Equal("CQL_QUERY_LIMIT", query.Take(1).ErrorCode);
        Assert.Throws<ArgumentException>(() => new CosmicEntityCompositionOptions(maximumStageRows: 0));
        Assert.Equal(0, native.StageCalls);
    }
    [Fact]
    public async Task ForgedFlatPermissionAndIncompleteAcknowledgementCannotPublishRows()
    {
        var native = new Native();
        var plan = Plan(native, Source().Take(2).Result.Compose().Result.Where(row => row.Name == "native").Result);
        var flat = new Flat();
        var missing = await Manager().ExecuteEntityCompositionQueryAsync(plan, new ICosmicQueryStorageProvider[] { flat });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", missing.ErrorCode); Assert.Equal(0, flat.Calls);
        native.Incomplete = true;
        var unfinished = await Manager().ExecuteEntityCompositionQueryAsync(plan, new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", unfinished.ErrorCode); Assert.Null(unfinished.Result);
        Assert.Equal(0, native.RowCalls);
    }

}
