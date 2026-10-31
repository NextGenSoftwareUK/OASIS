using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TypedCqlCrossClientConditionalWritesRejectStaleEntitiesAndRecoverOnFreshQuery(bool synchronous)
    {
        using var firstProvider = new SQLLiteDBOASIS(TestConnectionString);
        using var secondProvider = new SQLLiteDBOASIS(TestConnectionString);
        (await firstProvider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await secondProvider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), Name = "cql-source", MetaData = new() { ["Rating"] = 7 } };
        (await firstProvider.SaveHolonAsync(source, saveChildren: false)).IsError.Should().BeFalse();
        var firstManager = CosmicManager(firstProvider, true); var secondManager = CosmicManager(secondProvider, true);
        ICosmicQueryStorageProvider firstAdapter = firstProvider, secondAdapter = secondProvider;
        var firstRead = await firstManager.ExecuteQueryAsync(NativeTypedPlan(firstAdapter), new[] { firstAdapter });
        var secondRead = await secondManager.ExecuteQueryAsync(NativeTypedPlan(secondAdapter), new[] { secondAdapter });
        firstRead.IsError.Should().BeFalse(firstRead.Message); secondRead.IsError.Should().BeFalse(secondRead.Message);
        var winner = firstRead.Result.Providers.Single().Rows.Single(); var stale = secondRead.Result.Providers.Single().Rows.Single();
        var originalVersion = winner.Version;
        var originalRevision = CosmicConcurrency.GetProviderRevisionId(winner, ProviderType.SQLLiteDBOASIS);
        originalRevision.Should().NotBeNull(); originalRevision.Should().NotBe(Guid.Empty);
        CosmicConcurrency.GetProviderRevisionId(stale, ProviderType.SQLLiteDBOASIS).Should().Be(originalRevision);
        winner.Should().NotBeSameAs(stale); winner.Name = "cql-winner"; winner.Rating = 8;
        var saved = synchronous ? firstManager.SaveHolon(winner, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false,
            providerType: ProviderType.SQLLiteDBOASIS) : await firstManager.SaveHolonAsync(winner, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception); saved.IsSaved.Should().BeTrue();
        winner.Version.Should().Be(originalVersion + 1); HolonChangeTracking.HasChanges(winner).Should().BeFalse();
        var winningRevision = CosmicConcurrency.GetProviderRevisionId(winner, ProviderType.SQLLiteDBOASIS);
        winningRevision.Should().NotBeNull();
        winningRevision.GetValueOrDefault().Should().NotBe(originalRevision.GetValueOrDefault());
        stale.Name = "cql-stale"; stale.Rating = 9; var rejectedBefore = StoredRecordJson.Serialize(stale);
        var rejected = synchronous ? secondManager.SaveHolon(stale, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false,
            providerType: ProviderType.SQLLiteDBOASIS) : await secondManager.SaveHolonAsync(stale, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()),
            saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        rejected.IsError.Should().BeTrue(); rejected.ErrorCode.Should().Be(CosmicConcurrencyException.Code);
        rejected.IsSaved.Should().BeFalse(); StoredRecordJson.Serialize(stale).Should().Be(rejectedBefore);
        HolonChangeTracking.HasChanges(stale).Should().BeTrue();
        var persisted = (await secondProvider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        persisted.Name.Should().Be("cql-winner"); persisted.MetaData["Rating"].ToString().Should().Be("8");
        persisted.Version.Should().Be(winner.Version); CosmicConcurrency.GetProviderRevisionId(persisted, ProviderType.SQLLiteDBOASIS).Should().Be(winningRevision);
        var refreshed = await secondManager.ExecuteQueryAsync(NativeTypedPlan(secondAdapter), new[] { secondAdapter });
        refreshed.IsError.Should().BeFalse(refreshed.Message); var fresh = refreshed.Result.Providers.Single().Rows.Single(); fresh.Rating = 10;
        var recovered = await secondManager.SaveHolonAsync(fresh, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        recovered.IsError.Should().BeFalse(recovered.Message + recovered.Exception); fresh.Version.Should().Be(winner.Version + 1);
        HolonChangeTracking.HasChanges(fresh).Should().BeFalse();
    }

    [Fact]
    public async Task TypedCqlLeafEditPreservesUnselectedChildLinksAndChildRevision()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse(); var manager = CosmicManager(provider, true);
        var child = new Holon { Name = "unselected-child" }; var root = new Holon { Name = "cql-parent", Children = new List<IHolon> { child } };
        var created = await manager.SaveHolonAsync(root, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), providerType: ProviderType.SQLLiteDBOASIS);
        created.IsError.Should().BeFalse(created.Message + created.Exception);
        var before = (await provider.LoadHolonAsync(child.Id, loadChildren: false)).Result;
        var rootId = root.Id; ICosmicQueryStorageProvider adapter = provider;
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == rootId).Result;
        var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType, new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var read = await manager.ExecuteQueryAsync(plan.Result, new[] { adapter }); read.IsError.Should().BeFalse(read.Message + read.Exception);
        var leaf = read.Result.Providers.Single().Rows.Single(); leaf.Children.Should().BeEmpty(); leaf.Name = "cql-parent-edited";
        var saved = await manager.SaveHolonAsync(leaf, Guid.NewGuid(), new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
        var parent = await provider.LoadHolonAsync(rootId, loadChildren: true); parent.IsError.Should().BeFalse(parent.Message);
        parent.Result.Children.Single().Id.Should().Be(child.Id); parent.Result.Name.Should().Be("cql-parent-edited");
        var after = (await provider.LoadHolonAsync(child.Id, loadChildren: false)).Result;
        after.Name.Should().Be(before.Name); after.Version.Should().Be(before.Version); after.VersionId.Should().Be(before.VersionId);
        CosmicConcurrency.GetProviderRevisionId(after, ProviderType.SQLLiteDBOASIS).Should().Be(CosmicConcurrency.GetProviderRevisionId(before, ProviderType.SQLLiteDBOASIS));
    }
}
