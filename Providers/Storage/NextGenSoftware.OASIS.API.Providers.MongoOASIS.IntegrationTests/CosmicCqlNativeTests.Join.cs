using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private static CosmicJoinPlan<Holon,Holon,TKey,TResult> JoinPlan<TKey,TResult>(CosmicJoinSelection<Holon,Holon,TKey,TResult> query)
    {
        var plan=CosmicJoinPlanner.Plan(query,ProviderType.MongoDBOASIS,new[]{
            new CosmicJoinTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities,Provider.CqlJoinCapabilities)});
        Assert.False(plan.IsError,plan.Message);return plan.Result;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeJoinPublishesFanoutAndUnmatchedPresence(bool outer)
        =>WithMongoGraph(async fixture=>
        {
            var left=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L").OrderBy(h=>h.Name);
            var right=CosmicQuery<Holon>.Create().Where(h=>h.Description=="R");
            var selected=(outer?left.LeftJoin(right,h=>h.Name,h=>h.Name):left.Join(right,h=>h.Name,h=>h.Name))
                .Select((l,r)=>new{Label=l.Name,Right=r.Name,Present=r!=null}).Result;
            var read=await Manager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
            Assert.False(read.IsError,read.Message+read.Exception);
            var rows=read.Result.Providers.Single().Rows;
            Assert.Equal(outer?3:2,rows.Count);Assert.Equal(2,rows.Count(row=>row.HasRight));
            Assert.All(rows,row=>Assert.Equal(row.HasRight,row.Value.Present));
            if(outer)Assert.Null(rows.Last().Value.Right);
        },configure:root=>
        {
            root.Name="coffee";root.Description="L";
            foreach(var child in root.Children){child.Name="coffee";child.Description="R";}
            var other=root.Children.First().Children.Single();other.Name="tea";other.Description="L";
        });

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public Task NativeJoinEnforcesEveryRemainingBudgetAndAllowsFreshReuse(int budget)
        =>WithMongoGraph(async fixture=>
        {
            var id=fixture.Root.Id;
            var source=CosmicQuery<Holon>.Create().Where(h=>h.Id==id).Result;
            var selected=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
            var plan=JoinPlan(selected);var limits=new[]{10,10,10,10,10000,10000,10000};limits[budget]=0;
            var failed=await fixture.Provider.ExecuteCosmicJoinAsync(plan,
                new CosmicJoinExecutionLimits(limits[0],limits[1],limits[2],limits[3],limits[4],limits[5],limits[6]));
            Assert.True(failed.IsError);Assert.Equal("CQL_QUERY_LIMIT",failed.ErrorCode);Assert.False(failed.IsLoaded);Assert.Null(failed.Result);
            var fresh=await Manager(fixture.Provider).ExecuteJoinQueryAsync(plan,new[]{fixture.Provider});
            Assert.False(fresh.IsError,fresh.Message+fresh.Exception);
            Assert.Equal(fixture.Root.Name,fresh.Result.Providers.Single().Rows.Single().Value);
        },width:0);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeJoinValidatesUnmatchedSourceDomainsBeforePublication(bool missing)
        =>WithMongoGraph(async fixture=>
        {
            var left=CosmicQuery<Holon>.Create().Where(h=>h.Description=="L");
            var right=CosmicQuery<Holon>.Create().Where(h=>h.Description=="R");
            var selected=left.LeftJoin(right,h=>h.Name,h=>h.Name)
                .Select((l,r)=>new{l.Name,Stock=(long?)CosmicQueryMetadata.Value<long>(r,"stock")}).Result;
            var read=await Manager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
            Assert.Equal("CQL_DATA_INVALID",read.ErrorCode);Assert.False(read.IsLoaded);Assert.Null(read.Result);
        },width:1,configure:root=>
        {
            root.Description="L";var right=root.Children.Single();right.Description="R";
            if(!missing)right.MetaData["stock"]="wrong-domain";
        });

    [Fact]
    public Task NativeJoinFiltersBothSourcesBeforeMatchingAndPaging()
    {
        var actor=Guid.NewGuid();var other=Guid.NewGuid();
        return WithMongoGraph(async fixture=>
        {
            var rootId=fixture.Root.Id;var children=fixture.Root.Children.Select(child=>child.Id.ToString("D")).ToArray();
            fixture.KnownOutcome=false;
            var changed=await fixture.Records.UpdateManyAsync(new BsonDocument("HolonId",new BsonDocument("$in",new BsonArray(children))),
                new BsonDocument("$set",new BsonDocument{{"CreatedByAvatarId",other.ToString("D")},{"IsPublic",false}}));
            fixture.KnownOutcome=true;Assert.Equal(2,changed.MatchedCount);
            var visible=fixture.Root.Children.Last().Id;
            fixture.KnownOutcome=false;
            var published=await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId",visible.ToString("D")),
                new BsonDocument("$set",new BsonDocument("IsPublic",true)));
            fixture.KnownOutcome=true;Assert.Equal(1,published.MatchedCount);
            var source=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
            var left=source.Where(h=>h.Id==rootId);var right=source.Where(h=>h.ParentHolonId==rootId).Take(1);
            var selected=left.Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>r.Id).Result;
            var read=await Manager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
            Assert.False(read.IsError,read.Message+read.Exception);
            Assert.Equal(visible,read.Result.Providers.Single().Rows.Single().Value);
        },actor:actor,configure:root=>{root.Name="coffee";foreach(var child in root.Children)child.Name="coffee";});
    }

    [Fact]
    public Task NativeJoinPreCancellationPublishesNothingAndPermitsFreshRead()
        =>WithMongoGraph(async fixture=>
        {
            var id=fixture.Root.Id;var source=CosmicQuery<Holon>.Create().Where(h=>h.Id==id).Result;
            var selected=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;
            var plan=JoinPlan(selected);using var cancellation=new CancellationTokenSource();cancellation.Cancel();
            var failed=await fixture.Provider.ExecuteCosmicJoinAsync(plan,new CosmicJoinExecutionLimits(10,10,10,10,10000,10000,10000),cancellation.Token);
            Assert.Equal("CQL_QUERY_CANCELLED",failed.ErrorCode);Assert.Null(failed.Result);
            var fresh=await Manager(fixture.Provider).ExecuteJoinQueryAsync(plan,new[]{fixture.Provider});
            Assert.False(fresh.IsError,fresh.Message+fresh.Exception);Assert.Single(fresh.Result.Providers.Single().Rows);
        },width:0);

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
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
        _=>ExactJoinKey(ProviderType.MongoDBOASIS,ProviderType.SQLLiteDBOASIS)
    };

    private Task ExactJoinKey<T>(T key,T different)=>WithMongoGraph(async fixture=>
    {
        var id=fixture.Root.Id;
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.ParentHolonId==id);
        var selected=left.Join(right,h=>CosmicQueryMetadata.Value<T>(h,"key"),h=>CosmicQueryMetadata.Value<T>(h,"key"))
            .Select((l,r)=>new{Left=CosmicQueryMetadata.Value<T>(l,"key"),Right=CosmicQueryMetadata.Value<T>(r,"key")});
        Assert.False(selected.IsError,selected.Message);
        var read=await Manager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected.Result),new[]{fixture.Provider});
        Assert.False(read.IsError,read.Message+read.Exception);
        var row=read.Result.Providers.Single().Rows.Single().Value;
        Assert.Equal(key,row.Left);Assert.Equal(key,row.Right);
    },configure:root=>
    {
        root.MetaData["key"]=key!;root.Children.First().MetaData["key"]=key!;
        root.Children.Last().MetaData["key"]=different!;
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeJoinPreservesMissingNullOrderingThroughSourceWindows(bool descending)
        =>WithMongoGraph(async fixture=>
        {
            var id=fixture.Root.Id;
            var source=CosmicQuery<Holon>.Create().Where(h=>h.ParentHolonId==id);
            var ordered=descending?source.OrderByDescending(h=>CosmicQueryMetadata.Value<long>(h,"rank"))
                :source.OrderBy(h=>CosmicQueryMetadata.Value<long>(h,"rank"));
            var right=CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
            var selected=ordered.Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>l.Id).Result;
            var read=await Manager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
            Assert.False(read.IsError,read.Message+read.Exception);
            var ids=fixture.Root.Children.Select(child=>child.Id).ToArray();
            Assert.Equal(descending?ids.Reverse():ids,read.Result.Providers.Single().Rows.Select(row=>row.Value));
            var page=ordered.Skip(1).Take(1).Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>l.Id).Result;
            var paged=await Manager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(page),new[]{fixture.Provider});
            Assert.False(paged.IsError,paged.Message+paged.Exception);
            Assert.Equal(descending?ids[0]:ids[1],paged.Result.Providers.Single().Rows.Single().Value);
        },configure:root=>
        {
            root.Name="coffee";foreach(var child in root.Children)child.Name="coffee";
            root.Children.Last().MetaData["rank"]=null!;
        });

    [Fact]
    public Task NativeJoinCompositeKeysRequireAllComponents()=>WithMongoGraph(async fixture=>
    {
        var id=fixture.Root.Id;
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.ParentHolonId==id);
        var selected=left.Join(right,h=>new{h.Name,Region=CosmicQueryMetadata.Value<long>(h,"region")},
            h=>new{h.Name,Region=CosmicQueryMetadata.Value<long>(h,"region")}).Select((l,r)=>r.Id).Result;
        var read=await Manager(fixture.Provider).ExecuteJoinQueryAsync(JoinPlan(selected),new[]{fixture.Provider});
        Assert.False(read.IsError,read.Message+read.Exception);
        Assert.Equal(fixture.Root.Children.First().Id,read.Result.Providers.Single().Rows.Single().Value);
    },configure:root=>
    {
        root.Name="coffee";root.MetaData["region"]=7L;
        foreach(var child in root.Children)child.Name="coffee";
        root.Children.First().MetaData["region"]=7L;root.Children.Last().MetaData["region"]=8L;
    });

    [Fact]
    public Task NativeJoinKeepsBothSourcesAndMatchingInOneSnapshotAcrossConcurrentWrite()=>WithMongoGraph(async fixture=>
    {
        var id=fixture.Root.Id;var child=fixture.Root.Children.Single().Id;
        var left=CosmicQuery<Holon>.Create().Where(h=>h.Id==id);
        var right=CosmicQuery<Holon>.Create().Where(h=>h.Id==child);
        var selected=left.Join(right,h=>h.Name,h=>h.Name).Select((l,r)=>r.Name).Result;
        var plan=JoinPlan(selected);var admin=fixture.Client.GetDatabase("admin");
        var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},
            {"mode",new BsonDocument("skip",1)},{"data",new BsonDocument{{"failCommands",new BsonArray{"aggregate"}},
                {"appName","CosmicCqlGraph"},{"blockConnection",true},{"blockTimeMS",5000}}}});
        Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicJoinExecution<string>>>? pending=null;
        try
        {
            pending=Manager(fixture.Provider).ExecuteJoinQueryAsync(plan,new[]{fixture.Provider});
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"waitForFailPoint","failCommand"},
                {"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",10000}});
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
            fixture.KnownOutcome=false;
            var changed=await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId",child.ToString("D")),
                new BsonDocument("$set",new BsonDocument("Name","tea")));
            fixture.KnownOutcome=true;Assert.Equal(1,changed.ModifiedCount);
            var read=await pending;Assert.False(read.IsError,read.Message+read.Exception);
            Assert.Equal("coffee",read.Result.Providers.Single().Rows.Single().Value);
            var fresh=await Manager(fixture.Provider).ExecuteJoinQueryAsync(plan,new[]{fixture.Provider});
            Assert.False(fresh.IsError,fresh.Message+fresh.Exception);Assert.Empty(fresh.Result.Providers.Single().Rows);
        }
        finally
        {
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
            if(pending!=null)await pending;
        }
    },width:1,configure:root=>{root.Name="coffee";root.Children.Single().Name="coffee";});

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeJoinInterruptsOutstandingIoAndUsesOneDeadline(bool expire)=>WithMongoGraph(async fixture=>
    {
        var id=fixture.Root.Id;var source=CosmicQuery<Holon>.Create().Where(h=>h.Id==id).Result;
        var query=source.Join(source,h=>h.Name,h=>h.Name).Select((l,r)=>l.Name).Result;var plan=JoinPlan(query);
        var budgets=new System.Collections.Concurrent.ConcurrentQueue<long>();
        var settings=MongoClientSettings.FromConnectionString(fixture.Provider.ConnectionString);
        settings.MaxConnectionPoolSize=30;
        settings.ClusterConfigurator=cluster=>cluster.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(command=>
        {
            if(command.CommandName=="aggregate")budgets.Enqueue(command.Command["maxTimeMS"].ToInt64());
        });
        var monitored=new MongoClient(settings);
        fixture.Provider.Database.MongoClient=monitored;
        fixture.Provider.Database.MongoDB=monitored.GetDatabase(fixture.Provider.DBName);
        var admin=fixture.Client.GetDatabase("admin");
        var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
            {"data",new BsonDocument{{"failCommands",new BsonArray{"aggregate"}},{"appName","CosmicCqlGraph"},
                {"blockConnection",true},{"blockTimeMS",expire?11000:3000}}}});
        using var cancellation=new CancellationTokenSource();
        Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicJoinPacket>>? pending=null;BsonDocument? disabled=null;
        try
        {
            pending=fixture.Provider.ExecuteCosmicJoinAsync(plan,new CosmicJoinExecutionLimits(10,10,10,10,10000,10000,10000),cancellation.Token);
            if(!expire)
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"waitForFailPoint","failCommand"},
                    {"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",10000}});
                cancellation.Cancel();
            }
            var read=await pending;Assert.Equal(expire?"CQL_QUERY_TIMEOUT":"CQL_QUERY_CANCELLED",read.ErrorCode);
            Assert.Null(read.Result);Assert.False(read.IsLoaded);
        }
        finally
        {
            disabled=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
            if(pending!=null)await pending;
        }
        if(expire)
        {
            // MongoDB may enforce its native time budget before a third command
            // starts. Observe the actual wire budget rather than assuming when
            // its clock expires: a fresh per-command allowance would fail this.
            var observed=budgets.ToArray();
            output.WriteLine("Native join aggregate maxTimeMS budgets: "+string.Join(",",observed));
            Assert.True(observed.Length>=2);
            Assert.All(observed,value=>Assert.InRange(value,1L,30000L));
            for(var index=1;index<observed.Length;index++)Assert.True(observed[index]<observed[index-1]);
            Assert.True(disabled["count"].ToInt64()>=configured["count"].ToInt64()+2);
        }
        var fresh=await Manager(fixture.Provider).ExecuteJoinQueryAsync(plan,new[]{fixture.Provider});
        Assert.False(fresh.IsError,fresh.Message+fresh.Exception);Assert.Single(fresh.Result.Providers.Single().Rows);
    },width:0);
}
