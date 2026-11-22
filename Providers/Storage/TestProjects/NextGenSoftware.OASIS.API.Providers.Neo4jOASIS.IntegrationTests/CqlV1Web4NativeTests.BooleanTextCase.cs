using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public Task NativeBooleanTextCaseUsesLiteralBranchesAndCallerVisibility(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,booleanTextCaseJourney:true);
    private static string ForbiddenConditionalBranch()=>throw new InvalidOperationException("Captured branch must never execute.");
    private static async Task<int> VerifyBooleanTextCaseJourney(IOASISStorageProvider provider,List<Guid> owned,
        Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        async Task<T> Timed<T>(string label,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(label,clock.ElapsedMilliseconds);}
        }
        Holon New(bool isPublic)=>new(){Id=Guid.NewGuid(),Name="conditional owned",CreatedByAvatarId=actor,
            IsPublic=isPublic,IsActive=true,IsNewHolon=true,MetaData=new()};
        var yes=New(true);var no=New(false);var foreign=New(false);
        foreign.CreatedByAvatarId=Guid.NewGuid();
        var all=new[]{yes,no,foreign};owned.AddRange(all.Select(row=>row.Id));
        // The write session does not grant read access to a foreign private row.
        Assert.IsFalse(session.AddRange(new[]{yes,no}).IsError);
        var saved=await Timed("create-owned",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        using var foreignSession=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(foreign.CreatedByAvatarId).Result).Result;
        Assert.IsFalse(foreignSession.Add(foreign).IsError);
        var foreignSaved=await Timed("create-foreign",()=>foreignSession.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(foreignSaved.IsError,foreignSaved.Message+foreignSaved.Exception);Assert.IsTrue(foreignSaved.IsSaved);
        var scope=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var ids=all.Select(row=>row.Id).ToArray();
        var root=CosmicLinq.Create(manager,adapter,scope.Where(row=>ids.Contains(row.Id)).Result).Result;
        var rows=await Timed("conditional-values",()=>root.Select(row=>row.IsPublic?"public":"private")
            .OrderBy(text=>text).ToCosmicListAsync());
        Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);Assert.IsTrue(rows.IsLoaded);
        CollectionAssert.AreEqual(new[]{"private","public"},rows.Result.ToArray());
        var pair=root.Select(row=>new{First=row.IsPublic?"yes":"no",Second=row.IsPublic?"Y":"N"})
            .Where(row=>row.First=="no");
        var pairs=await Timed("different-branches",()=>pair.ToCosmicListAsync());
        Assert.IsFalse(pairs.IsError,pairs.Message+pairs.Exception);Assert.IsTrue(pairs.IsLoaded);
        Assert.AreEqual(1,pairs.Result.Count);Assert.AreEqual("no",pairs.Result[0].First);Assert.AreEqual("N",pairs.Result[0].Second);
        Assert.ThrowsException<CosmicLinqException>(()=>root.Select(row=>row.IsPublic?"yes":ForbiddenConditionalBranch()));
        Assert.ThrowsException<CosmicLinqException>(()=>root.Select(row=>CosmicQueryMetadata.Value<bool>(row,"flag")?"yes":"no"));
        return 4;
    }
}
