using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    // A13: split-query and join-fetch strategies for roots plus their direct children.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeJoinFetchMatchesSplitInOneNativeCommand(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyJoinFetchJourney,nativeJourneySuffix:"-join-fetch-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyJoinFetchJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        Assert.IsInstanceOfType(adapter,typeof(ICosmicJoinFetchStorageProvider));
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var foreignActor=Guid.NewGuid();
        var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name,Guid owner,Guid parent)
        {
            var row=new Holon{Id=Guid.NewGuid(),Name=name,ParentHolonId=parent,CreatedByAvatarId=owner,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        var checks=0;var writes=0;

        // Seed: three roots (2, 1 and 0 visible children) and one private child owned by another actor.
        using(var writer=CosmicSession.Create(manager,adapter,access).Result)
        {
            var seededRoots=new[]{New("jf root a",actor,Guid.Empty),New("jf root b",actor,Guid.Empty),New("jf root c",actor,Guid.Empty)};
            foreach(var root in seededRoots)Assert.IsFalse(writer.Add(root).IsError);
            Saved(await Timed("seed-roots",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));writes++;
            foreach(var child in new[]{New("jf child a1",actor,seededRoots[0].Id),New("jf child a2",actor,seededRoots[0].Id),New("jf child b1",actor,seededRoots[1].Id)})
                Assert.IsFalse(writer.Add(child).IsError);
            Saved(await Timed("seed-children",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));writes++;
        }
        var rootIds=owned.Take(3).ToArray();
        var hidden=New("jf private foreign child",foreignActor,rootIds[0]);
        using(var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(foreignActor).Result).Result)
        {
            Assert.IsFalse(foreign.Add(hidden).IsError);
            Saved(await Timed("seed-hidden-child",()=>foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));writes++;
            var exists=await foreign.ReadAsync(foreign.Query<Holon>().Result.Where(row=>row.Id==hidden.Id).Result,trackResults:false);
            Assert.IsFalse(exists.IsError,exists.Message);Assert.AreEqual(1,exists.Result.Count,"The hidden child is real; only access hides it.");
        }
        CosmicQuery<Holon> Roots(CosmicSession session)
            =>session.Query<Holon>().Result.Where(row=>rootIds.Contains(row.Id)).Result.OrderByDescending(row=>row.Name).Result;

        // 1. Explicit split switch: one root query plus one batched child query.
        using var split=CosmicSession.Create(manager,adapter,access).Result;
        var splitBefore=split.Statistics.QueryCount;
        var splitRead=await Timed("split-fetch",()=>split.ReadWithChildrenAsync<Holon,Holon>(Roots(split),CosmicFetchStrategy.Split,10,10));
        Assert.IsFalse(splitRead.IsError,splitRead.Message+splitRead.Exception);
        Assert.AreEqual(CosmicFetchStrategy.Split,splitRead.Result.Strategy);
        Assert.AreEqual(2,splitRead.Result.NativeQueries);Assert.AreEqual(2,split.Statistics.QueryCount-splitBefore);
        Assert.IsNull(splitRead.Result.NativeCommand);
        checks++;

        // 2. Join fetch: exactly one native command, counted by the provider at its native call site and by the session.
        using var join=CosmicSession.Create(manager,adapter,access).Result;
        var joinBefore=join.Statistics.QueryCount;
        var joinRead=await Timed("join-fetch",()=>join.ReadWithChildrenAsync<Holon,Holon>(Roots(join),CosmicFetchStrategy.Join,10,10));
        Assert.IsFalse(joinRead.IsError,joinRead.Message+joinRead.Exception);
        Assert.AreEqual(CosmicFetchStrategy.Join,joinRead.Result.Strategy);
        Assert.AreEqual(1,joinRead.Result.NativeQueries,"The provider reported its native command count.");
        Assert.AreEqual(1,join.Statistics.QueryCount-joinBefore,"The session issued one native read.");
        var command=joinRead.Result.NativeCommand;Assert.IsNotNull(command);
        switch(provider)
        {
            case NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS:
                StringAssert.Contains(command,"$lookup");StringAssert.Contains(command,"CosmicJoinFetchChildren");break;
            case NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS:
                StringAssert.Contains(command,"JOIN Holon ON");StringAssert.Contains(command,"WITH \"JoinFetchRoots\"");
                Assert.AreEqual(0,command.Count(character=>character==';'),"One SQL statement.");break;
            default:
                StringAssert.Contains(command,"OPTIONAL MATCH");StringAssert.Contains(command,"collect(");break;
        }
        checks++;

        // 3. Join fetch equals split: same roots in the same order, the same children with the same stored values.
        string Shape(IHolon row)=>string.Join("|",row.Id,row.Name,row.ParentHolonId,row.CreatedByAvatarId,row.IsPublic,row.IsActive,row.Version,
            row.VersionId,row.CreatedDate.ToUniversalTime().Ticks,row.ModifiedDate.ToUniversalTime().Ticks,
            CosmicConcurrency.GetProviderRevisionId(row,adapter.QueryProviderType));
        CollectionAssert.AreEqual(splitRead.Result.Roots.Select(Shape).ToArray(),joinRead.Result.Roots.Select(Shape).ToArray());
        CollectionAssert.AreEqual(new[]{"jf root c","jf root b","jf root a"},joinRead.Result.Roots.Select(row=>row.Name).ToArray());
        Assert.AreEqual(3,joinRead.Result.ChildCount);Assert.AreEqual(3,splitRead.Result.ChildCount);
        for(var index=0;index<3;index++)
            CollectionAssert.AreEquivalent(splitRead.Result.Roots[index].Children.Select(Shape).ToArray(),
                joinRead.Result.Roots[index].Children.Select(Shape).ToArray());
        checks++;

        // 4. Children are fixed up on the tracked roots with tracked child instances.
        var rootA=joinRead.Result.Roots.Single(row=>row.Name=="jf root a");
        Assert.AreEqual(2,rootA.Children.Count);Assert.AreEqual(1,joinRead.Result.Roots.Single(row=>row.Name=="jf root b").Children.Count);
        Assert.AreEqual(0,joinRead.Result.Roots.Single(row=>row.Name=="jf root c").Children.Count);
        foreach(var root in joinRead.Result.Roots)
        {
            Assert.AreEqual(CosmicEntityState.Unchanged,join.State(root).Result);
            foreach(var child in root.Children)
            {Assert.AreEqual(root.Id,child.ParentHolonId);Assert.AreEqual(CosmicEntityState.Unchanged,join.State(child).Result);}
        }
        Assert.AreEqual(6,join.Statistics.EntityCount);Assert.AreEqual(3,join.Statistics.LoadedCollectionCount);
        checks++;

        // 5. The identity map is shared with rows the session already tracks, and pending edits survive.
        using var tracked=CosmicSession.Create(manager,adapter,access).Result;
        var childA1Id=rootA.Children.Single(row=>row.Name=="jf child a1").Id;
        var rootBId=joinRead.Result.Roots.Single(row=>row.Name=="jf root b").Id;
        var prior=await tracked.ReadAsync(tracked.Query<Holon>().Result.Where(row=>row.Id==childA1Id || row.Id==rootBId).Result);
        Assert.IsFalse(prior.IsError,prior.Message);Assert.AreEqual(2,prior.Result.Count);
        var priorChild=prior.Result.Single(row=>row.Id==childA1Id);var priorRoot=prior.Result.Single(row=>row.Id==rootBId);
        priorChild.Name="pending a1 edit";
        var shared=await Timed("join-fetch-identity",()=>tracked.ReadWithChildrenAsync<Holon,Holon>(Roots(tracked),CosmicFetchStrategy.Join,10,10));
        Assert.IsFalse(shared.IsError,shared.Message+shared.Exception);
        Assert.AreSame(priorRoot,shared.Result.Roots.Single(row=>row.Id==rootBId));
        var sharedA=shared.Result.Roots.Single(row=>row.Id==rootA.Id);
        Assert.AreSame(priorChild,sharedA.Children.Single(row=>row.Id==childA1Id));
        Assert.AreEqual("pending a1 edit",priorChild.Name);
        Assert.AreEqual(CosmicEntityState.Modified,tracked.State(priorChild).Result);
        checks++;

        // 6. A private child owned by another actor never appears, in either strategy.
        foreach(var read in new[]{splitRead.Result,joinRead.Result,shared.Result})
            Assert.IsFalse(read.Roots.SelectMany(row=>row.Children).Any(row=>row.Id==hidden.Id));
        Assert.AreEqual(CosmicEntityState.Detached,join.State(hidden).Result);
        checks++;

        // 7. Bounds: exceeded root or child bounds are refused explicitly and nothing is tracked.
        using(var bounded=CosmicSession.Create(manager,adapter,access).Result)
        {
            var children=await Timed("join-child-bound",()=>bounded.ReadWithChildrenAsync<Holon,Holon>(Roots(bounded),CosmicFetchStrategy.Join,10,2));
            Assert.IsTrue(children.IsError);Assert.AreEqual("COSMIC_SESSION_COLLECTION_LIMIT",children.ErrorCode);
            var roots=await Timed("join-root-bound",()=>bounded.ReadWithChildrenAsync<Holon,Holon>(Roots(bounded),CosmicFetchStrategy.Join,2,10));
            Assert.IsTrue(roots.IsError);Assert.AreEqual("COSMIC_FETCH_ROOT_LIMIT",roots.ErrorCode);
            Assert.AreEqual(0,bounded.Statistics.EntityCount);
            var splitRoots=await bounded.ReadWithChildrenAsync<Holon,Holon>(Roots(bounded),CosmicFetchStrategy.Split,2,10);
            Assert.IsTrue(splitRoots.IsError);Assert.AreEqual("COSMIC_FETCH_ROOT_LIMIT",splitRoots.ErrorCode);
            Assert.AreEqual(0,bounded.Statistics.EntityCount);
            var invalid=await bounded.ReadWithChildrenAsync<Holon,Holon>(Roots(bounded),CosmicFetchStrategy.Join,10,10000);
            Assert.IsTrue(invalid.IsError);Assert.AreEqual("COSMIC_SESSION_COLLECTION_LIMIT",invalid.ErrorCode);
        }
        checks++;

        // 8. Unsupported combinations are refused, never silently split.
        using(var refused=CosmicSession.Create(manager,adapter,access).Result)
        {
            var before=refused.Statistics.QueryCount;
            var graph=Roots(refused).WithChildren(CosmicQueryGraphOptions.Create().Result).Result;
            var withGraph=await refused.ReadWithChildrenAsync<Holon,Holon>(graph,CosmicFetchStrategy.Join,10,10);
            Assert.IsTrue(withGraph.IsError);Assert.AreEqual("COSMIC_FETCH_UNSUPPORTED",withGraph.ErrorCode);
            Assert.AreEqual(before,refused.Statistics.QueryCount,"A refused join issues no native read.");
            var relational=Roots(refused).WhereAnyChild(child=>child.Name=="jf child b1").Result;
            var withRelationship=await refused.ReadWithChildrenAsync<Holon,Holon>(relational,CosmicFetchStrategy.Join,10,10);
            Assert.IsTrue(withRelationship.IsError);Assert.AreEqual("COSMIC_FETCH_UNSUPPORTED",withRelationship.ErrorCode);
            var undefined=await refused.ReadWithChildrenAsync<Holon,Holon>(Roots(refused),(CosmicFetchStrategy)0,10,10);
            Assert.IsTrue(undefined.IsError);Assert.AreEqual("COSMIC_FETCH_STRATEGY_INVALID",undefined.ErrorCode);
            Assert.AreEqual(0,refused.Statistics.EntityCount);
        }
        checks++;

        return new NativeFixtureJourneyProof(checks,writes,new Dictionary<string,bool>{
            ["explicitSplitSwitchVerified"]=true,["joinFetchOneNativeCommand"]=true,["joinEqualsSplit"]=true,
            ["childrenFixedUp"]=true,["identityMapShared"]=true,["hiddenChildNeverAppears"]=true,
            ["boundsRefused"]=true,["unsupportedCombinationRefused"]=true});
    }
}
