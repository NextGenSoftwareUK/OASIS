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
    private static async Task VerifyChildSource(SqliteProvider provider, Guid actor, Guid parentId, Guid childId,
        List<object> checks)
    {
        var native = (ICosmicQueryJoinStorageProvider)provider;
        var scope = CosmicQueryScope.Named(native.QueryProviderType).Result;
        var source = CosmicQuery<Holon>.Create(scope).WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result);
        var parents = source.Where(row => row.Id == parentId).Take(1).Result;
        var options = CosmicJoinOptions.Create(maximumLeftRows: 1, maximumRightRows: 1).Result;
        var joined = parents.JoinChildren(source, options).Select((parent, child) => child.Id);
        Assert.IsFalse(joined.IsError, joined.Message);
        var plan = CosmicJoinPlanner.Plan(joined.Result, native.QueryProviderType,
            new[] { new CosmicJoinTarget(native.QueryProviderType, native.QueryCapabilities, native.JoinCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var rows = await NativeManager(provider).ExecuteJoinQueryAsync(plan.Result, new[] { native });
        Assert.IsFalse(rows.IsError, rows.Message + rows.Exception);
        Assert.AreEqual(childId, rows.Result.Providers.Single().Rows.Single().Value);
        checks.Add(new { provider = "SQLLiteDBOASIS", linq = "scoped child input before native budget" });
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
