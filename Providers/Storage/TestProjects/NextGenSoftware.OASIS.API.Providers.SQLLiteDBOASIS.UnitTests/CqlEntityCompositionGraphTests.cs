using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using System.Linq.Expressions;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

public sealed partial class CqlEntityCompositionOwnerTests
{
    private static HolonManager Manager(SQLLiteDBOASIS provider)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        return new HolonManager(provider, dna, new ProviderManager(null, dna));
    }
    public sealed class EntityDemoRow : Holon
    {
        [CustomOASISProperty] public int Rating { get; set; }
    }

    [TestMethod]
    public async Task TypedV2EntityStagesHydrateTrackAndRebindDeferredValues()
    {
        var settings = "Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper = new SqliteConnection(settings); await keeper.OpenAsync();
        using var provider = new SQLLiteDBOASIS(settings);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        foreach (var rating in new[] { 3, 9, 6 })
        {
            var saved = await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "coffee" + rating,
                MetaData = new() { ["Rating"] = rating } }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
        }
        var source = CosmicQuery<EntityDemoRow>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result);
        var root = CosmicLinq.Create(Manager(provider), provider, source).Result;
        var threshold = 7;
        var query = root.OrderBy(row => row.Name).Take(3).Where(row => row.Rating > threshold)
            .OrderByDescending(row => row.Rating).Take(1);
        var first = await query.ToCosmicListAsync();
        Assert.IsFalse(first.IsError, first.Message + first.Exception);
        Assert.AreEqual(9, first.Result.Single().Rating);
        Assert.IsFalse(HolonChangeTracking.HasChanges(first.Result.Single()));
        first.Result.Single().Rating = 10;
        Assert.IsTrue(HolonChangeTracking.HasChanges(first.Result.Single()));
        threshold = 4;
        var next = await query.ToCosmicListAsync();
        Assert.IsFalse(next.IsError, next.Message + next.Exception);
        Assert.AreEqual(9, next.Result.Single().Rating);
        Assert.AreNotSame(first.Result.Single(), next.Result.Single());
        Assert.AreEqual(2, root.Take(3).Where(row => row.Rating > threshold).ToArray().Length);
    }

    [TestMethod]
    public async Task EntityGraphStagesRespectVisibilityDepthAndSnapshotThroughTypedOwner()
    {
        var settings = "Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper = new SqliteConnection(settings); await keeper.OpenAsync();
        using var provider = new SQLLiteDBOASIS(settings);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var root = new Holon { Id = Guid.NewGuid(), Name = "root", IsPublic = true };
        var visible = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "visible", IsPublic = true };
        visible.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = visible.Id, Name = "deep", IsPublic = true });
        root.Children.Add(visible);
        root.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "hidden", IsPublic = false });
        var saved = await provider.SaveHolonAsync(root);
        Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
        var id = root.Id;
        var policy = CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot,
            maxChildDepth: 1, maximumNodes: 2).Result;
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result)
            .WithReadAccess(policy).Where(row => row.Id == id).WithChildren(graph).Result;
        var query = CosmicLinq.Create(Manager(provider), provider, source).Result.Take(1).Where(row => row.Name == "root");
        var result = await query.ToCosmicListAsync();
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        var row = result.Result.Single();
        Assert.AreEqual("visible", row.Children.Single().Name);
        Assert.AreEqual(0, row.Children.Single().Children.Count());
        Assert.IsFalse(HolonChangeTracking.HasChanges(row));
    }

    [TestMethod]
    public async Task ProtectedEntityTraversalLimitsSurviveBooleanAndEmptyWindowStages()
    {
        var settings = "Data Source=cosmic-entity-owner;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper = new SqliteConnection(settings); await keeper.OpenAsync();
        using var provider = new SQLLiteDBOASIS(settings);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var root = new Holon { Id = Guid.NewGuid(), Name = "root" };
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "child" };
        child.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = child.Id, Name = "grandchild" });
        root.Children.Add(child);
        var saved = await provider.SaveHolonAsync(root); Assert.IsFalse(saved.IsError, saved.Message);
        var id = root.Id;
        var source = CosmicQuery<Holon>.Create().Where(row => row.Id == id).Take(1).Result;
        var limits = CosmicQueryTraversalOptions.Create(3, 1).Result;
        Expression<Func<Holon, bool>>[] predicates = {
            row => row.Name == "root" || CosmicQueryRelations.AnyDescendant(row, node => node.Name == "missing", limits),
            row => row.Name == "missing" && CosmicQueryRelations.AnyDescendant(row, node => node.Name == "missing", limits) };
        foreach (var predicate in predicates)
        {
            var query = source.Compose().Result.Where(predicate).Result.Take(0).Result;
            var failed = await provider.ExecuteCosmicEntityCompositionAsync(Plan(query), 10);
            Assert.IsTrue(failed.IsError); Assert.AreEqual("CQL_QUERY_LIMIT", failed.ErrorCode, failed.Message + failed.Exception);
            Assert.IsFalse(failed.IsLoaded);
        }
        var valid = CosmicQueryTraversalOptions.Create(3, 3).Result;
        var match = source.Compose().Result.Where(row => CosmicQueryRelations.AnyDescendant(row,
            node => node.Name == "grandchild", valid)).Result;
        var found = await provider.ExecuteCosmicEntityCompositionAsync(Plan(match), 10);
        Assert.IsFalse(found.IsError, found.Message + found.Exception); Assert.AreEqual(id, found.Result.Single().Id);

        // A stage runs only on its actual input. An earlier empty window has
        // no roots to traverse; a later empty window cannot hide an error.
        var noInput = source.Compose().Result.Take(0).Result.Where(predicates[0]).Result;
        var absent = await provider.ExecuteCosmicEntityCompositionAsync(Plan(noInput), 10);
        Assert.IsFalse(absent.IsError, absent.Message + absent.Exception); Assert.AreEqual(0, absent.Result.Count);

        // Inject legacy parent-link corruption without a canonical membership
        // manifest. A normal save maintains membership and cannot create this
        // raw-data cycle merely by changing one parent link.
        using (var context = new DataContext(settings))
        {
            var key = id.ToString("D");
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Holon SET ParentHolonId={key}, PersistentValueJson=json_remove(PersistentValueJson,'$.ChildRoleManifestJson') WHERE Id={key}");
        }
        var cyclic = source.Compose().Result.Where(row => row.Name == "root" ||
            CosmicQueryRelations.AnyDescendant(row, node => node.Name == "missing", valid)).Result.Take(0).Result;
        var invalid = await provider.ExecuteCosmicEntityCompositionAsync(Plan(cyclic), 10);
        Assert.AreEqual("CQL_DATA_INVALID", invalid.ErrorCode, invalid.Message + invalid.Exception);
        Assert.IsFalse(invalid.IsLoaded);
    }
}

