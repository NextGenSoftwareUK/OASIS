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
    public async Task NativeNumericCancellationAfterActualScopeReadReleasesOwnership()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph(); (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using var cancellation = new CancellationTokenSource(); var observed = false;
        using var context = new GraphReadContext(TestConnectionString, new DescendantReaderInterceptor(() =>
        { observed = true; cancellation.Cancel(); return Task.CompletedTask; }));
        var repository = new HolonRepository(context, () => false); var id = root.Id;
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result).Sum(h => h.Version).Result;
        var plan = NumericPlan(provider, query);
        var cancelled = await repository.ExecuteCosmicScalarAsync(plan, cancellation.Token);
        observed.Should().BeTrue(); cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        context.Database.GetDbConnection().State.Should().Be(ConnectionState.Closed); context.Database.CurrentTransaction.Should().BeNull();
        var fresh = await repository.ExecuteCosmicScalarAsync(plan);
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Should().Be(root.Version);
    }

    [Fact]
    public async Task NativeNumericRetainsSnapshotAcrossScopeAndValueReads()
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
            .WhereAnyDescendant(h => h.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result).Sum(h => h.Version).Result;
        var result = await repository.ExecuteCosmicScalarAsync(NumericPlan(provider, query));
        changed.Should().BeTrue(); result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Should().Be(oldVersion);
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(h => h.Id == id.ToString()).Version.Should().Be(42);
    }

    [Fact]
    public async Task NativeNumericRetainsCallerConnectionAndSerializableTransaction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Id = Guid.NewGuid(), Name = "before caller" };
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString); await context.Database.OpenConnectionAsync();
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Version={42} WHERE Id={row.Id.ToString()}");
        var id = row.Id; var repository = new HolonRepository(context, () => false);
        var result = await repository.ExecuteCosmicScalarAsync(NumericPlan(provider,
            CosmicQuery<Holon>.Create().Where(h => h.Id == id).Sum(h => h.Version).Result));
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Should().Be(42);
        context.Database.CurrentTransaction.Should().BeSameAs(transaction); context.Database.GetDbConnection().State.Should().Be(ConnectionState.Open);
        await transaction.RollbackAsync(); await context.Database.CloseConnectionAsync();
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(h => h.Id == id.ToString()).Version.Should().Be(row.Version);
    }

    [Fact]
    public async Task NativeNumericTraversalErrorsPrecedeAnEmptySourceWindow()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph(); (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse(); var id = root.Id;
        var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name != "absent", CosmicQueryTraversalOptions.Create(2, 1).Result).Skip(1000).Take(1).Result;
        var invalid = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => h.Version).Result));
        invalid.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); invalid.IsLoaded.Should().BeFalse();
        var fresh = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider,
            CosmicQuery<Holon>.Create().Where(h => h.Id == id).Sum(h => h.Version).Result));
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception);
    }

    [Fact]
    public async Task NativeNumericPreCancellationWinsOverAnEmptyWindow()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var query = CosmicQuery<Holon>.Create().Take(0).Sum(h => h.Version).Result; var plan = NumericPlan(provider, query);
        var cancelled = await provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
        cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        (await provider.ExecuteCosmicScalarAsync(plan)).Result.Should().Be(0);
    }
    [Fact]
    public async Task NativeNumericInputExceedsGraphAndRowPublicationLimits()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, Enumerable.Range(0, 137).Select(i => (object)(long)i).ToArray());
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
        var selected = CosmicQuery<Holon>.Create().Where(h => h.Name.StartsWith("owned numeric ", StringComparison.Ordinal)).WithChildren(graph);
        selected.IsError.Should().BeFalse(selected.Message);
        var source = selected.Result;
        var sum = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message + sum.Exception); sum.Result.Should().Be(9316L);
        var average = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        average.IsError.Should().BeFalse(average.Message + average.Exception); average.Result.Should().Be(68m);
    }

    [Fact]
    public async Task NativeNumericVisibilityAppliesBeforeOverflowAndAverageParticipation()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid(); var keys = new List<Guid>();
        foreach (var item in new[] { (actor, false, 7L), (foreign, false, long.MaxValue), (foreign, true, -1L) })
        {
            var row = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = item.Item1, IsPublic = item.Item2, MetaData = new() { ["value"] = item.Item3 } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); keys.Add(row.Id);
        }
        var ids = keys.ToArray(); var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Where(h => ids.Contains(h.Id)).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message + sum.Exception); sum.Result.Should().Be(6L);
        var average = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        average.IsError.Should().BeFalse(average.Message + average.Exception); average.Result.Should().Be(3m);
    }

    [Fact]
    public async Task NativeNumericMissingAndNullAreIgnoredOnlyForNullableInput()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, 7L, null!);
        var missing = new Holon { Id = Guid.NewGuid() }; (await provider.SaveHolonAsync(missing, saveChildren: false)).IsError.Should().BeFalse();
        ids = ids.Append(missing.Id).ToArray(); var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var average = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Average(h => CosmicQueryMetadata.Value<long?>(h, "value")).Result));
        average.IsError.Should().BeFalse(average.Message + average.Exception); average.Result.Should().Be(7m);
        var required = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result));
        required.ErrorCode.Should().Be("CQL_DATA_INVALID"); required.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeNumericCanonicalIntegerBoundsAreCheckedBeforeReduction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Id = Guid.NewGuid() }; (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        using (var context = new DataContext(TestConnectionString)) context.Database.ExecuteSqlRaw("UPDATE Holon SET Version=9223372036854775807 WHERE Id={0}", row.Id.ToString());
        var id = row.Id; var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
        var invalid = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, source.Sum(h => h.Version).Result));
        invalid.ErrorCode.Should().Be("CQL_DATA_INVALID"); invalid.IsLoaded.Should().BeFalse();
    }
}
