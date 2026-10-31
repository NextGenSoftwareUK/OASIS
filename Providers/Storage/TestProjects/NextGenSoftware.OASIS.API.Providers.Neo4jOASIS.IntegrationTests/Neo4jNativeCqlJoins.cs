using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static CosmicJoinPlan<TLeft,TRight,TKey,TResult> JoinPlan<TLeft,TRight,TKey,TResult>(
        CosmicJoinSelection<TLeft,TRight,TKey,TResult> query) where TLeft:IHolon where TRight:IHolon
    {
        var plan=CosmicJoinPlanner.Plan(query,ProviderType.Neo4jOASIS,new[]{
            new CosmicJoinTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities,Provider.CqlJoinCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeJoinPublishesDetachedFanoutAndUnmatchedPresence(bool outer)=>WithNativeGraph(async fixture=>
    {
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L").OrderBy(h=>h.Name);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Description=="R");
        var selected=(outer?left.LeftJoin(right,h=>h.Name,h=>h.Name):left.Join(right,h=>h.Name,h=>h.Name))
            .Select((l,r)=>new{Label=l.Name,Right=r.Name,Present=r!=null}).Result;
        var read=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
        Assert.IsFalse(read.IsError,read.Message+read.Exception);
        var rows=read.Result.Providers.Single().Rows;
        Assert.AreEqual(outer?3:2,rows.Count);Assert.AreEqual(2,rows.Count(row=>row.HasRight));
        Assert.IsTrue(rows.All(row=>row.HasRight==row.Value.Present));
        if(outer)Assert.IsNull(rows.Last().Value.Right);
    },configure:root=>
    {
        root.Name="coffee";root.Description="L";
        foreach(var child in root.Children){child.Name="coffee";child.Description="R";}
        var other=root.Children.First().Children.Single();other.Name="tea";other.Description="L";
    });

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)] [DataRow(6)]
    public Task NativeJoinEnforcesEveryRemainingBudgetAndAllowsFreshReuse(int budget)=>WithNativeGraph(async fixture=>
    {
        var id=fixture.Root.Id;var source=CosmicQuery<Holon>.Create().Where(h=>h.Id==id).Result;
        var selected=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        var plan=JoinPlan(selected);var limits=new[]{10,10,10,10,10000,10000,10000};limits[budget]=0;
        var failed=await fixture.Provider.ExecuteCosmicJoinAsync(plan,
            new CosmicJoinExecutionLimits(limits[0],limits[1],limits[2],limits[3],limits[4],limits[5],limits[6]));
        Assert.AreEqual("CQL_QUERY_LIMIT",failed.ErrorCode,failed.Message+failed.Exception);Assert.IsFalse(failed.IsLoaded);Assert.IsNull(failed.Result);
        var fresh=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(plan,new[]{fixture.Provider});
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);
        Assert.AreEqual(fixture.Root.Name,fresh.Result.Providers.Single().Rows.Single().Value);
    },width:0);

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeJoinValidatesUnmatchedSourceDomainsBeforePublication(bool missing)=>WithNativeGraph(async fixture=>
    {
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L");
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Description=="R");
        var selected=left.LeftJoin(right,h=>h.Name,h=>h.Name)
            .Select((l,r)=>new{l.Name,Stock=(long?)CosmicQueryMetadata.Value<long>(r,"stock")}).Result;
        var read=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
        Assert.AreEqual("CQL_DATA_INVALID",read.ErrorCode,read.Message+read.Exception);Assert.IsFalse(read.IsLoaded);Assert.IsNull(read.Result);
    },width:1,configure:root=>
    {
        root.Description="L";var right=root.Children.Single();right.Description="R";
        if(!missing)right.MetaData["stock"]="wrong-domain";
    });

    [TestMethod]
    public Task NativeJoinFiltersBothSourcesBeforeMatchingAndPaging()
    {
        var actor=Guid.NewGuid();var other=Guid.NewGuid();
        return WithNativeGraph(async fixture=>
        {
            var id=fixture.Root.Id;
            var source=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
            var left=source.Where(h=>h.Id==id);var right=source.Where(h=>h.ParentHolonId==id).Take(1);
            var selected=left.Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>r.Id).Result;
            var read=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
            Assert.IsFalse(read.IsError,read.Message+read.Exception);
            Assert.AreEqual(fixture.Root.Children.Last().Id,read.Result.Providers.Single().Rows.Single().Value);
        },configure:root=>
        {
            root.Name="coffee";root.CreatedByAvatarId=actor;
            foreach(var child in root.Children){child.Name="coffee";child.CreatedByAvatarId=other;child.IsPublic=false;}
            root.Children.Last().IsPublic=true;
        });
    }

    [TestMethod]
    public Task NativeJoinPreCancellationPublishesNothingAndPermitsFreshRead()=>WithNativeGraph(async fixture=>
    {
        var id=fixture.Root.Id;var source=CosmicQuery<Holon>.Create().Where(h=>h.Id==id).Result;
        var query=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;var plan=JoinPlan(query);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var failed=await fixture.Provider.ExecuteCosmicJoinAsync(plan,new CosmicJoinExecutionLimits(10,10,10,10,10000,10000,10000),cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED",failed.ErrorCode);Assert.IsNull(failed.Result);
        var fresh=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(plan,new[]{fixture.Provider});
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(1,fresh.Result.Providers.Single().Rows.Count);
    },width:0);

    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)]
    [DataRow(6)] [DataRow(7)] [DataRow(8)] [DataRow(9)] [DataRow(10)] [DataRow(11)]
    public Task NativeJoinUsesExactMetadataKeyDomains(int domain)=>domain switch
    {
        0=>ExactJoinKey(9007199254740993L,9007199254740992L),
        1=>ExactJoinKey(ulong.MaxValue,ulong.MaxValue-1),
        2=>ExactJoinKey(123456789.123456789123456789m,123456789.123456789123456788m),
        3=>ExactJoinKey(1.0000000000000002d,1d),
        4=>ExactJoinKey(1.0000001f,1f),
        5=>ExactJoinKey(new DateTime(638900000000000001,DateTimeKind.Utc),new DateTime(638900000000000000,DateTimeKind.Utc)),
        6=>ExactJoinKey(Guid.Parse("a0000000-0000-0000-0000-000000000001"),Guid.Parse("a0000000-0000-0000-0000-000000000002")),
        7=>ExactJoinKey(true,false),
        8=>ExactJoinKey("coffee' OR 1=1 --","COFFEE' OR 1=1 --"),
        9=>ExactJoinKey(int.MaxValue,int.MaxValue-1),
        10=>ExactJoinKey((long?)9007199254740993L,(long?)null),
        _=>ExactJoinKey(ProviderType.Neo4jOASIS,ProviderType.SQLLiteDBOASIS)
    };

    private static Task ExactJoinKey<T>(T key,T different)=>WithNativeGraph(async fixture=>
    {
        var id=fixture.Root.Id;
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.ParentHolonId==id);
        var selected=left.Join(right,h=>CosmicQueryMetadata.Value<T>(h,"key"),h=>CosmicQueryMetadata.Value<T>(h,"key"))
            .Select((l,r)=>new{Left=CosmicQueryMetadata.Value<T>(l,"key"),Right=CosmicQueryMetadata.Value<T>(r,"key")});
        Assert.IsFalse(selected.IsError,selected.Message);
        var read=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected.Result),new[]{fixture.Provider});
        Assert.IsFalse(read.IsError,read.Message+read.Exception);
        var row=read.Result.Providers.Single().Rows.Single().Value;
        Assert.AreEqual(key,row.Left);Assert.AreEqual(key,row.Right);
    },configure:root=>
    {
        root.MetaData["key"]=key!;root.Children.First().MetaData["key"]=key!;
        root.Children.Last().MetaData["key"]=different!;
    });

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeJoinPreservesMissingNullOrderingThroughSourceWindows(bool descending)=>WithNativeGraph(async fixture=>
    {
        var id=fixture.Root.Id;var source=CosmicQuery<Holon>.Create().Where(h=>h.ParentHolonId==id);
        var ordered=descending?source.OrderByDescending(h=>CosmicQueryMetadata.Value<long>(h,"rank"))
            :source.OrderBy(h=>CosmicQueryMetadata.Value<long>(h,"rank"));
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
        var selected=ordered.Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>l.Id).Result;
        var read=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
        Assert.IsFalse(read.IsError,read.Message+read.Exception);
        var ids=fixture.Root.Children.Select(child=>child.Id).ToArray();
        CollectionAssert.AreEqual(descending?ids.Reverse().ToArray():ids,read.Result.Providers.Single().Rows.Select(row=>row.Value).ToArray());
        var page=ordered.Skip(1).Take(1).Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>l.Id).Result;
        var paged=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(page),new[]{fixture.Provider});
        Assert.IsFalse(paged.IsError,paged.Message+paged.Exception);
        Assert.AreEqual(descending?ids[0]:ids[1],paged.Result.Providers.Single().Rows.Single().Value);
    },configure:root=>
    {
        root.Name="coffee";foreach(var child in root.Children)child.Name="coffee";
        root.Children.Last().MetaData["rank"]=null!;
    });

    [TestMethod]
    public Task NativeJoinCompositeKeysRequireEveryComponent()=>WithNativeGraph(async fixture=>
    {
        var id=fixture.Root.Id;
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.ParentHolonId==id);
        var query=left.Join(right,h=>new{h.Name,Region=CosmicQueryMetadata.Value<long>(h,"region")},
            h=>new{h.Name,Region=CosmicQueryMetadata.Value<long>(h,"region")}).Select((l,r)=>r.Id).Result;
        var read=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(query),new[]{fixture.Provider});
        Assert.IsFalse(read.IsError,read.Message+read.Exception);
        Assert.AreEqual(fixture.Root.Children.First().Id,read.Result.Providers.Single().Rows.Single().Value);
    },configure:root=>
    {
        root.Name="coffee";root.MetaData["region"]=7L;
        foreach(var child in root.Children)child.Name="coffee";
        root.Children.First().MetaData["region"]=7L;root.Children.Last().MetaData["region"]=8L;
    });

    [TestMethod]
    public Task NativeJoinNeverConstructsSourceEntities()=>WithNativeGraph(async fixture=>
    {
        var id=fixture.Root.Id;
        var left=CosmicQuery<ScalarMustNotConstruct>.Create().Where(h=>h.Id==id).Result;
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Id==id).Result;
        var query=left.Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
        var read=await GraphManager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(query),new[]{fixture.Provider});
        Assert.IsFalse(read.IsError,read.Message+read.Exception);
        Assert.AreEqual(fixture.Root.Name,read.Result.Providers.Single().Rows.Single().Value);
    },width:0);

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeJoinCancellationInterruptsPausedBoltResponse(bool warm)=>RunPausedReadAsync(false,warm,"join",false);

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeJoinDeadlineInterruptsPausedBoltResponse(bool warm)=>RunPausedReadAsync(false,warm,"join",true);
}
