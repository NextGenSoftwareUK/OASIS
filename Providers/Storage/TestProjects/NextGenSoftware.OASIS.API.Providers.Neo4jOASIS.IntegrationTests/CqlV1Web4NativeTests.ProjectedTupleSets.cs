using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeProjectedTupleSetsCompareEveryVisibleColumn(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,tupleSetJourney:true);

    private static async Task<int> VerifyNativeTupleSetJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var queryProvider=(ICosmicQueryStorageProvider)provider;
        var native=(ICosmicQueryProjectedSetStorageProvider)provider;
        var authority=queryProvider.QueryProviderType;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var other=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,queryProvider,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var foreign=CosmicSession.Create(manager,queryProvider,CosmicQueryReadAccess.OwnerOrPublic(other).Result).Result;
        Holon New(string? variant,Guid owner)=>new(){Id=Guid.NewGuid(),Name="same",Description=variant,
            CreatedByAvatarId=owner,IsPublic=false,IsActive=true,IsNewHolon=true};
        var a=New(null,actor);var b=New("different",actor);var c=New(null,actor);var hidden=New("private",other);
        ownedIds.AddRange(new[]{a.Id,b.Id,c.Id,hidden.Id});
        Assert.IsFalse(session.AddRange(new[]{a,b,c}).IsError);
        var saved=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        Assert.IsFalse(foreign.Add(hidden).IsError);
        var foreignSaved=await foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(foreignSaved.IsError,foreignSaved.Message+foreignSaved.Exception);Assert.IsTrue(foreignSaved.IsSaved);
        var ids=ownedIds.ToArray();var leftIds=new[]{a.Id,b.Id};var rightIds=new[]{c.Id,hidden.Id};
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result.Where(row=>ids.Contains(row.Id)).Result;
        var columns=new[]{
            CosmicProjectionSelection<Holon>.Create("Key",row=>row.Name).Result,
            CosmicProjectionSelection<Holon>.Create("Variant",row=>row.Description).Result};
        var left=root.Where(row=>leftIds.Contains(row.Id)).Result.OrderBy(row=>row.Description).Take(2).Result.SelectColumns(columns).Result;
        var right=root.Where(row=>rightIds.Contains(row.Id)).Result.OrderBy(row=>row.Description).Take(2).Result.SelectColumns(columns).Result;
        var target=new CosmicProjectionCompositionTarget(authority,queryProvider.QueryCapabilities,
            native.ProjectionCapabilities,native.CompositionCapabilities);
        async Task<string[]> Read(CosmicSetQueryKind kind)
        {
            var query=CosmicProjectedSetQuery<Holon,CosmicProjectionRecord>.Create(left,right,kind);
            Assert.IsFalse(query.IsError,query.Message);
            var plan=CosmicProjectedSetPlanner.Plan(query.Result,authority,target,native.ProjectedSetCapabilities);
            Assert.IsFalse(plan.IsError,plan.Message);
            var clock=System.Diagnostics.Stopwatch.StartNew();
            var result=await CosmicProjectedSetExecutor.ExecuteAsync(plan.Result,new[]{queryProvider});
            timings.Add(kind.ToString(),clock.ElapsedMilliseconds);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);
            return result.Result.Providers.Single().Rows.Select(row=>
                row.Get<string>("Key")+"|"+(row.Get<string>("Variant")??"<null>")).ToArray();
        }
        CollectionAssert.AreEquivalent(new[]{"same|<null>","same|different"},await Read(CosmicSetQueryKind.Union));
        CollectionAssert.AreEqual(new[]{"same|<null>","same|different","same|<null>"},await Read(CosmicSetQueryKind.Concat));
        CollectionAssert.AreEqual(new[]{"same|<null>"},await Read(CosmicSetQueryKind.Intersect));
        CollectionAssert.AreEqual(new[]{"same|different"},await Read(CosmicSetQueryKind.Except));
        return 4;
    }
}
