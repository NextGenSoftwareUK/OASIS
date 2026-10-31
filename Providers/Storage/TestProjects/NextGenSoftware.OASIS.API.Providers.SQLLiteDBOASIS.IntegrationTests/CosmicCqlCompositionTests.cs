using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicProjectionCompositionPlan<Holon, TResult> CompositionPlan<TResult>(SQLLiteDBOASIS provider,
        CosmicProjectionQuery<Holon, TResult> query, CosmicProjectionCompositionOptions? options = null)
    {
        var native = (ICosmicQueryProjectionCompositionStorageProvider)provider;
        var plan = CosmicProjectionCompositionPlanner.Plan(query, native.QueryProviderType,
            new[] { new CosmicProjectionCompositionTarget(native.QueryProviderType, native.QueryCapabilities, native.ProjectionCapabilities, native.CompositionCapabilities) }, options);
        plan.IsError.Should().BeFalse(plan.Message); return plan.Result;
    }

    [Fact]
    public async Task NativeCompositionCancellationAfterActualSqlEntryReleasesOwnerAndAllowsReuse()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Name = "coffee" }, saveChildren: false)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString); await context.Database.OpenConnectionAsync();
        var connection = (SqliteConnection)context.Database.GetDbConnection();
        var columns = new List<string>();
        using (var schema = connection.CreateCommand())
        {
            schema.CommandText = "PRAGMA table_info(Holon)";
            using var reader = await schema.ExecuteReaderAsync();
            while (await reader.ReadAsync()) columns.Add(reader.GetString(1));
        }
        using var cancellation = new CancellationTokenSource(); var entered = 0;
        connection.CreateFunction<string, string>("composition_observe", name =>
        { if (Interlocked.Increment(ref entered) == 1) cancellation.Cancel(); return name; }, isDeterministic: false);
        using (var view = connection.CreateCommand())
        {
            string Quote(string name) => "\"" + name.Replace("\"", "\"\"") + "\"";
            view.CommandText = "ALTER TABLE Holon RENAME TO CompositionInput; CREATE VIEW Holon AS SELECT "
                + string.Join(",", columns.Select(name => name == "Name" ? "composition_observe(\"Name\") AS \"Name\"" : Quote(name)))
                + " FROM CompositionInput";
            await view.ExecuteNonQueryAsync();
        }
        var repository = new HolonRepository(context, () => false);
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result; var plan = CompositionPlan(provider, query);
        var limits = new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000);
        var cancelled = await repository.ExecuteCosmicProjectionCompositionAsync(plan, limits, cancellation.Token);
        entered.Should().BeGreaterThan(0); cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        context.Database.CurrentTransaction.Should().BeNull();
        var fresh = await repository.ExecuteCosmicProjectionCompositionAsync(plan, limits);
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception);
        CosmicProjectionCompositionResults.Materialize(query, fresh.Result).Result.Should().Equal("coffee");
    }

    [Fact]
    public async Task NativeCompositionNeverHydratesSourceEntities()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Name = "coffee" }, saveChildren: false)).IsError.Should().BeFalse();
        var native = (ICosmicQueryProjectionCompositionStorageProvider)provider;
        var query = CosmicQuery<CompositionNeverConstructedHolon>.Create().Select(h => h.Name).Take(1).Result;
        var plan = CosmicProjectionCompositionPlanner.Plan(query, native.QueryProviderType,
            new[] { new CosmicProjectionCompositionTarget(native.QueryProviderType, native.QueryCapabilities, native.ProjectionCapabilities, native.CompositionCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(plan.Result, new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Providers.Single().Rows.Should().Equal("coffee");
    }
    public sealed class CompositionNeverConstructedHolon : Holon
    { public CompositionNeverConstructedHolon() => throw new InvalidOperationException("Native projection must not construct source entities."); }

    [Fact]
    public async Task NativeCompositionRunsOnReadOnlyConnection()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Name = "coffee" }, saveChildren: false)).IsError.Should().BeFalse();
        var readOnly = new SqliteConnectionStringBuilder(TestConnectionString) { Mode = SqliteOpenMode.ReadOnly };
        using var context = new DataContext(readOnly.ToString());
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result;
        var result = await new HolonRepository(context, () => false).ExecuteCosmicProjectionCompositionAsync(CompositionPlan(provider, query),
            new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        CosmicProjectionCompositionResults.Materialize(query, result.Result).Result.Should().Equal("coffee");
    }

    [Fact]
    public async Task NativeCompositionPreservesSourceAndSelectedWindowOrder()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonsAsync(new[] { new Holon { Name = "C" }, new Holon { Name = "A" }, new Holon { Name = "B" } }, saveChildren: false)).IsError.Should().BeFalse();
        var selected = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Take(2).Select(h => new { Label = h.Name })
            .Take(1).Where(row => row.Label == "B").Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(CompositionPlan(provider, selected), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Providers.Single().Rows.Should().BeEmpty();
        var reordered = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Take(2).Select(h => new { Label = h.Name })
            .Where(row => row.Label == "B").Take(1).Result;
        var fresh = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(CompositionPlan(provider, reordered), new[] { provider });
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Providers.Single().Rows.Single().Label.Should().Be("B");
    }

    [Fact]
    public async Task NativeCompositionRetainsPreviousSortRankForEqualKeys()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var seed = new[] { new Holon { Name = "C", Description = "same" }, new Holon { Name = "A", Description = "same" }, new Holon { Name = "B", Description = "same" } };
        (await provider.SaveHolonsAsync(seed, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Select(h => new { h.Name, h.Description }).OrderBy(row => row.Name).OrderByDescending(row => row.Description).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(CompositionPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Select(row => row.Name).Should().Equal("A", "B", "C");
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)] [InlineData(14)]
    public Task NativeCompositionPreservesExactSelectedDomains(int type) => type switch
    {
        0 => CompositionDomain(9007199254740993L), 1 => CompositionDomain(ulong.MaxValue),
        2 => CompositionDomain(1.0000000000000000000000000001m), 3 => CompositionDomain(1d + 2.220446049250313e-16d),
        4 => CompositionDomain(1f + 1.1920929e-7f), 5 => CompositionDomain(Guid.Parse("abcdef01-2345-6789-abcd-ef0123456789")),
        6 => CompositionDomain(new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc).AddTicks(7)),
        7 => CompositionDomain("Caf\u00e9 \U0001f600"), 8 => CompositionDomain(true), 9 => CompositionDomain<int?>(null),
        10 => CompositionDomain<long?>(7L), 11 => CompositionDomain(HolonType.Quest), 12 => CompositionDomain(-7),
        13 => CompositionDomain<decimal?>(null), _ => CompositionDomain<double?>(null)
    };
    private async Task CompositionDomain<T>(T value)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var seed = new Holon { Name = "value", MetaData = new() { ["value"] = value! } };
        (await provider.SaveHolonAsync(seed, saveChildren: false)).IsError.Should().BeFalse();
        var id = seed.Id;
        var intent = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<T>(h, "value"))
            .OrderBy(result => result).Take(1);
        intent.IsError.Should().BeFalse(intent.Message);
        var query = intent.Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(CompositionPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Providers.Single().Rows.Single().Should().Be(value);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCompositionEnforcesVisibilityBeforeSourceAndResultPages(bool visitor)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor = Guid.NewGuid(); var bob = Guid.NewGuid();
        var hidden = new Holon { Name = "A-private", CreatedByAvatarId = bob, MetaData = new() { ["SHARED_WITH"] = actor.ToString("D") } };
        (await provider.SaveHolonsAsync(new[] { hidden, new Holon { Name = "B-owned", CreatedByAvatarId = actor },
            new Holon { Name = "C-public", CreatedByAvatarId = bob, IsPublic = true } }, saveChildren: false)).IsError.Should().BeFalse();
        var access = CosmicQueryReadAccess.OwnerOrPublic(visitor ? Guid.NewGuid() : actor).Result;
        var query = CosmicQuery<Holon>.Create().WithReadAccess(access).OrderBy(h => h.Name).Take(1).Select(h => h.Name).Where(name => name != "A-private").Take(1).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(CompositionPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Should().Equal(visitor ? "C-public" : "B-owned");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCompositionTakeZeroCannotHideInvalidRequiredSource(bool missing)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var seed = new Holon { Name = "bad" }; if (!missing) seed.MetaData["value"] = "invalid";
        (await provider.SaveHolonAsync(seed, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<long>(h, "value")).Take(0).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(CompositionPlan(provider, query), new[] { provider });
        result.ErrorCode.Should().Be("CQL_DATA_INVALID"); result.Result.Should().BeNull();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task NativeCompositionRejectsAllSixExhaustedBudgetsAndReleasesOwner(int index)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Name = "coffee" }, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result; var plan = CompositionPlan(provider, query);
        var work = new[] { 10, 10000, 100, 10000, 10, 10000 }; work[index] = 0;
        var result = await provider.ExecuteCosmicProjectionCompositionAsync(plan, new CosmicProjectionCompositionExecutionLimits(work[0], work[1], work[2], work[3], work[4], work[5]));
        result.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); result.IsLoaded.Should().BeFalse();
        var fresh = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(plan, new[] { provider });
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Providers.Single().Rows.Should().Equal("coffee");
    }

    [Fact]
    public async Task NativeCompositionCancellationDoesNotPoisonTheReadOwner()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result; var plan = CompositionPlan(provider, query);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var failed = await provider.ExecuteCosmicProjectionCompositionAsync(plan, new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000), cancellation.Token);
        failed.ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
        var fresh = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(plan, new[] { provider });
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Providers.Single().Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task NativeCompositionReusesCallerSnapshotWithoutCommittingIt()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var seed = new Holon { Name = "coffee" }; (await provider.SaveHolonAsync(seed, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Where(name => name == "coffee").Take(1).Result;
        var plan = CompositionPlan(provider, query);
        using (var context = new DataContext(TestConnectionString))
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL"); await context.Database.OpenConnectionAsync();
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            using var snapshot = connection.BeginTransaction(System.Data.IsolationLevel.Serializable, deferred: true);
            using var caller = context.Database.UseTransaction(snapshot);
            (await context.Database.SqlQueryRaw<long>("SELECT count(*) AS Value FROM Holon").SingleAsync()).Should().Be(1);
            using (var writer = new DataContext(TestConnectionString))
                (await writer.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Name={"tea"} WHERE Id={seed.Id.ToString("D")}")).Should().Be(1);
            var native = await new HolonRepository(context, () => false).ExecuteCosmicProjectionCompositionAsync(plan,
                new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000));
            native.IsError.Should().BeFalse(native.Message + native.Exception);
            var selected = CosmicProjectionCompositionResults.Materialize(query, native.Result);
            selected.IsError.Should().BeFalse(selected.Message + selected.Exception); selected.Result.Should().Equal("coffee");
            context.Database.CurrentTransaction.Should().NotBeNull();
        }
        var fresh = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(plan, new[] { provider });
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Providers.Single().Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task NativeCompositionReusesProtectedChildPredicateOwner()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Name = "parent" }; root.Children.Add(new Holon { Name = "child" });
        (await provider.SaveHolonAsync(root, saveChildren: true)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().WhereAnyChild(child => child.Name == "child").Select(h => h.Name).Take(1).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionCompositionQueryAsync(CompositionPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Providers.Single().Rows.Should().Equal("parent");
    }
}
