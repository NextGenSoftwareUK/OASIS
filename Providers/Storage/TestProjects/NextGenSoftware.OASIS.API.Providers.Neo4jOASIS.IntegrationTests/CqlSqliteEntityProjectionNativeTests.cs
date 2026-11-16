using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
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
        checks.Add(new { provider = provider.QueryProviderType.ToString(), linq = "scoped child input before native budget" });
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
            Assert.AreEqual(65,checks.Count);
            await File.WriteAllTextAsync(Path.Combine(rootPath,"TestResults/Neo4jCurrent/sqlite-entity-projection-native.json"),
                JsonSerializer.Serialize(new{provider="SQLLiteDBOASIS",checks=checks.Count,cases=checks,inMemoryFixture=true}));
        }
        finally{Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);}
    }
}
