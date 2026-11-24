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
    public sealed class FluentJoinHolon:Holon {public int Score {get;set;}}

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeFluentModelsBindIndependentJoinSources(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyFluentJoinJourney,nativeJourneySuffix:"-fluent-join-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyFluentJoinJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var builder=new CosmicModelBuilder();
        builder.Entity<FluentJoinHolon>().Property(row=>row.Score).HasMetadataKey("left_score");
        var leftModel=builder.Build();Assert.IsFalse(leftModel.IsError,leftModel.Message);
        builder.Entity<FluentJoinHolon>().Property(row=>row.Score).HasMetadataKey("right_score");
        var rightModel=builder.Build();Assert.IsFalse(rightModel.IsError,rightModel.Message);
        using var leftWriter=CosmicSession.Create(manager,adapter,access,leftModel.Result).Result;
        using var rightWriter=CosmicSession.Create(manager,adapter,access,rightModel.Result).Result;
        FluentJoinHolon New(string name)=>new(){Id=Guid.NewGuid(),Name=name,Score=11,
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var a=New("left");var b=New("right");owned.AddRange(new[]{a.Id,b.Id});
        Assert.IsFalse(leftWriter.Add(a).IsError);Assert.IsFalse(rightWriter.Add(b).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var saveA=await Timed("create-left",()=>leftWriter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        var saveB=await Timed("create-right",()=>rightWriter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saveA.IsError,saveA.Message+saveA.Exception);Assert.IsTrue(saveA.IsSaved);
        Assert.IsFalse(saveB.IsError,saveB.Message+saveB.Exception);Assert.IsTrue(saveB.IsSaved);
        Assert.IsTrue(a.MetaData.ContainsKey("left_score"));Assert.IsFalse(a.MetaData.ContainsKey("right_score"));
        Assert.IsTrue(b.MetaData.ContainsKey("right_score"));Assert.IsFalse(b.MetaData.ContainsKey("left_score"));
        var leftQuery=leftWriter.Query<FluentJoinHolon>().Result.Where(row=>row.Id==a.Id).Result;
        var rightQuery=rightWriter.Query<FluentJoinHolon>().Result.Where(row=>row.Id==b.Id).Result;
        var left=CosmicLinq.Create(manager,adapter,leftQuery).Result;
        var right=CosmicLinq.Create(manager,adapter,rightQuery).Result;
        var projected=await Timed("scalar-join",()=>left.Join(right,l=>l.Score,r=>r.Score,
            (l,r)=>new{LeftScore=l.Score,RightScore=r.Score}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);Assert.AreEqual(1,projected.Result.Count);
        Assert.AreEqual(11,projected.Result[0].LeftScore);Assert.AreEqual(11,projected.Result[0].RightScore);
        var entity=await Timed("typed-right-join",()=>left.Join(right,l=>l.Score,r=>r.Score,(l,r)=>r).ToCosmicListAsync());
        Assert.IsFalse(entity.IsError,entity.Message+entity.Exception);Assert.AreEqual(1,entity.Result.Count);
        Assert.AreEqual(b.Id,entity.Result[0].Id);Assert.AreEqual(11,entity.Result[0].Score);
        var grouped=await Timed("typed-groupjoin",()=>left.GroupJoin(right,l=>l.Score,r=>r.Score,
            (l,matches)=>new{l.Score,Left=l,Matches=matches,Count=matches.Count()}).ToCosmicListAsync());
        Assert.IsFalse(grouped.IsError,grouped.Message+grouped.Exception);Assert.AreEqual(1,grouped.Result.Count);
        Assert.AreEqual(11,grouped.Result[0].Score);Assert.AreEqual(a.Id,grouped.Result[0].Left.Id);
        Assert.AreEqual(1,grouped.Result[0].Count);Assert.AreEqual(b.Id,grouped.Result[0].Matches.Single().Id);
        Assert.AreEqual(11,grouped.Result[0].Matches.Single().Score);
        var empty=CosmicLinq.Create(manager,adapter,rightQuery.Where(row=>row.Score==999).Result).Result;
        var unmatched=await Timed("unmatched-groupjoin",()=>left.GroupJoin(empty,l=>l.Score,r=>r.Score,
            (l,matches)=>new{l.Score,Count=matches.Count()}).ToCosmicListAsync());
        Assert.IsFalse(unmatched.IsError,unmatched.Message+unmatched.Exception);Assert.AreEqual(1,unmatched.Result.Count);
        Assert.AreEqual(11,unmatched.Result[0].Score);Assert.AreEqual(0,unmatched.Result[0].Count);
        var flattened=await Timed("correlated-selectmany",()=>(from l in left
            from r in right.Where(r=>r.Score==l.Score)
            select new{LeftScore=l.Score,RightScore=r.Score}).ToCosmicListAsync());
        Assert.IsFalse(flattened.IsError,flattened.Message+flattened.Exception);Assert.AreEqual(1,flattened.Result.Count);
        Assert.AreEqual(11,flattened.Result[0].LeftScore);Assert.AreEqual(11,flattened.Result[0].RightScore);
        return new NativeFixtureJourneyProof(5,2,new Dictionary<string,bool>{
            ["independentSourceAliases"]=true,["scalarJoinProjection"]=true,
            ["typedJoinAndGroupJoin"]=true,["unmatchedGroupPreserved"]=true,["correlatedSelectMany"]=true});
    }
}
