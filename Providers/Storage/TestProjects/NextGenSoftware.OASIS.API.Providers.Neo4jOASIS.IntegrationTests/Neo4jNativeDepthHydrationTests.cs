using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false,false)] [DataRow(false,true,false)] [DataRow(true,false,false)] [DataRow(true,true,false)]
    [DataRow(false,false,true)] [DataRow(false,true,true)] [DataRow(true,false,true)] [DataRow(true,true,true)]
    public async Task NativeCosmicReadDepthHonorsFullRangeAndHistoricalFrontiers(bool synchronous,bool byKey,bool historical)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=raw.AsyncSession();
        var marker=Guid.NewGuid().ToString("N");
        var grand=new Holon{Id=Guid.NewGuid(),Name="depth-grand-"+marker};
        var child=new Holon{Id=Guid.NewGuid(),Name="depth-child-"+marker,Children=new List<IHolon>{grand}};
        var root=new Holon{Id=Guid.NewGuid(),Name="depth-root-"+marker,Children=new List<IHolon>{child}};
        root.MetaData["owned-depth-marker"]=marker;
        var ids=new[]{root.Id.ToString(),child.Id.ToString(),grand.Id.ToString()};var setupKnown=false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.AreEqual(0L,(await(await session.RunAsync("MATCH (n:Holon) WHERE n.Id IN $ids RETURN count(n) AS count",new{ids})).SingleAsync())["count"].As<long>());
            Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
            root.Name+="-v2";child.Name+="-v2";grand.Name+="-v2";
            var saved=await provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            Assert.IsNull(saved.Exception);Assert.AreEqual(2,child.Version);
            setupKnown=true; // All subsequent operations are reads; cleanup cannot discard an uncertain write.
            var version=historical ? 1:0;
            foreach(var fromProvider in new[]{false,true})
            foreach(var load in new[]{false,true})
            foreach(var recursive in new[]{false,true})
            foreach(var maximum in new[]{int.MaxValue,0})
            {
                var result=byKey
                    ? synchronous ? provider.LoadHolonsForParent(root.Id.ToString(),loadChildren:load,recursive:recursive,maxChildDepth:maximum,curentChildDepth:int.MaxValue,loadChildrenFromProvider:fromProvider,version:version)
                        : await provider.LoadHolonsForParentAsync(root.Id.ToString(),loadChildren:load,recursive:recursive,maxChildDepth:maximum,curentChildDepth:int.MaxValue,loadChildrenFromProvider:fromProvider,version:version)
                    : synchronous ? provider.LoadHolonsForParent(root.Id,loadChildren:load,recursive:recursive,maxChildDepth:maximum,curentChildDepth:int.MaxValue,loadChildrenFromProvider:fromProvider,version:version)
                        : await provider.LoadHolonsForParentAsync(root.Id,loadChildren:load,recursive:recursive,maxChildDepth:maximum,curentChildDepth:int.MaxValue,loadChildrenFromProvider:fromProvider,version:version);
                Assert.IsFalse(result.IsError,result.Message+result.Exception);
                Assert.AreEqual(child.Id,result.Result.Single().Id);Assert.AreEqual(historical ? 1:2,result.Result.Single().Version);
                Assert.AreEqual(load && recursive && maximum==0 ? 1:0,result.Result.Single().Children.Count);
            }
            foreach(var current in new[]{1,int.MaxValue})
            foreach(var maximum in new[]{0,1,int.MaxValue})
            {
                var all=synchronous ? provider.LoadAllHolons(maxChildDepth:maximum,curentChildDepth:current,version:version)
                    : await provider.LoadAllHolonsAsync(maxChildDepth:maximum,curentChildDepth:current,version:version);
                var metadata=synchronous ? provider.LoadHolonsByMetaData("owned-depth-marker",marker,maxChildDepth:maximum,curentChildDepth:current,version:version)
                    : await provider.LoadHolonsByMetaDataAsync("owned-depth-marker",marker,maxChildDepth:maximum,curentChildDepth:current,version:version);
                foreach(var result in new[]{all,metadata})
                {
                    Assert.IsFalse(result.IsError,result.Message+result.Exception);
                    Assert.AreEqual(maximum==0 || current<maximum ? 1:0,result.Result.Single(row=>row.Id==root.Id).Children.Count);
                }
            }
        }
        finally
        {
            if(setupKnown)
            {
                await(await session.RunAsync("MATCH (n:Holon) WHERE n.Id IN $ids DETACH DELETE n",new{ids})).ConsumeAsync();
                await(await session.RunAsync("MATCH (n:OasisRecordVersion {Kind:'Holon'}) WHERE n.RecordId IN $ids DELETE n",new{ids})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();
        }
    }

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicReadDepthHydratesDeepGraphsWithoutRecursiveQueries(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var session=raw.AsyncSession();
        var rows=Enumerable.Range(0,1024).Select(index=>new Holon{Id=Guid.NewGuid(),Name="deep-neo-"+index}).ToArray();
        for(var index=1;index<rows.Length;index++){rows[index].ParentHolonId=rows[index-1].Id;rows[index-1].Children.Add(rows[index]);}
        var ids=rows.Select(row=>row.Id.ToString()).ToArray();var setupKnown=false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var saved=await provider.SaveHolonAsync(rows[0]);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsNull(saved.Exception);setupKnown=true;
            var loaded=synchronous ? provider.LoadHolon(rows[0].Id):await provider.LoadHolonAsync(rows[0].Id);
            Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);var current=loaded.Result;
            for(var index=0;index<rows.Length;index++)
            {Assert.AreEqual(rows[index].Id,current.Id);Assert.AreEqual(index+1<rows.Length ? 1:0,current.Children.Count);if(index+1<rows.Length)current=current.Children.Single();}
        }
        finally
        {if(setupKnown)await(await session.RunAsync("MATCH (n:Holon) WHERE n.Id IN $ids DETACH DELETE n",new{ids})).ConsumeAsync();await provider.DeActivateProviderAsync();}
    }
}
