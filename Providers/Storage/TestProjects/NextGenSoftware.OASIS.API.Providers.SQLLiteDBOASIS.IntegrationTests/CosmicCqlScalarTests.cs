using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicScalarPlan<T, TResult> ScalarPlan<T, TResult>(SQLLiteDBOASIS provider,
        CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        var adapter = (ICosmicQueryScalarStorageProvider)provider;
        var plan = CosmicScalarPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, adapter.ScalarCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData(0, -1, 5)]
    [InlineData(1, 2, 2)]
    [InlineData(3, 100, 2)]
    [InlineData(9, 2, 0)]
    [InlineData(0, 0, 0)]
    public async Task NativeCqlScalarCountsApplyTheComposedWindowWithoutEntityMaterialization(int skip, int take, int expected)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var rows = Enumerable.Range(0, 5).Select(index => new Holon
        {
            Id = Guid.NewGuid(), Name = "owned scalar " + index,
            MetaData = new() { ["Rating"] = "not an integer" }
        }).ToArray();
        foreach (var row in rows)
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();

        var ids = rows.Select(row => row.Id).ToArray();
        var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => ids.Contains(row.Id))
            .OrderByDescending(row => row.Name).Skip(skip).Result;
        if (take >= 0) source = source.Take(take).Result;

        var count = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider, source.Count().Result));
        count.IsError.Should().BeFalse(count.Message + count.Exception);
        count.IsLoaded.Should().BeTrue(); count.Result.Should().Be(expected);

        var wide = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider, source.LongCount().Result));
        wide.IsError.Should().BeFalse(wide.Message + wide.Exception); wide.Result.Should().Be(expected);

        var any = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider, source.Any().Result));
        any.IsError.Should().BeFalse(any.Message + any.Exception); any.Result.Should().Be(expected != 0);

        // Manager execution remains generic and detached: the deliberately
        // throwing entity constructor and invalid unselected metadata are irrelevant.
        var managed = await CosmicManager(provider, true).ExecuteScalarQueryAsync(ScalarPlan(provider, source.LongCount().Result),
            new ICosmicQueryStorageProvider[] { provider });
        managed.IsError.Should().BeFalse(managed.Message + managed.Exception);
        managed.Result.Providers.Single().Value.Should().Be(expected);
    }

    public sealed class ScalarMustNotConstruct : Holon
    {
        public ScalarMustNotConstruct() => throw new InvalidOperationException("A scalar must not construct a holon.");
        [NextGenSoftware.OASIS.API.Core.CustomAttrbiutes.CustomOASISProperty]
        public int Rating { get; set; }
    }

    [Fact]
    public async Task NativeCqlScalarCountIsNotTruncatedByRowOrGraphPublicationLimits()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var prefix = "owned scalar larger " + Guid.NewGuid().ToString("N");
        var rows = Enumerable.Range(0, 137).Select(index => new Holon
        {
            Id = Guid.NewGuid(), Name = prefix + " " + index
        }).ToArray();
        (await provider.SaveHolonsAsync(rows, saveChildren: false)).IsError.Should().BeFalse();

        // Graph output limits do not truncate aggregate input, but the requested
        // provider snapshot must still be retained by the native count owner.
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot,
            maxChildDepth: 1, maximumNodes: 1).Result;
        var source = CosmicQuery<Holon>.Create().WithChildren(graph)
            .Where(row => row.Name.StartsWith(prefix, StringComparison.Ordinal)).Result;
        var result = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider, source.LongCount().Result));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Should().Be(137);
    }

    [Fact]
    public async Task NativeCqlScalarAccessIsAppliedBeforeCountAndExistence()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor = Guid.NewGuid(); var other = Guid.NewGuid();
        var rows = new[]
        {
            new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = actor },
            new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = other, IsPublic = true },
            new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = other, IsPublic = false }
        };
        foreach (var row in rows)
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        var ids = rows.Select(row => row.Id).ToArray();
        var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(row => ids.Contains(row.Id)).Result;
        var count = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider, source.Count().Result));
        count.IsError.Should().BeFalse(count.Message + count.Exception); count.Result.Should().Be(2);
        var hidden = rows[2].Id;
        var any = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider, source.Where(row => row.Id == hidden).Any().Result));
        any.IsError.Should().BeFalse(any.Message + any.Exception); any.Result.Should().BeFalse();
    }

    [Fact]
    public async Task NativeCqlScalarEmptyWindowAndCancellationDoNotOpenAnUncreatedDatabase()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var empty = ScalarPlan(provider, CosmicQuery<Holon>.Create().Take(0).LongCount().Result);
        var count = await provider.ExecuteCosmicScalarAsync(empty);
        count.IsError.Should().BeFalse(count.Message + count.Exception); count.Result.Should().Be(0);
        File.Exists(_databasePath).Should().BeFalse();

        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await provider.ExecuteCosmicScalarAsync(empty, cancellation.Token);
        cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        File.Exists(_databasePath).Should().BeFalse();
    }

    [Fact]
    public async Task NativeCqlScalarAnyDoesNotHideADescendantBudgetErrorAndCanReadAgain()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph();
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var id = root.Id;
        var source = CosmicQuery<Holon>.Create().Where(row => row.Id == id)
            .WhereAnyDescendant(child => child.Name != "absent", CosmicQueryTraversalOptions.Create(2, 1).Result).Take(1).Result;
        var failed = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider, source.Any().Result));
        failed.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); failed.IsLoaded.Should().BeFalse();
        var fresh = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            CosmicQuery<Holon>.Create().Where(row => row.Id == id).Count().Result));
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Should().Be(1);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCqlScalarSnapshotSupportsReadOnlyStorageAndRetainsCallerOwnership(bool callerOwned)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Id = Guid.NewGuid(), Name = "owned scalar snapshot" };
        (await provider.SaveHolonAsync(root, saveChildren: false)).IsError.Should().BeFalse();

        var connectionString = callerOwned ? TestConnectionString : TestConnectionString + ";Mode=ReadOnly";
        using var context = new DataContext(connectionString);
        await context.Database.OpenConnectionAsync();
        using var transaction = callerOwned
            ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var repository = new HolonRepository(context, () => false);
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maxChildDepth: 1).Result;
        var id = root.Id;
        var query = CosmicQuery<Holon>.Create().WithChildren(graph).Where(row => row.Id == id).LongCount().Result;
        var count = await repository.ExecuteCosmicScalarAsync(ScalarPlan(provider, query));
        count.IsError.Should().BeFalse(count.Message + count.Exception); count.Result.Should().Be(1);
        context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Open);
        context.Database.CurrentTransaction.Should().BeSameAs(transaction);
        if (transaction != null) await transaction.RollbackAsync();
    }

    [Fact]
    public async Task NativeCqlScalarCancellationAfterActualReaderDoesNotPublishOrRetainItsReadOwner()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph();
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using var cancellation = new CancellationTokenSource();
        var observed = false;
        using var context = new GraphReadContext(TestConnectionString, new DescendantReaderInterceptor(() =>
        {
            observed = true;
            cancellation.Cancel();
            return Task.CompletedTask;
        }));
        var repository = new HolonRepository(context, () => false);
        var id = root.Id;
        var source = CosmicQuery<Holon>.Create().Where(row => row.Id == id)
            .WhereAnyDescendant(child => child.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result);
        var plan = ScalarPlan(provider, source.LongCount().Result);
        var cancelled = await repository.ExecuteCosmicScalarAsync(plan, cancellation.Token);
        observed.Should().BeTrue();
        cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        cancelled.Result.Should().Be(0);
        context.Database.CurrentTransaction.Should().BeNull();
        context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);

        var fresh = await repository.ExecuteCosmicScalarAsync(plan);
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Should().Be(1);
    }
}
