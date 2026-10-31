using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using SQLiteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [TestMethod]
    public async Task ClosedPortRetainsNativeTransportCause()
    {
        using var driver=GraphDatabase.Driver("bolt://127.0.0.1:17688",AuthTokens.Basic("neo4j","oasis-local-test-only"),
            config=>config.WithConnectionTimeout(TimeSpan.FromSeconds(1)).WithMaxTransactionRetryTime(TimeSpan.Zero));
        using var session=driver.AsyncSession();
        try{await (await session.RunAsync("RETURN 1")).ConsumeAsync();Assert.Fail("Closed owned outage port unexpectedly accepted a query.");}
        catch(Neo4j.Driver.ServiceUnavailableException cause){
            Console.WriteLine(cause.ToString());
            var provider=new Provider("bolt://127.0.0.1:17688","neo4j","oasis-local-test-only");
            Assert.IsTrue(provider.IsProviderTransportFailure(cause));
            Assert.IsFalse(provider.IsProviderTransportFailure(new OperationCanceledException()));
            Assert.IsFalse(provider.IsProviderTransportFailure(new InvalidDataException()));
        }
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)]
    [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeSearchUsesActualGroupsScopesAndCompleteSnapshots(bool synchronous,bool throughV2)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var path=Path.Combine(work,"search.db");Assert.IsFalse(File.Exists(path),"Preserved search fixture requires reconciliation.");
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=raw.AsyncSession();
        Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:Holon RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
        var neo=new Provider(uri,"neo4j","oasis-local-test-only");
        using var backup=new SQLiteProvider($"Data Source={path};Pooling=False");
        var owner=Guid.NewGuid();var firstId=Guid.NewGuid();var childId=Guid.NewGuid();var avatarId=Guid.NewGuid();
        var owned=new List<Guid> {firstId,childId,avatarId};var verified=false;
        try
        {
            Assert.IsFalse((await neo.ActivateProviderAsync()).IsError);Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            var first=new Holon {Id=firstId,Name="cedar canopy",Description="park cleanup",Version=7,CreatedByAvatarId=owner,IsActive=false};
            first.MetaData["Region"]="park";
            var child=new Holon {Id=childId,ParentHolonId=firstId,Name="river path",Version=9,CreatedByAvatarId=Guid.NewGuid()};
            Assert.IsFalse((await neo.SaveHolonAsync(first,saveChildren:false)).IsError);
            Assert.IsFalse((await neo.SaveHolonAsync(child,saveChildren:false)).IsError);
            var avatar=new Avatar {Id=avatarId,Username="keeper",FirstName="Tree Keeper",CreatedByAvatarId=owner,Password="private-never-search"};
            avatar.MetaData["Region"]="park";
            Assert.IsFalse((await neo.SaveAvatarAsync(avatar)).IsError);
            var deletedHolon=new Holon {Id=Guid.NewGuid(),Name="deleted search root",DeletedDate=DateTime.UtcNow};
            var deletedAvatar=new Avatar {Id=Guid.NewGuid(),Username="deleted-search-root",DeletedDate=DateTime.UtcNow};
            owned.Add(deletedHolon.Id);owned.Add(deletedAvatar.Id);
            var savedDeletedHolon=await neo.SaveHolonAsync(deletedHolon,saveChildren:false);Assert.IsFalse(savedDeletedHolon.IsError,savedDeletedHolon.Message);
            var savedDeletedAvatar=await neo.SaveAvatarAsync(deletedAvatar);Assert.IsFalse(savedDeletedAvatar.IsError,savedDeletedAvatar.Message);
            Assert.AreNotEqual(DateTime.MinValue,(await neo.LoadHolonAsync(deletedHolon.Id,loadChildren:false)).Result.DeletedDate);
            Assert.AreNotEqual(DateTime.MinValue,(await neo.LoadAvatarAsync(deletedAvatar.Id)).Result.DeletedDate);
            var fillers=new List<Holon>();
            if(!synchronous && !throughV2)
            {
                for(var i=0;i<101;i++) {var filler=new Holon {Id=Guid.NewGuid(),Name="owned filler "+i};fillers.Add(filler);owned.Add(filler.Id);}
                var saved=await neo.SaveHolonsAsync(fillers,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);
            }
            Assert.IsFalse((await backup.SaveHolonAsync(new Holon {Id=firstId,Name="stale provider"},saveChildren:false)).IsError);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true};manager.RegisterProvider(neo);manager.RegisterProvider(backup);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new OASISHyperDrive(manager);
            async Task<NextGenSoftware.OASIS.Common.OASISResult<ISearchResults>> Run(SearchParams query)
            {
                if(!throughV2) return synchronous ? neo.Search(query,loadChildren:false):await neo.SearchAsync(query,loadChildren:false);
                var request=new StorageOperationRequest {Operation="Search",SearchParams=query,LoadChildren=false,PreferredProvider=ProviderType.Neo4jOASIS};
                return synchronous ? router.RouteRequest<ISearchResults>(request):await router.RouteRequestAsync<ISearchResults>(request);
            }
            async Task<ISearchResults> Found(SearchParams query)
            {
                var result=await Run(query);Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);return result.Result;
            }
            SearchParams Group(ISearchGroupBase group)=>new() {SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase> {group}};
            var all=await Found(new SearchParams {SearchOnlyForCurrentAvatar=false});Assert.AreEqual(3+fillers.Count,all.NumberOfResults);
            Assert.IsFalse(all.SearchResultHolons.Single(row=>row.Id==firstId).IsActive,"Inactive nondeleted roots must remain eligible.");
            var name=Group(new SearchTextGroup {SearchQuery="CEDAR",SearchHolons=true,HolonType=HolonType.All,HolonSearchParams=new SearchHolonParams {Name=true}});
            var named=await Found(name);Assert.AreEqual(firstId,named.SearchResultHolons.Single().Id);Assert.AreEqual("park cleanup",named.SearchResultHolons.Single().Description);
            var metadata=await Found(new SearchParams {SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string> {{"Region","park"}}});
            Assert.AreEqual(2,metadata.NumberOfResults);Assert.AreEqual(avatarId,metadata.SearchResultAvatars.Single().Id);
            Assert.AreEqual(2,(await Found(new SearchParams {SearchOnlyForCurrentAvatar=true,AvatarId=owner})).NumberOfResults);
            Assert.AreEqual(childId,(await Found(new SearchParams {SearchOnlyForCurrentAvatar=false,ParentId=firstId})).SearchResultHolons.Single().Id);
            var numeric=Group(new SearchNumberGroup {SearchHolons=true,HolonType=HolonType.All,Number=9,NumberOperator=SearchOperatorType.EqualTo,HolonSearchParams=new SearchHolonParams {Version=true}});
            Assert.AreEqual(childId,(await Found(numeric)).SearchResultHolons.Single().Id);
            var identity=new SearchTextGroup {SearchHolons=true,HolonType=HolonType.All,SearchQuery=firstId.ToString(),SearchIds=false,HolonSearchParams=new SearchHolonParams {Name=true}};
            Assert.AreEqual(0,(await Found(Group(identity))).NumberOfResults);identity.SearchIds=true;
            CollectionAssert.AreEquivalent(new[] {firstId,childId},(await Found(Group(identity))).SearchResultHolons.Select(row=>row.Id).ToArray());
            var privateField=Group(new SearchTextGroup {SearchAvatars=true,HolonType=HolonType.All,SearchQuery="private-never-search",
                AvatarSearchParams=new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams {SearchAllFields=true}});
            Assert.AreEqual(0,(await Found(privateField)).NumberOfResults);
            if(throughV2)
            {
                var original=(await (await session.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",new {id=firstId.ToString()})).SingleAsync())["snapshot"].As<string>();
                await (await session.RunAsync("MATCH (h:Holon {Id:$id}) SET h.SnapshotJson='not-json' RETURN h.Id",new {id=firstId.ToString()})).ConsumeAsync();
                var failed=await Run(name);Assert.IsTrue(failed.IsError);Assert.IsFalse(failed.IsLoaded);Assert.IsNull(failed.Result);
                Assert.IsInstanceOfType(failed.Exception,typeof(InvalidDataException));
                Assert.AreEqual("stale provider",(await backup.LoadHolonAsync(firstId,loadChildren:false)).Result.Name);
                await (await session.RunAsync("MATCH (h:Holon {Id:$id}) SET h.SnapshotJson=$snapshot RETURN h.Id",new {id=firstId.ToString(),snapshot=original})).ConsumeAsync();
            }
            Assert.AreEqual(0L,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            verified=true;
        }
        finally
        {
            await neo.DeActivateProviderAsync();backup.Dispose();
            if(verified)
            {
                await (await session.RunAsync("MATCH (n) WHERE (n:Holon OR n:Avatar) AND n.Id IN $ids DETACH DELETE n",new {ids=owned.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                File.Delete(path);
            }
        }
    }
}
