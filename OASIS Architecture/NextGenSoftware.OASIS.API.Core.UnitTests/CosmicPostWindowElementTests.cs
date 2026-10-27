using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicPostWindowElementTests
{
    private sealed class Native : ICosmicQueryStorageProvider
    {
        public bool Enabled = true;
        public int Calls;
        public CosmicQueryPlan<Holon>? Seen;
        public string? Failure;
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => new(
            CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Paging | CosmicQueryFeatures.TailPaging
                | (Enabled ? CosmicQueryFeatures.PostWindowElementPredicate : CosmicQueryFeatures.None),
            new[] { new CosmicQueryFieldCapability("Name", typeof(string), Enum.GetValues<CosmicQueryComparisonOperator>(),
                Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true) });
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            Seen = (CosmicQueryPlan<Holon>)(object)plan;
            if (Failure != null) return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>
                { IsError = true, ErrorCount = 1, ErrorCode = Failure, Message = "Owned terminal fault" });
            var rows = new[] { "a", "b", "c", "d" }.Select((name, index) => (IHolon)new Holon
                { Id = Guid.Parse($"10000000-0000-0000-0000-{index + 1:D12}"), Name = name });
            // Simulate the provider contract, not a production client-side implementation.
            rows = rows.Skip(plan.Query.Offset);
            if (plan.Query.Limit.HasValue) rows = rows.Take(plan.Query.Limit.Value);
            if (plan.PostWindowPredicate != null)
                rows = rows.Where(row => plan.PostWindowPredicate.Predicates.All(predicate =>
                    CosmicSelectedPredicateEvaluation.Evaluate(predicate, _ => row.Name, cancellationToken)));
            if (plan.TailIndex.HasValue) rows = rows.Reverse().Skip(plan.TailIndex.Value - 1);
            rows = rows.Take(plan.PostWindowLimit ?? maximumResults);
            return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(rows.ToArray()) { IsLoaded = true });
        }
    }

    private static (HolonManager Manager, IQueryable<Holon> Query) Setup(Native native, string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        return (manager, CosmicLinq.Create(manager, native,
            CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)).Result);
    }

    [Fact]
    public void PredicatesStayAfterSourcePageBeforeFirstSingleAndLast()
    {
        var native = new Native(); var (_, query) = Setup(native);
        var page = query.OrderBy(row => row.Name).Skip(1).Take(2);
        Assert.Equal("c", page.First(row => row.Name == "c").Name);
        Assert.Equal(1, native.Seen!.Query.Offset);
        Assert.Equal(2, native.Seen.Query.Limit);
        Assert.Empty(native.Seen.Query.Predicates);
        Assert.NotNull(native.Seen.PostWindowPredicate);
        Assert.Equal(1, native.Seen.PostWindowLimit);
        Assert.Equal("c", page.Single(row => row.Name == "c").Name);
        Assert.Equal(2, native.Seen.PostWindowLimit);
        Assert.Equal("c", page.Last(row => row.Name != "missing").Name);
        Assert.Equal(1, native.Seen.TailIndex);
        Assert.Null(page.FirstOrDefault(row => row.Name == "d"));
        Assert.Equal("CQL_SEQUENCE_EMPTY", Assert.Throws<CosmicLinqException>(() => page.First(row => row.Name == "d")).Code);
        Assert.Equal("CQL_SEQUENCE_MULTIPLE", Assert.Throws<CosmicLinqException>(() => page.Single(row => row.Name != "missing")).Code);
    }

    [Fact]
    public void PredicateCaptureDefaultsAndNativeFailuresKeepTheirOwners()
    {
        var native = new Native(); var (_, query) = Setup(native);
        var page = query.OrderBy(row => row.Name).Take(3);
        var name = "b";
        Assert.Equal("b", page.First(row => row.Name == name).Name);
        name = "c";
        Assert.Equal("c", page.First(row => row.Name == name).Name);
        var supplied = new Holon { Name = "caller default" };
        Assert.Same(supplied, page.FirstOrDefault(row => row.Name == "missing", supplied));
        Assert.Same(supplied, page.LastOrDefault(row => row.Name == "missing", supplied));
        native.Failure = "CQL_QUERY_TIMEOUT";
        Assert.Equal("CQL_QUERY_TIMEOUT", Assert.Throws<CosmicLinqException>(() => page.SingleOrDefault(row => row.Name == "missing", supplied)).Code);
    }

    [Fact]
    public async Task ActualCapabilitiesAreReadmittedBeforeIoAndLegacyStaysRejected()
    {
        var native = new Native(); var (manager, _) = Setup(native);
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result).Take(2).Result;
        var element = source.First(row => row.Name == "b").Result;
        var planned = CosmicElementPlanner.Plan(element, native.QueryProviderType,
            new[] { new CosmicQueryTarget(native.QueryProviderType, native.QueryCapabilities) });
        Assert.False(planned.IsError, planned.Message);
        native.Enabled = false;
        var rejected = await manager.ExecuteElementQueryAsync(planned.Result, new ICosmicQueryStorageProvider[] { native });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", rejected.ErrorCode);
        Assert.Equal(0, native.Calls);
        native.Enabled = true;
        var (_, legacy) = Setup(native, "legacy");
        Assert.Equal("CQL_V2_REQUIRED", Assert.Throws<CosmicLinqException>(() => legacy.Take(2).First(row => row.Name == "b")).Code);
        Assert.Equal(0, native.Calls);
    }
}
