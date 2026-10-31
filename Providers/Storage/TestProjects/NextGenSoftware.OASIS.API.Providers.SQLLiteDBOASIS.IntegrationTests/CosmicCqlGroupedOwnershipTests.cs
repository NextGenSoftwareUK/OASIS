using System.Data;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeGroupingCancellationAfterActualScopeReadReleasesOwnership()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph(); (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using var cancellation = new CancellationTokenSource(); var observed = false;
        using var context = new GraphReadContext(TestConnectionString, new DescendantReaderInterceptor(() =>
        { observed = true; cancellation.Cancel(); return Task.CompletedTask; }));
        var repository = new HolonRepository(context, () => false); var id = root.Id;
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result).GroupBy(h => h.Name).Select(g => g.Sum(h => h.Version)).Result;
        var plan = GroupPlan(provider, query);
        var cancelled = await repository.ExecuteCosmicGroupedAsync(plan, cancellationToken: cancellation.Token);
        observed.Should().BeTrue(); cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed); context.Database.CurrentTransaction.Should().BeNull();
        var fresh = await repository.ExecuteCosmicGroupedAsync(plan);
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Single().Values.Single().Should().Be(root.Version);
    }

    [Fact]
    public async Task NativeGroupingRetainsSnapshotAcrossScopeAndValueReads()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph(); var oldVersion = root.Version; var id = root.Id;
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using (var configure = new DataContext(TestConnectionString)) configure.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        var changed = false;
        using var context = new GraphReadContext(TestConnectionString, new DescendantReaderInterceptor(async () =>
        {
            using var writer = new DataContext(TestConnectionString);
            (await writer.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Version={42} WHERE Id={id.ToString()}")).Should().Be(1);
            changed = true;
        }));
        var repository = new HolonRepository(context, () => false);
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result).GroupBy(h => h.Name).Select(g => g.Sum(h => h.Version)).Result;
        var result = await repository.ExecuteCosmicGroupedAsync(GroupPlan(provider, query));
        changed.Should().BeTrue(); result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Single().Values.Single().Should().Be(oldVersion);
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(h => h.Id == id.ToString()).Version.Should().Be(42);
    }

    [Fact]
    public async Task NativeGroupingRetainsCallerConnectionAndSerializableTransaction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Id = Guid.NewGuid(), Name = "before caller" };
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString); await context.Database.OpenConnectionAsync();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Version={42} WHERE Id={row.Id.ToString()}");
        var id = row.Id; var repository = new HolonRepository(context, () => false);
        var result = await repository.ExecuteCosmicGroupedAsync(GroupPlan(provider,
            CosmicQuery<Holon>.Create().Where(h => h.Id == id).GroupBy(h => h.Name).Select(g => g.Sum(h => h.Version)).Result));
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Single().Values.Single().Should().Be(42);
        context.Database.CurrentTransaction.Should().BeSameAs(transaction); context.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
        await transaction.RollbackAsync(); await context.Database.CloseConnectionAsync();
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(h => h.Id == id.ToString()).Version.Should().Be(row.Version);
    }

}
