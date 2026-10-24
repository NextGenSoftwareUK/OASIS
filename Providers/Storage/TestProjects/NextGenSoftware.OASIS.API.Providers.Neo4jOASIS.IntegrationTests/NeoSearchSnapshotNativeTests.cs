using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    private sealed class SearchMutationProvider(string uri):Provider(uri,"neo4j","oasis-local-test-only")
    {
        public Action? BeforeRead;
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {BeforeRead?.Invoke();return base.LoadAllHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeSearchKeepsOriginalOwnerAndMasksAcrossRealRead(bool sync)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        Assert.AreEqual(0,Directory.GetFiles(work,"neo-search-snapshot-*.pending.json").Length,"Reconcile original search fixtures before another test.");
        var journal=Path.Combine(work,$"neo-search-snapshot-{sync}.pending.json");var observedFile=Path.Combine(work,$"neo-search-snapshot-{sync}.observed.json");
        Assert.IsFalse(File.Exists(observedFile));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var check=raw.AsyncSession();
        Assert.AreEqual(0L,(await(await check.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var owner=Guid.NewGuid();var other=Guid.NewGuid();var marker=Guid.NewGuid().ToString("D");
        var first=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,Name="oak",Description="east",MetaData=new(){{"searchFixture",marker},{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,Name="pine",Description="oak",MetaData=new(){{"searchFixture",marker},{"zone","south"}}},new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=other,Name="oak",MetaData=new(){{"searchFixture",marker},{"zone","north"}}}};
        var ids=rows.Select(row=>row.Id.ToString()).ToArray();
        static async Task WriteNew(string path,object value){using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(value));file.Flush(true);}
        await WriteNew(journal,new{uri,ids,marker,stage="before-owned-search-create"});
        var provider=new SearchMutationProvider(uri);
        async Task<Dictionary<string,string>> Read()=> (await(await check.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN h.Id AS id,h.SnapshotJson AS snapshot",new{ids})).ToListAsync()).ToDictionary(row=>row["id"].As<string>(),row=>row["snapshot"].As<string>());
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var row in rows){var saved=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
            var before=await Read();Assert.AreEqual(3,before.Count);
            await WriteNew(observedFile,before.ToDictionary(pair=>pair.Key,pair=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(pair.Value)))));
            for(var kind=0;kind<3;kind++){
                var group=new SearchTextGroup{SearchHolons=true,HolonType=HolonType.All,SearchQuery="oak",HolonSearchParams=new SearchHolonParams{Name=true}};
                var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,FilterByMetaData=new(){{"searchFixture",marker},{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
                provider.BeforeRead=()=>{if(kind==0)query.AvatarId=other;else if(kind==1){group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;query.FilterByMetaData["zone"]="south";}else query.FilterByMetaData["zone"]="south";};
                var found=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
                Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.AreEqual(first.Id,found.Result.SearchResultHolons.Single().Id);Assert.AreEqual(1,found.LoadedCount);
            }
            var after=await Read();CollectionAssert.AreEquivalent(before.ToArray(),after.ToArray(),"Search must not rewrite native canonical records.");
            foreach(var pair in before){var removed=await(await check.RunAsync("MATCH (h:Holon {Id:$id}) WHERE h.SnapshotJson=$snapshot WITH h DETACH DELETE h RETURN 1 AS removed",new{id=pair.Key,snapshot=pair.Value})).SingleAsync();Assert.AreEqual(1L,removed["removed"].As<long>());}
            Assert.AreEqual(0,(await Read()).Count);File.Delete(observedFile);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
