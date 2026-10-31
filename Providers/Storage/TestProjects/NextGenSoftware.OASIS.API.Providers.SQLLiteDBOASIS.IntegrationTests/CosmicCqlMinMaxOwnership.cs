using System.Data;
using Microsoft.Data.Sqlite;
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
    [Theory]
    [InlineData("Version")] [InlineData("IsActive")]
    public async Task NativeMinMaxRejectsMalformedNonWinningCanonicalValues(string field)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var good = new Holon { Id = Guid.NewGuid(), Name = "good", IsActive = false };
        var bad = new Holon { Id = Guid.NewGuid(), Name = "bad", IsActive = false };
        foreach (var row in new[] { good, bad }) (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        using (var native = new DataContext(TestConnectionString))
            native.Database.ExecuteSqlRaw(field == "Version" ? "UPDATE Holon SET Version=9223372036854775807 WHERE Id={0}"
                : "UPDATE Holon SET IsActive=2 WHERE Id={0}", bad.Id.ToString());
        var keys = new[] { good.Id, bad.Id };
        var source = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id)).Result;
        if (field == "Version")
            (await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => h.Version).Result))).ErrorCode.Should().Be("CQL_DATA_INVALID");
        else
            (await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => h.IsActive).Result))).ErrorCode.Should().Be("CQL_DATA_INVALID");
    }

    [Fact]
    public async Task NativeMinMaxRejectsRawInvalidUtf8AndRemovesItsOwnedFunction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Id = Guid.NewGuid(), Name = "valid canonical name", MetaData = new() { ["x"] = "valid" } };
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString);
        context.Database.ExecuteSqlRaw("UPDATE Holon SET MetaDataJson={0} WHERE Id={1}", "{\"x\":\"\\ud800\"}", row.Id.ToString());
        await context.Database.OpenConnectionAsync();
        var repository = new HolonRepository(context, () => false); var id = row.Id;
        var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
        var invalid = await repository.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => CosmicQueryMetadata.Value<string>(h, "x")).Result));
        invalid.ErrorCode.Should().Be("CQL_DATA_INVALID"); context.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
        using (var probe = context.Database.GetDbConnection().CreateCommand())
        {
            probe.CommandText = "SELECT cosmic_cql_utf8_valid(X'61')";
            await Assert.ThrowsAsync<SqliteException>(async () => { await probe.ExecuteScalarAsync(); });
        }
        var fresh = await repository.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Max(h => h.Name).Result));
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Should().Be(row.Name);
        await context.Database.CloseConnectionAsync();
    }

    [Fact]
    public async Task NativeMinMaxInputExceedsGraphAndRowPublicationLimits()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var prefix = "owned minmax137 " + Guid.NewGuid().ToString("N");
        var rows = Enumerable.Range(0, 137).Select(index => new Holon
        { Id = Guid.NewGuid(), Name = prefix + index, MetaData = new() { ["stock"] = (long)index } }).ToArray();
        (await provider.SaveHolonsAsync(rows, saveChildren: false)).IsError.Should().BeFalse();
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maxChildDepth: 1, maximumNodes: 1).Result;
        var source = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith(prefix, StringComparison.Ordinal)).WithChildren(graph).Result;
        var max = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Max(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result));
        max.IsError.Should().BeFalse(max.Message + max.Exception); max.Result.Should().Be(136L);
    }

    [Fact]
    public async Task NativeMinMaxCancellationAfterActualScopeReadReleasesOwnership()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph(); (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using var cancellation = new CancellationTokenSource(); var observed = false;
        using var context = new GraphReadContext(TestConnectionString, new DescendantReaderInterceptor(() =>
        { observed = true; cancellation.Cancel(); return Task.CompletedTask; }));
        var repository = new HolonRepository(context, () => false); var id = root.Id;
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result).Min(h => h.Name).Result;
        var plan = MinMaxPlan(provider, query);
        var cancelled = await repository.ExecuteCosmicScalarAsync(plan, cancellation.Token);
        observed.Should().BeTrue(); cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed); context.Database.CurrentTransaction.Should().BeNull();
        var fresh = await repository.ExecuteCosmicScalarAsync(plan);
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Should().Be(root.Name);
    }

    [Fact]
    public async Task NativeMinMaxRetainsSnapshotAcrossScopeAndValueReads()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph(); var oldName = root.Name; var id = root.Id;
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using (var configure = new DataContext(TestConnectionString)) configure.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        var changed = false;
        using var context = new GraphReadContext(TestConnectionString, new DescendantReaderInterceptor(async () =>
        {
            using var writer = new DataContext(TestConnectionString);
            (await writer.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Name={"newer minmax root"} WHERE Id={id.ToString()}")).Should().Be(1);
            changed = true;
        }));
        var repository = new HolonRepository(context, () => false);
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result).Max(h => h.Name).Result;
        var result = await repository.ExecuteCosmicScalarAsync(MinMaxPlan(provider, query));
        changed.Should().BeTrue(); result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Should().Be(oldName);
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(h => h.Id == id.ToString()).Name.Should().Be("newer minmax root");
    }

    [Fact]
    public async Task NativeMinMaxRetainsCallerConnectionAndSerializableTransaction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Id = Guid.NewGuid(), Name = "before caller" };
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString); await context.Database.OpenConnectionAsync();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Name={"caller pending"} WHERE Id={row.Id.ToString()}");
        var id = row.Id; var repository = new HolonRepository(context, () => false);
        var result = await repository.ExecuteCosmicScalarAsync(MinMaxPlan(provider,
            CosmicQuery<Holon>.Create().Where(h => h.Id == id).Min(h => h.Name).Result));
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Should().Be("caller pending");
        context.Database.CurrentTransaction.Should().BeSameAs(transaction); context.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
        await transaction.RollbackAsync(); await context.Database.CloseConnectionAsync();
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(h => h.Id == id.ToString()).Name.Should().Be(row.Name);
    }

    [Fact]
    public async Task NativeMinMaxTraversalErrorsPrecedeAnEmptySourceWindow()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph(); (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse(); var id = root.Id;
        var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name != "absent", CosmicQueryTraversalOptions.Create(2, 1).Result).Skip(1000).Take(1).Result;
        var invalid = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => h.Name).Result));
        invalid.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); invalid.IsLoaded.Should().BeFalse();
        var fresh = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider,
            CosmicQuery<Holon>.Create().Where(h => h.Id == id).Max(h => h.Name).Result));
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception);
    }

    [Fact]
    public async Task NativeMinMaxPreCancellationWinsOverAnEmptyWindow()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var query = CosmicQuery<Holon>.Create().Take(0).Min(h => h.Version).Result; var plan = MinMaxPlan(provider, query);
        var cancelled = await provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
        cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        (await provider.ExecuteCosmicScalarAsync(plan)).ErrorCode.Should().Be("CQL_EMPTY_SEQUENCE");
    }
}
