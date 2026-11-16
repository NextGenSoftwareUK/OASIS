extern alias ShippingWeb4;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using CqlCompiler = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlV1Compiler;
using CqlJoinRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Request;
using CqlJoinColumn = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Column;
using CqlJoinFilter = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Filter;
using CqlJoinOperator = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Operator;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyChildSource(ICosmicQueryJoinStorageProvider provider, Guid actor, Guid parentId, Guid childId,
        List<object> checks)
    {
        var scope = CosmicQueryScope.Named(provider.QueryProviderType).Result;
        var source = CosmicQuery<Holon>.Create(scope).WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var parents = source.Where(row => row.Id == parentId).Take(1).Result;
        var options = CosmicJoinOptions.Create(maximumLeftRows: 1, maximumRightRows: 1).Result;
        var joined = parents.JoinChildren(source, options).Select((parent, child) => child.Id);
        Assert.IsFalse(joined.IsError, joined.Message);
        var plan = CosmicJoinPlanner.Plan(joined.Result, provider.QueryProviderType,
            new[] { new CosmicJoinTarget(provider.QueryProviderType, provider.QueryCapabilities, provider.JoinCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var rows = await NativeManager(provider).ExecuteJoinQueryAsync(plan.Result, new[] { provider });
        Assert.IsFalse(rows.IsError, rows.Message + rows.Exception);
        Assert.AreEqual(childId, rows.Result.Providers.Single().Rows.Single().Value);
        var linq = CosmicLinq.Create(NativeManager(provider), provider, parents, maximumResults: 1).Result;
        var flattened = await linq.SelectMany(parent => parent.Children,
            (parent, child) => new { ParentId = parent.Id, ChildId = child.Id })
            .Select(pair => pair.ChildId).ToCosmicListAsync();
        Assert.IsFalse(flattened.IsError, flattened.Message + flattened.Exception);
        Assert.AreEqual(childId, flattened.Result.Single());
        var collectionProjection = linq.SelectMany(parent => parent.Children.Select(child => child.Id))
            .Select(id => new { Identity = id });
        var selectedChildren = await collectionProjection.ToCosmicListAsync();
        Assert.IsFalse(selectedChildren.IsError, selectedChildren.Message + selectedChildren.Exception);
        Assert.AreEqual(childId, selectedChildren.Result.Single().Identity);
        var childName = "visible match";
        var filtered = linq.SelectMany(parent => parent.Children.Where(child => child.Name == childName),
            (parent, child) => child.Id);
        CollectionAssert.AreEqual(new[] { childId }, (await filtered.ToCosmicListAsync()).Result.ToArray());
        childName = "absent";
        var noChildren = await filtered.ToCosmicListAsync();
        Assert.IsFalse(noChildren.IsError, noChildren.Message + noChildren.Exception);
        Assert.AreEqual(0, noChildren.Result.Count);
        var projectedFilter = linq.SelectMany(parent => parent.Children.Select(child => new { child.Id, child.Name }))
            .Where(child => child.Name == "visible match").Select(child => child.Id);
        var filteredProjection = await projectedFilter.ToCosmicListAsync();
        Assert.IsFalse(filteredProjection.IsError, filteredProjection.Message + filteredProjection.Exception);
        Assert.AreEqual(childId, filteredProjection.Result.Single());
        var request = new CqlJoinRequest
        {
            Kind = "Child",
            OutputOrderBy = "ChildId", OutputDescending = true,
            Left = new() { Take = 1, Filters = new[] {
                new CqlJoinFilter { Field = "Id", Operator = CqlJoinOperator.Equal,
                    Value = JsonSerializer.SerializeToElement(parentId) } } },
            MaximumLeftRows = 1, MaximumRightRows = 1,
            Columns = new[] { new CqlJoinColumn { Alias = "ChildId", Source = "Right", Selection = new() { Field = "Id" } } }
        };
        var wire = CqlCompiler.CompileJoin(request, actor, provider.QueryProviderType, CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        Assert.IsTrue(wire.Join.RestrictsRightToLeftParents);
        Assert.AreEqual(0, wire.OutputOrderColumn);
        Assert.IsTrue(wire.OutputOrderDescending);
        var wirePlan = CosmicJoinPlanner.Plan(wire, provider.QueryProviderType,
            new[] { new CosmicJoinTarget(provider.QueryProviderType, provider.QueryCapabilities, provider.JoinCapabilities) });
        Assert.IsFalse(wirePlan.IsError, wirePlan.Message);
        var wireRows = await NativeManager(provider).ExecuteJoinQueryAsync(wirePlan.Result, new[] { provider });
        Assert.IsFalse(wireRows.IsError, wireRows.Message + wireRows.Exception);
        Assert.AreEqual(childId, (Guid)wireRows.Result.Providers.Single().Rows.Single().Value["ChildId"]);
        checks.Add(new { provider = provider.QueryProviderType.ToString(), linq = "scoped child input before native budget" });
    }

    private static async Task VerifyJoinOutputOrdering(ICosmicQueryJoinStorageProvider provider, Guid[] ids, List<object> checks)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider.QueryProviderType).Result);
        var admitted = new[] { ids[0], ids[1], ids[4] };
        var parentId = ids[0];
        var left = source.Where(row => row.Id == parentId).Result;
        var right = source.Where(row => admitted.Contains(row.Id)).OrderBy(row => row.Name).Result;
        var selected = left.CrossJoin(right).Select((parent, child) => new { Name = child.Name }).Result
            .OrderByOutput("Name", descending: true);
        Assert.IsFalse(selected.IsError, selected.Message);
        var native = provider;
        var sorted = CosmicJoinPlanner.Plan(selected.Result, native.QueryProviderType,
            new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
        Assert.IsFalse(sorted.IsError, sorted.Message);
        var ordered = await NativeManager(provider).ExecuteJoinQueryAsync(sorted.Result, new[] { native });
        Assert.IsFalse(ordered.IsError, ordered.Message + ordered.Exception);
        CollectionAssert.AreEqual(new[] { "visible match", "protected source root", "grandchild two" },
            ordered.Result.Providers.Single().Rows.Select(row => row.Value.Name).ToArray());
        var byDescription = left.CrossJoin(right).Select((parent, child) => new { child.Id, child.Description }).Result;
        foreach (var descending in new[] { false, true })
        {
            var orderedDescriptions = byDescription.OrderByOutput("Description", descending);
            Assert.IsFalse(orderedDescriptions.IsError, orderedDescriptions.Message);
            var nullablePlan = CosmicJoinPlanner.Plan(orderedDescriptions.Result, native.QueryProviderType,
                new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
            Assert.IsFalse(nullablePlan.IsError, nullablePlan.Message);
            var nullableRows = await NativeManager(provider).ExecuteJoinQueryAsync(nullablePlan.Result, new[] { native });
            Assert.IsFalse(nullableRows.IsError, nullableRows.Message + nullableRows.Exception);
            var expected = descending ? new[] { ids[0], ids[1], ids[4] } : new[] { ids[4], ids[0], ids[1] };
            CollectionAssert.AreEqual(expected, nullableRows.Result.Providers.Single().Rows.Select(row => row.Value.Id).ToArray());
        }
        checks.Add(new { provider = provider.QueryProviderType.ToString(), join = "native selected output ordering" });
    }

    [TestMethod]
    public async Task NativeSqliteEntityProjectionPreservesStagesAndProofBeforeDtoConstruction()
    {
        var rootPath=Environment.GetEnvironmentVariable("OASIS_CQL_NEO_ENTITY_PROJECTION_ROOT")
            ?? throw new InvalidOperationException("Use the explicit entity-projection runner.");
        const string connection="Data Source=cosmic-entity-projection-linq;Mode=Memory;Cache=Shared;Pooling=False";
        await using var keeper=new SqliteConnection(connection);await keeper.OpenAsync();
        using var provider=new SqliteProvider(connection){IsVersionControlEnabled=false};
        var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
        try
        {
            var ids=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).ToArray();
            var actor=Guid.NewGuid();
            var saved=await provider.SaveHolonAsync(CreateEntityProjectionFixture(ids,actor,Guid.NewGuid()),saveChildren:true);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var checks=new List<object>();
            await VerifyEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyStandardEntityProjection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedComposition((ICosmicQueryEntityProjectionCompositionStorageProvider)provider,ids[0],ids,checks);
            await VerifyEntitySelectedReselection((ICosmicQueryEntityProjectionStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedCardinality((ICosmicQueryEntityProjectionCardinalityStorageProvider)provider,ids[0],checks);
            await VerifyEntitySelectedAll((ICosmicQueryEntityProjectedAllStorageProvider)provider,ids[0],checks);
            await VerifyMetadataPresenceWire(provider, ids[0], checks);
            await VerifyDistinctWire(provider, ids, checks);
            await VerifyNativeFlattening(provider,ids[0],ids[1],checks);
            var unlisted = await provider.SaveHolonAsync(new Holon
            {
                Id = Guid.NewGuid(), ParentHolonId = ids[0], Name = "visible outside the child manifest",
                CreatedByAvatarId = actor, IsPublic = true
            }, saveChildren: false);
            Assert.IsFalse(unlisted.IsError, unlisted.Message + unlisted.Exception);
            await VerifyChildSource(provider, actor, ids[0], ids[1], checks);
            await VerifyJoinOutputOrdering(provider, ids, checks);
            Assert.AreEqual(66,checks.Count);
            await File.WriteAllTextAsync(Path.Combine(rootPath,"TestResults/Neo4jCurrent/sqlite-entity-projection-native.json"),
                JsonSerializer.Serialize(new{provider="SQLLiteDBOASIS",checks=checks.Count,cases=checks,inMemoryFixture=true}));
        }
        finally{Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);}
    }
}
