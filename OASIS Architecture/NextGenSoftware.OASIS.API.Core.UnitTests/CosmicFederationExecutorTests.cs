using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicFederationExecutorTests
{
    private static readonly ProviderType[] Providers =
        { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS };
    private static CosmicQueryFieldCapability Field(string name, Type type) => new(name, type,
        new[] { CosmicQueryComparisonOperator.Equal }, Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true);
    private static readonly CosmicQueryCapabilities Capabilities = new(
        CosmicQueryFeatures.Metadata | CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Paging,
        new[] { Field("Name", typeof(string)), Field("Id", typeof(Guid)), Field("CreatedDate", typeof(DateTime)) },
        new[] { typeof(long), typeof(long?), typeof(decimal), typeof(string), typeof(float), typeof(double), typeof(Guid), typeof(DateTime) }
            .Select(type => new CosmicQueryMetadataCapability(type, new[] { CosmicQueryComparisonOperator.Equal }, supportsOrdering: true)).ToArray());

    private sealed class Adapter(ProviderType provider, params IHolon[] rows) : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => provider;
        public CosmicQueryCapabilities QueryCapabilities => Capabilities;
        public int Calls;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            return Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(rows));
        }
    }

    private static CosmicFederationPlan<Holon> Plan(CosmicQuery<Holon>? source = null, CosmicFederationOptions? options = null)
    {
        var result = CosmicFederationPlanner.Plan(source ?? CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible),
            options ?? CosmicFederationOptions.Create().Result, Providers[0],
            Providers.Select(provider => new CosmicQueryTarget(provider, Capabilities)).ToArray());
        Assert.False(result.IsError, result.Message);
        return result.Result;
    }

    private static Holon Row(string name, Guid? id = null) => new() { Id = id ?? Guid.NewGuid(), Name = name };

    [Fact]
    public async Task GlobalOrderingAndWindowApplyAfterAllThreeCompleteSources()
    {
        var adapters = new[] { new Adapter(Providers[0], Row("Z")), new Adapter(Providers[1], Row("A")),
            new Adapter(Providers[2], Row("M")) };
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).OrderBy(row => row.Name).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(source,
            CosmicFederationOptions.Create(skip: 1, take: 1).Result), adapters);
        Assert.False(result.IsError, result.Message);
        Assert.Equal("M", Assert.Single(result.Result.Rows).Selected.Row.Name);
        Assert.Equal(3, result.Result.ObservedInputRows);
        Assert.Equal(3, result.Result.ResolvedRows);
        Assert.All(adapters, adapter => Assert.Equal(1, adapter.Calls));
        Assert.Equal(CosmicFederationConsistency.MovingProviderReads, result.Result.Consistency);
    }

    [Fact]
    public async Task PreservedReplicaTiesUseLogicalIdThenConcreteProvider()
    {
        var id = Guid.Parse("10000000-0000-0000-0000-000000000000");
        var adapters = Providers.Select(provider => new Adapter(provider, Row("same", id))).Reverse().ToArray();
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(), adapters);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(Providers.OrderBy(provider => provider), result.Result.Rows.Select(row => row.Selected.Provider));
        Assert.All(result.Result.Rows, row =>
        {
            Assert.Single(row.Copies);
            Assert.False(row.CausalAgreementEstablished);
            Assert.Equal(CosmicFederationIdentityMode.PreserveCopies, row.SelectionReason);
        });
    }

    [Fact]
    public async Task ExplicitPriorityResolvesIdentityBeforeOrderingAndPagingAndRetainsAllCopies()
    {
        var id = Guid.NewGuid();
        var adapters = new[] { new Adapter(Providers[0], Row("A", id)), new Adapter(Providers[1], Row("Z", id)),
            new Adapter(Providers[2], Row("B")) };
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).OrderBy(row => row.Name).Result;
        var options = CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,
            providerPriority: new[] { Providers[1], Providers[0], Providers[2] }).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(source, options), adapters);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(new[] { "B", "Z" }, result.Result.Rows.Select(row => row.Selected.Row.Name));
        Assert.Equal(2, result.Result.ResolvedRows);
        var resolved = result.Result.Rows[1];
        Assert.Equal(Providers[1], resolved.Selected.Provider);
        Assert.Equal(new[] { "A", "Z" }, resolved.Copies.Select(copy => copy.Row.Name));
        Assert.False(resolved.CausalAgreementEstablished);
        Assert.Equal(CosmicFederationIdentityMode.ProviderPriority, resolved.SelectionReason);
        var firstPage = await CosmicFederationExecutor.ExecuteAsync(Plan(source,
            CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,
                providerPriority: options.ProviderPriority, take: 1).Result), adapters);
        Assert.Equal("B", Assert.Single(firstPage.Result.Rows).Selected.Row.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MetadataOrderingPreservesMissingNullAndValueStates(bool descending)
    {
        var missing = Row("missing");
        var explicitNull = Row("null"); explicitNull.MetaData["score"] = null!;
        var value = Row("value"); value.MetaData["score"] = 9007199254740993L;
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        source = descending ? source.OrderByDescending(row => CosmicQueryMetadata.Value<long?>(row, "score")).Result
            : source.OrderBy(row => CosmicQueryMetadata.Value<long?>(row, "score")).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(source), new[]
        { new Adapter(Providers[0], value), new Adapter(Providers[1], explicitNull), new Adapter(Providers[2], missing) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(descending ? new[] { "value", "null", "missing" } : new[] { "missing", "null", "value" },
            result.Result.Rows.Select(row => row.Selected.Row.Name));
    }

    [Fact]
    public async Task LongOrderingRetainsValuesBeyondDoublePrecision()
    {
        var first = Row("first"); first.MetaData["score"] = 9007199254740992L;
        var second = Row("second"); second.MetaData["score"] = 9007199254740993L;
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible)
            .OrderBy(row => CosmicQueryMetadata.Value<long>(row, "score")).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(source), new[]
        { new Adapter(Providers[0], second), new Adapter(Providers[1], first), new Adapter(Providers[2]) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(new[] { "first", "second" }, result.Result.Rows.Select(row => row.Selected.Row.Name));
    }

    [Fact]
    public async Task CanonicalStringOrderUsesUnicodeScalarsAndThenBy()
    {
        var early = Row("\uE000"); early.CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var late = Row("\uE000"); late.CreatedDate = early.CreatedDate.AddDays(1);
        var supplementary = Row("\U00010000"); supplementary.CreatedDate = early.CreatedDate;
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible)
            .OrderBy(row => row.Name).ThenByDescending(row => row.CreatedDate).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(source), new[]
        { new Adapter(Providers[0], early), new Adapter(Providers[1], supplementary), new Adapter(Providers[2], late) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(new[] { late.Id, early.Id, supplementary.Id }, result.Result.Rows.Select(row => row.Selected.Row.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidLateOrPriorityLosingOrderingCellsCannotHideBehindGlobalPage(bool priority)
    {
        var id = Guid.NewGuid();
        var valid = Row("valid", id); valid.MetaData["score"] = 1L;
        var invalid = Row("invalid", priority ? id : Guid.NewGuid()); invalid.MetaData["score"] = "2";
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible)
            .OrderBy(row => CosmicQueryMetadata.Value<long>(row, "score")).Result;
        var options = priority ? CosmicFederationOptions.Create(CosmicFederationIdentityMode.ProviderPriority,
            providerPriority: Providers, take: 1).Result : CosmicFederationOptions.Create(take: 1).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(source, options), new[]
        { new Adapter(Providers[0], valid), new Adapter(Providers[1], invalid), new Adapter(Providers[2]) });
        Assert.Equal("CQL_FEDERATION_RESULT_INVALID", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MergeWorkAndFullProvenanceOutputShareTheOriginalBudget(bool output)
    {
        var options = output ? CosmicFederationOptions.Create(maximumOutputBytes: 1).Result
            : CosmicFederationOptions.Create(maximumWork: 5).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(options: options), new[]
        { new Adapter(Providers[0], Row("A")), new Adapter(Providers[1], Row("B")), new Adapter(Providers[2]) });
        Assert.Equal(output ? "CQL_FEDERATION_OUTPUT_LIMIT" : "CQL_FEDERATION_WORK_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task CancellationAfterInputCaptureCannotPublishMergedRows()
    {
        using var cancellation = new CancellationTokenSource();
        var plan = Plan();
        var input = await CosmicFederationInputCapture.CaptureAsync(plan, Providers.Select(provider => new Adapter(provider)).ToArray(), cancellation.Token);
        Assert.False(input.IsError, input.Message);
        cancellation.Cancel();
        var result = CosmicFederationExecutor.Merge(plan, input.Result);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FloatingMetadataUsesTheDeclaredNativeIeeeDomainRatherThanRawJsonNumberType(bool single)
    {
        var row = Row("numeric JSON");
        // Native Single ordering explicitly normalizes JSON decimal values to
        // binary32. Double ordering similarly normalizes integer JSON values.
        row.MetaData["score"] = single ? (object)1.0000002m : 9007199254740993L;
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        source = single ? source.OrderBy(value => CosmicQueryMetadata.Value<float>(value, "score")).Result
            : source.OrderBy(value => CosmicQueryMetadata.Value<double>(value, "score")).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(source), new[]
        { new Adapter(Providers[0], row), new Adapter(Providers[1]), new Adapter(Providers[2]) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(row.Id, Assert.Single(result.Result.Rows).Selected.Row.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactIeeeNormalizationTiesUseIdentityRatherThanUnroundedJsonOrder(bool single)
    {
        var high = Row("higher raw", Guid.Parse("10000000-0000-0000-0000-000000000000"));
        var low = Row("lower raw", Guid.Parse("20000000-0000-0000-0000-000000000000"));
        high.MetaData["score"] = single ? (object)1.000000059604644775390625m : 9007199254740993L;
        low.MetaData["score"] = single ? (object)1m : 9007199254740992L;
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        source = single ? source.OrderBy(value => CosmicQueryMetadata.Value<float>(value, "score")).Result
            : source.OrderBy(value => CosmicQueryMetadata.Value<double>(value, "score")).Result;
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(source), new[]
        { new Adapter(Providers[0], high), new Adapter(Providers[1], low), new Adapter(Providers[2]) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(new[] { high.Id, low.Id }, result.Result.Rows.Select(item => item.Selected.Row.Id));
    }
}
