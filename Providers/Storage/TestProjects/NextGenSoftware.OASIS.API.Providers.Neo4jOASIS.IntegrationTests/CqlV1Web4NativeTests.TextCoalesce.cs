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
    public Task NativeTextCoalescePreservesNullEmptyAndInvalidDomains(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,textCoalesceJourney:true);
    private static string ForbiddenCoalesceReplacement()=>throw new InvalidOperationException("Captured query code must never run.");
    private static async Task<int> VerifyTextCoalesceJourney(IOASISStorageProvider provider,List<Guid> owned,
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
        Holon New()=>new(){Id=Guid.NewGuid(),Name="coalesce owned",CreatedByAvatarId=actor,
            IsPublic=false,IsActive=true,IsNewHolon=true,MetaData=new()};
        var missing=New();var nil=New();var empty=New();var value=New();var invalid=New();
        nil.MetaData["optional"]=null!;empty.MetaData["optional"]="";value.MetaData["optional"]="known";
        invalid.MetaData["optional"]=12;
        var all=new[]{missing,nil,empty,value,invalid};owned.AddRange(all.Select(row=>row.Id));
        Assert.IsFalse(session.AddRange(all).IsError);
        var saved=await Timed("create-owned",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        var scope=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var ids=new[]{missing.Id,nil.Id,empty.Id,value.Id};
        var root=CosmicLinq.Create(manager,adapter,scope.Where(row=>ids.Contains(row.Id)).Result).Result;
        var query=root.Select(row=>CosmicQueryMetadata.Value<string>(row,"optional")??"unknown").OrderBy(text=>text);
        var rows=await Timed("coalesced-values",()=>query.ToCosmicListAsync());
        Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);Assert.IsTrue(rows.IsLoaded);
        CollectionAssert.AreEqual(new[]{"","known","unknown","unknown"},rows.Result.ToArray());
        var pair=root.Select(row=>new{
            First=CosmicQueryMetadata.Value<string>(row,"optional")??"a",
            Second=CosmicQueryMetadata.Value<string>(row,"optional")??"b"}).Where(row=>row.First=="a");
        var pairs=await Timed("different-replacements",()=>pair.ToCosmicListAsync());
        Assert.IsFalse(pairs.IsError,pairs.Message+pairs.Exception);Assert.IsTrue(pairs.IsLoaded);
        Assert.AreEqual(2,pairs.Result.Count);
        foreach(var row in pairs.Result){Assert.AreEqual("a",row.First);Assert.AreEqual("b",row.Second);}
        var bad=CosmicLinq.Create(manager,adapter,scope.Where(row=>row.Id==invalid.Id).Result).Result
            .Select(row=>CosmicQueryMetadata.Value<string>(row,"optional")??"unknown");
        var refused=await Timed("invalid-domain",()=>bad.ToCosmicListAsync());
        Assert.IsTrue(refused.IsError,"Coalescing must not hide a stored non-string value.");Assert.IsNull(refused.Result);
        Assert.ThrowsException<CosmicLinqException>(()=>root.Select(row=>row.Name??ForbiddenCoalesceReplacement()));
        return 4;
    }
}
