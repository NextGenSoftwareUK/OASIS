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
    private static CosmicProjectionPlan<T, TResult> ProjectionPlan<T, TResult>(SQLLiteDBOASIS provider,
        CosmicProjectionQuery<T, TResult> query) where T : IHolon
    {
        var adapter = (ICosmicQueryProjectionStorageProvider)provider;
        var plan = CosmicProjectionPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicProjectionTarget(adapter.QueryProviderType, adapter.QueryCapabilities, adapter.ProjectionCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        return plan.Result;
    }

    [Fact]
    public async Task NativeCqlProjectionSelectsPagedColumnsWithoutHydratingEntities()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var rows = Enumerable.Range(0, 5).Select(index => new Holon
        { Id = Guid.NewGuid(), Name = "projection " + index, MetaData = new() { ["stock"] = 9007199254740993L + index, ["Rating"] = "invalid unselected integer" } }).ToArray();
        (await provider.SaveHolonsAsync(rows, saveChildren: false)).IsError.Should().BeFalse();
        var ids = rows.Select(row => row.Id).ToArray();
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => ids.Contains(row.Id))
            .OrderBy(row => row.Name).Skip(1).Take(2)
            .Select(row => new { row.Name, Stock = CosmicQueryMetadata.Value<long>(row, "stock") }).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query),
            new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Select(row => row.Name).Should().Equal("projection 1", "projection 2");
        result.Result.Providers.Single().Rows.Select(row => row.Stock).Should().Equal(9007199254740994L, 9007199254740995L);
    }

    [Fact]
    public async Task NativeCqlProjectionPreservesMetadataScalarDomainsAndNullableValues()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid(); var code = Guid.NewGuid(); var instant = new DateTime(638500000000000007L, DateTimeKind.Utc);
        (await provider.SaveHolonAsync(new Holon { Id = id, Name = "typed projection", MetaData = new()
        { ["code"] = code, ["instant"] = instant, ["enabled"] = true, ["nothing"] = null, ["stock"] = 9007199254740993L } }, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Where(row => row.Id == id).Select(row => new
        {
            Code = CosmicQueryMetadata.Value<Guid>(row, "code"),
            Instant = CosmicQueryMetadata.Value<DateTime>(row, "instant"),
            Enabled = CosmicQueryMetadata.Value<bool>(row, "enabled"),
            Nothing = CosmicQueryMetadata.Value<long?>(row, "nothing"),
            Missing = CosmicQueryMetadata.Value<long?>(row, "missing"),
            Stock = CosmicQueryMetadata.Value<long>(row, "stock")
        }).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var selected = result.Result.Providers.Single().Rows.Single();
        selected.Code.Should().Be(code); selected.Instant.Should().Be(instant); selected.Instant.Kind.Should().Be(DateTimeKind.Utc);
        selected.Enabled.Should().BeTrue(); selected.Nothing.Should().BeNull(); selected.Missing.Should().BeNull(); selected.Stock.Should().Be(9007199254740993L);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task NativeCqlProjectionRejectsWrongOrMissingRequiredDomains(bool missing)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid();
        var row = new Holon { Id = id, Name = "invalid projection" };
        if (!missing) row.MetaData["stock"] = "7";
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new[] { provider });
        result.IsError.Should().BeTrue(); result.IsLoaded.Should().BeFalse(); result.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeCqlProjectionVisibilityPrecedesPagingAndPayloadBounds()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor = Guid.NewGuid(); var other = Guid.NewGuid();
        var rows = new[] { new Holon { Id = Guid.NewGuid(), Name = "A-private", CreatedByAvatarId = other },
            new Holon { Id = Guid.NewGuid(), Name = "B-owned", CreatedByAvatarId = actor },
            new Holon { Id = Guid.NewGuid(), Name = "C-public", CreatedByAvatarId = other, IsPublic = true } };
        (await provider.SaveHolonsAsync(rows, saveChildren: false)).IsError.Should().BeFalse();
        var ids = rows.Select(row => row.Id).ToArray();
        var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(row => ids.Contains(row.Id)).OrderBy(row => row.Name).Skip(1).Take(1).Select(row => row.Name).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Should().Equal("C-public");
        var limited = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new[] { provider }, maximumPayloadBytes: 2);
        limited.IsError.Should().BeTrue(); limited.Result.Should().BeNull();
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCqlProjectionSnapshotRetainsReadOnlyAndCallerOwnership(bool callerOwned)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Id = Guid.NewGuid(), Name = "readonly projection" };
        (await provider.SaveHolonAsync(root, saveChildren: false)).IsError.Should().BeFalse();
        using var context = new DataContext(TestConnectionString + ";Mode=ReadOnly");
        await context.Database.OpenConnectionAsync();
        using var transaction = callerOwned ? await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var repository = new HolonRepository(context, () => false);
        var id = root.Id;
        var query = CosmicQuery<Holon>.Create().WithChildren(CosmicQueryGraphOptions.Create(
            CosmicQueryGraphConsistency.ProviderSnapshot, maxChildDepth: 1).Result).Where(h => h.Id == id).Select(h => h.Name).Result;
        var result = await repository.ExecuteCosmicProjectionAsync(ProjectionPlan(provider, query));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Single().Values.Single().Should().Be("readonly projection");
        context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Open);
        context.Database.CurrentTransaction.Should().BeSameAs(transaction);
        if (transaction != null) await transaction.RollbackAsync();
    }

    [Fact]
    public async Task NativeCqlProjectionEmptyCancellationAndActualCapabilityDenialDoNotOpenDatabase()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var adapter = (ICosmicQueryProjectionStorageProvider)provider;
        var empty = ProjectionPlan(provider, CosmicQuery<Holon>.Create().Take(0).Select(h => h.Name).Result);
        var result = await adapter.ExecuteCosmicProjectionAsync(empty);
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Should().BeEmpty();
        File.Exists(_databasePath).Should().BeFalse();
        var cancelled = await adapter.ExecuteCosmicProjectionAsync(empty, cancellationToken: new CancellationToken(true));
        cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
        var unsupported = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<decimal>(h, "unsupported")).Result;
        var borrowed = CosmicProjectionPlanner.Plan(unsupported, adapter.QueryProviderType,
            new[] { new CosmicProjectionTarget(adapter.QueryProviderType, adapter.QueryCapabilities,
                new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(decimal) })) }).Result;
        var rejected = await adapter.ExecuteCosmicProjectionAsync(borrowed);
        rejected.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
        File.Exists(_databasePath).Should().BeFalse();
    }

    [Fact]
    public async Task NativeCqlProjectionScopeErrorsCannotBeHiddenBySourceWindowAndReaderIsReusable()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph();
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var id = root.Id;
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name != "absent", CosmicQueryTraversalOptions.Create(2, 1).Result).Take(1).Select(h => h.Name).Result;
        var failed = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new[] { provider });
        failed.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); failed.IsLoaded.Should().BeFalse(); failed.Result.Should().BeNull();
        var fresh = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider,
            CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => h.Name).Result), new[] { provider });
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception);
    }

    [Fact]
    public async Task NativeCqlProjectionCancelsAfterActualScopeReaderAndReleasesReadOwner()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph();
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using var cancellation = new CancellationTokenSource();
        var observed = false;
        using var context = new GraphReadContext(TestConnectionString, new DescendantReaderInterceptor(() =>
        { observed = true; cancellation.Cancel(); return Task.CompletedTask; }));
        var repository = new HolonRepository(context, () => false);
        var id = root.Id;
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result).Select(h => h.Name).Result;
        var plan = ProjectionPlan(provider, query);
        var cancelled = await repository.ExecuteCosmicProjectionAsync(plan, cancellationToken: cancellation.Token);
        observed.Should().BeTrue(); cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.IsLoaded.Should().BeFalse();
        context.Database.GetDbConnection().State.Should().Be(System.Data.ConnectionState.Closed);
        context.Database.CurrentTransaction.Should().BeNull();
        var fresh = await repository.ExecuteCosmicProjectionAsync(plan);
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception);
    }

    [Fact]
    public async Task NativeCqlProjectionKeepsOneSnapshotAcrossScopeValidationAndColumnRead()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = NativeQueryGraph(); var oldName = root.Name;
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        using (var configure = new DataContext(TestConnectionString)) configure.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL");
        var changed = false; var id = root.Id; var persistedId = id.ToString("D");
        using var context = new GraphReadContext(TestConnectionString, new DescendantReaderInterceptor(async () =>
        {
            using var writer = new DataContext(TestConnectionString);
            (await writer.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET Name={"newer projection root"} WHERE Id={persistedId}")).Should().Be(1);
            changed = true;
        }));
        var repository = new HolonRepository(context, () => false);
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .WhereAnyDescendant(h => h.Name == "grandchild", CosmicQueryTraversalOptions.Create(4, 10).Result).Select(h => h.Name).Result;
        var result = await repository.ExecuteCosmicProjectionAsync(ProjectionPlan(provider, query));
        changed.Should().BeTrue("the writer must acknowledge before the projection reader");
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Single().Values.Single().Should().Be(oldName);
        using var control = new DataContext(TestConnectionString);
        control.Holons.AsNoTracking().Single(h => h.Id == persistedId).Name.Should().Be("newer projection root");
    }

    [Fact]
    public async Task NativeCqlProjectionGraphOutputCapDoesNotTruncateSelectedInputAndCountLimitRejects()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var prefix = "projection137 " + Guid.NewGuid().ToString("N");
        (await provider.SaveHolonsAsync(Enumerable.Range(0, 137).Select(index => new Holon
        { Id = Guid.NewGuid(), Name = prefix + index }), saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().WithChildren(CosmicQueryGraphOptions.Create(
            CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result)
            .Where(h => h.Name.StartsWith(prefix, StringComparison.Ordinal)).Select(h => h.Name).Result;
        var plan = ProjectionPlan(provider, query);
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(plan, new[] { provider }, maximumResults: 200);
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Providers.Single().Rows.Should().HaveCount(137);
        var limited = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(plan, new[] { provider }, maximumResults: 100);
        limited.IsError.Should().BeTrue(); limited.Result.Should().BeNull(); limited.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeCqlProjectionNestedMetadataRejectsAmbiguousDuplicatePaths()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid();
        (await provider.SaveHolonAsync(new Holon { Id = id, Name = "nested projection", MetaData = new()
        { ["location"] = new Dictionary<string, object> { ["city"] = "London" } } }, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<string>(h, "location", "city")).Result;
        var plan = ProjectionPlan(provider, query);
        var good = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(plan, new[] { provider });
        good.IsError.Should().BeFalse(good.Message + good.Exception); good.Result.Providers.Single().Rows.Should().Equal("London");
        using (var control = new DataContext(TestConnectionString))
        {
            var persistedId = id.ToString("D"); var ambiguous = "{\"location\":{\"city\":\"London\",\"city\":\"Paris\"}}";
            (await control.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET MetaDataJson={ambiguous} WHERE Id={persistedId}")).Should().Be(1);
        }
        var failed = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(plan, new[] { provider });
        failed.ErrorCode.Should().Be("CQL_DATA_INVALID"); failed.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeCqlProjectionCanonicalColumnsRetainExactClrDomains()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root = new Holon { Id = Guid.NewGuid(), Name = "canonical projection", Description = "typed columns",
            CreatedByAvatarId = Guid.NewGuid(), IsPublic = true };
        var saved = await provider.SaveHolonAsync(root, saveChildren: false);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var id = root.Id;
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => new
        {
            h.Id, h.Name, h.Description, h.ParentHolonId, h.VersionId, h.PreviousVersionId,
            h.CreatedByAvatarId, h.ModifiedByAvatarId, h.Version, h.HolonType, h.IsActive, h.IsPublic
        }).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var selected = result.Result.Providers.Single().Rows.Single();
        selected.Id.Should().Be(id); selected.Name.Should().Be(root.Name); selected.Description.Should().Be(root.Description);
        selected.ParentHolonId.Should().Be(saved.Result.ParentHolonId); selected.VersionId.Should().Be(saved.Result.VersionId);
        selected.PreviousVersionId.Should().Be(saved.Result.PreviousVersionId); selected.CreatedByAvatarId.Should().Be(root.CreatedByAvatarId);
        selected.ModifiedByAvatarId.Should().Be(saved.Result.ModifiedByAvatarId); selected.Version.Should().Be(saved.Result.Version);
        selected.HolonType.Should().Be(saved.Result.HolonType); selected.IsActive.Should().Be(saved.Result.IsActive); selected.IsPublic.Should().BeTrue();
    }

    [Theory]
    [InlineData("Version", "9999999999999")] [InlineData("IsPublic", "2")]
    public async Task NativeCqlProjectionRejectsInvalidStoredCanonicalDomain(string column, string invalid)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid();
        (await provider.SaveHolonAsync(new Holon { Id = id, Name = "invalid native canonical" }, saveChildren: false)).IsError.Should().BeFalse();
        using (var control = new DataContext(TestConnectionString))
        {
            var persistedId = id.ToString("D");
            // Column names are test constants, never public CQL input.
            var sql = column == "Version" ? "UPDATE Holon SET Version={0} WHERE Id={1}" : "UPDATE Holon SET IsPublic={0} WHERE Id={1}";
            (await control.Database.ExecuteSqlRawAsync(sql, long.Parse(invalid, System.Globalization.CultureInfo.InvariantCulture), persistedId)).Should().Be(1);
        }
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => new { h.Version, h.IsPublic }).Result;
        var failed = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new[] { provider });
        failed.ErrorCode.Should().Be("CQL_DATA_INVALID"); failed.IsLoaded.Should().BeFalse(); failed.Result.Should().BeNull();
    }

}
