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
    /// <summary>S02/S03: one SaveChanges with creates, updates and deletions commits atomically or not at all.</summary>
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task SessionMixedSaveAndDeleteCommitsAsOneNativeUnitOfWork(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyMixedUnitOfWorkJourney,nativeJourneySuffix:"-mixed-unit-of-work-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyMixedUnitOfWorkJourney(
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
        Holon New(string name)
        {
            var row=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        async Task<List<Holon>> Stored(params Guid[] ids)
        {
            using var reader=CosmicSession.Create(manager,adapter,access).Result;
            var rows=await reader.Linq<Holon>(trackResults:false).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
            Assert.IsFalse(rows.IsError,rows.Message);return rows.Result.ToList();
        }
        var checks=0;
        var seedIds=new[]{Guid.NewGuid(),Guid.NewGuid()};owned.AddRange(seedIds);
        using var session=CosmicSession.Create(manager,adapter,access).Result;
        var keep=New("uow-keep");var drop=New("uow-drop");
        Assert.IsFalse(session.Add(keep).IsError);Assert.IsFalse(session.Add(drop).IsError);
        var created=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(created.IsError,created.Message);checks++;

        // S13: a cancelled token refuses the flush before dispatch and the query before reading.
        using(var cancelled=new CancellationTokenSource())
        {
            cancelled.Cancel();
            var early=New("uow-cancelled");Assert.IsFalse(session.Add(early).IsError);
            var refused=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()),cancelled.Token);
            Assert.IsTrue(refused.IsError);Assert.AreEqual("COSMIC_SESSION_CANCELLED",refused.ErrorCode);
            Assert.IsFalse(session.Remove(early).IsError,"Removing an unsaved add detaches it.");
            Assert.AreEqual(0,(await Stored(early.Id)).Count,"A cancelled flush writes nothing.");
            var query=await session.Linq<Holon>().Result.Where(row=>row.Id==keep.Id).ToCosmicListAsync(cancelled.Token);
            Assert.IsTrue(query.IsError);StringAssert.Contains(query.ErrorCode??"","CANCEL",query.Message);
            var cascade=await session.RemoveCascadeAsync(keep,cancellationToken:cancelled.Token);
            Assert.IsTrue(cascade.IsError);Assert.AreEqual(CosmicEntityState.Unchanged,session.State(keep).Result,"A cancelled cascade queues nothing.");
        }
        checks++;
        // One unit of work: update keep, delete drop, create added.
        var added=New("uow-added");
        keep.Name="uow-keep-updated";
        Assert.IsFalse(session.Update(keep).IsError);
        Assert.IsFalse(session.Remove(drop,softDelete:false).IsError);
        Assert.IsFalse(session.Add(added).IsError);
        var clock=System.Diagnostics.Stopwatch.StartNew();
        var mixed=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        timings.Add("mixed-commit",clock.ElapsedMilliseconds);
        Assert.IsFalse(mixed.IsError,mixed.ErrorCode+": "+mixed.Message);
        Assert.IsTrue(mixed.IsSaved);checks++;
        var after=await Stored(keep.Id,drop.Id,added.Id);
        CollectionAssert.AreEquivalent(new[]{keep.Id,added.Id},after.Select(row=>row.Id).ToArray());
        Assert.AreEqual("uow-keep-updated",after.Single(row=>row.Id==keep.Id).Name);checks++;

        // A stale update must abort the deletion in the same unit of work.
        using(var rival=CosmicSession.Create(manager,adapter,access).Result)
        {
            var copy=(await rival.FindAsync<Holon>(keep.Id)).Result;
            copy.Name="uow-rival";Assert.IsFalse(rival.Update(copy).IsError);
            var rivalSaved=await rival.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsFalse(rivalSaved.IsError,rivalSaved.Message);
        }
        keep.Name="uow-stale";Assert.IsFalse(session.Update(keep).IsError);
        Assert.IsFalse(session.Remove(added,softDelete:false).IsError);
        var failedCalls=0;
        Assert.IsFalse(session.SetLifecycle(new CosmicSessionLifecycle(saveFailed:(context,failed)=>{
            failedCalls++;Assert.AreEqual(CosmicConcurrencyException.Code,failed.ErrorCode);Assert.IsFalse(context.IsCommitted);
            return Task.CompletedTask;})).IsError);
        var stale=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsTrue(stale.IsError);Assert.AreEqual(CosmicConcurrencyException.Code,stale.ErrorCode,stale.Message);checks++;
        Assert.AreEqual(1,failedCalls,"S21: the failed-save callback observes the native refusal once.");checks++;
        var unchanged=await Stored(keep.Id,added.Id);
        Assert.AreEqual(2,unchanged.Count,"The deletion must roll back with the stale update.");
        Assert.AreEqual("uow-rival",unchanged.Single(row=>row.Id==keep.Id).Name);checks++;
        // Q64: async streaming page by page (page size 1) yields every row exactly once.
        using(var streamer=CosmicSession.Create(manager,adapter,access).Result)
        {
            var wanted=new[]{keep.Id,added.Id};var streamed=new List<Guid>();
            await foreach(var row in streamer.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>wanted.Contains(row.Id)).ToCosmicAsyncEnumerable(1))
                streamed.Add(row.Id);
            CollectionAssert.AreEquivalent(wanted,streamed.ToArray());Assert.AreEqual(2,streamed.Count);
        }
        checks++;
        // Q31 date arithmetic: a date field's AddX(k) compared with a closed date runs natively.
        using(var dates=CosmicSession.Create(manager,adapter,access).Result)
        {
            var now=new DateTime(2026,11,24,12,0,0,DateTimeKind.Utc);
            var stamped=New("date-stamped");stamped.MetaData=new Dictionary<string,object>{{"stamp",now}};
            Assert.IsFalse(dates.Add(stamped).IsError);
            Assert.IsFalse((await dates.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            var wanted=new[]{stamped.Id};
            async Task<int> Count(System.Linq.Expressions.Expression<Func<Holon,bool>> filter)
            {
                var rows=await dates.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>wanted.Contains(row.Id)).Where(filter).ToCosmicListAsync();
                Assert.IsFalse(rows.IsError,rows.ErrorCode+": "+rows.Message);return rows.Result.Count;
            }
            Assert.AreEqual(1,await Count(row=>CosmicQueryMetadata.Value<DateTime>(row,"stamp").AddDays(1)>now));
            Assert.AreEqual(0,await Count(row=>CosmicQueryMetadata.Value<DateTime>(row,"stamp").AddDays(-1)>now));
            Assert.AreEqual(1,await Count(row=>CosmicQueryMetadata.Value<DateTime>(row,"stamp").AddHours(-48)<now));
            var refused=false;
            try
            {
                var months=await dates.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>CosmicQueryMetadata.Value<DateTime>(row,"stamp").AddMonths(1)>now).ToCosmicListAsync();
                refused=months.IsError;
            }
            catch(CosmicLinqException){refused=true;}
            Assert.IsTrue(refused,"Month arithmetic is not linear and is refused.");
        }
        checks++;
        // S06 notification tracking: only raised PropertyChanged marks a notifying entity Modified.
        using(var notifying=CosmicSession.Create(manager,adapter,access).Result)
        {
            Assert.IsFalse(notifying.SetChangeTrackingStrategy(CosmicChangeTrackingStrategy.ChangedNotifications).IsError);
            var watched=new Holon{Id=Guid.NewGuid(),Name="notify-row",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(watched.Id);Assert.IsFalse(notifying.Add(watched).IsError);
            Assert.IsFalse((await notifying.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            watched.MetaData??=new Dictionary<string,object>();watched.MetaData["silent"]="edit";
            Assert.AreEqual(CosmicEntityState.Unchanged,notifying.State(watched).Result,"No notification, no change.");
            watched.NotifyPropertyChanged(nameof(Holon.MetaData));
            Assert.AreEqual(CosmicEntityState.Modified,notifying.State(watched).Result);
            var notifiedSave=await notifying.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
            Assert.IsFalse(notifiedSave.IsError,notifiedSave.ErrorCode+": "+notifiedSave.Message);
            Assert.AreEqual(CosmicEntityState.Unchanged,notifying.State(watched).Result);
            Assert.AreEqual("edit",(await Stored(watched.Id)).Single().MetaData["silent"]?.ToString());
        }
        checks++;
        // M15 collection membership: a metadata array contains a string element; scalars never match.
        using(var tagged=CosmicSession.Create(manager,adapter,access).Result)
        {
            var listKey="tags"+Guid.NewGuid().ToString("N");
            var withRed=New("tags-red");withRed.MetaData=new Dictionary<string,object>{{listKey,new List<string>{"red","blue"}}};
            var withoutRed=New("tags-green");withoutRed.MetaData=new Dictionary<string,object>{{listKey,new List<string>{"green"}}};
            var scalarRed=New("tags-scalar");scalarRed.MetaData=new Dictionary<string,object>{{listKey,"red"}};
            foreach(var row in new[]{withRed,withoutRed,scalarRed})Assert.IsFalse(tagged.Add(row).IsError);
            var taggedSave=await tagged.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));Assert.IsFalse(taggedSave.IsError,taggedSave.Message);
            var three=new[]{withRed.Id,withoutRed.Id,scalarRed.Id};
            var red=await tagged.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>three.Contains(row.Id))
                .Where(row=>CosmicQueryMetadata.Contains(row,"red",listKey)).ToCosmicListAsync();
            Assert.IsFalse(red.IsError,red.ErrorCode+": "+red.Message);
            CollectionAssert.AreEqual(new[]{withRed.Id},red.Result.Select(row=>row.Id).ToArray());
        }
        checks++;
        // Q09 string transforms: ordinal Replace and Trim(char) compared with a literal, natively.
        using(var strings=CosmicSession.Create(manager,adapter,access).Result)
        {
            var dashed=New("code-dashed");dashed.MetaData=new Dictionary<string,object>{{"code","ab-12-cd"}};
            var starred=New("code-starred");starred.MetaData=new Dictionary<string,object>{{"code","**x**"}};
            foreach(var row in new[]{dashed,starred})Assert.IsFalse(strings.Add(row).IsError);
            Assert.IsFalse((await strings.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            var both=new[]{dashed.Id,starred.Id};
            async Task<Guid[]> Ids(System.Linq.Expressions.Expression<Func<Holon,bool>> filter)
            {
                var rows=await strings.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>both.Contains(row.Id)).Where(filter).ToCosmicListAsync();
                Assert.IsFalse(rows.IsError,rows.ErrorCode+": "+rows.Message);return rows.Result.Select(row=>row.Id).ToArray();
            }
            CollectionAssert.AreEqual(new[]{dashed.Id},await Ids(row=>CosmicQueryMetadata.Value<string>(row,"code").Replace("-","")=="ab12cd"));
            CollectionAssert.AreEqual(new[]{starred.Id},await Ids(row=>CosmicQueryMetadata.Value<string>(row,"code").Trim('*')=="x"));
            CollectionAssert.AreEqual(new[]{starred.Id},await Ids(row=>CosmicQueryMetadata.Value<string>(row,"code").Replace("-","")!="ab12cd"));
        }
        checks++;
        // Q65 compiled query: built once per argument and actor policy, then reused.
        using(var compiledSession=CosmicSession.Create(manager,adapter,access).Result)
        {
            var byName=new CosmicCompiledQuery<string,Holon>((root,name)=>root.Where(row=>row.Name==name));
            var first=await byName.ExecuteAsync(compiledSession,"uow-added",trackingMode:CosmicQueryTrackingMode.NoTracking);
            Assert.IsFalse(first.IsError,first.ErrorCode+": "+first.Message);
            var again=await byName.ExecuteAsync(compiledSession,"uow-added",trackingMode:CosmicQueryTrackingMode.NoTracking);
            var other=await byName.ExecuteAsync(compiledSession,"uow-keep-updated",trackingMode:CosmicQueryTrackingMode.NoTracking);
            Assert.IsTrue(first.Result.Any(row=>row.Id==added.Id));
            CollectionAssert.AreEquivalent(first.Result.Select(row=>row.Id).ToArray(),again.Result.Select(row=>row.Id).ToArray());
            Assert.IsFalse(other.Result.Any(row=>row.Id==added.Id));
            Assert.AreEqual(1L,byName.CacheHits);Assert.AreEqual(2L,byName.CacheMisses);
        }
        checks++;
        // O15 as-of reads: archived versions are read natively; history needs version control on.
        var versioned=(IOASISDBStorageProvider)provider;var hadHistory=versioned.IsVersionControlEnabled;
        versioned.IsVersionControlEnabled=true;
        try
        {
            using var history=CosmicSession.Create(manager,adapter,access).Result;
            var aged=New("asof-v1");Assert.IsFalse(history.Add(aged).IsError);
            Assert.IsFalse((await history.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            await Task.Delay(1100);var betweenOneAndTwo=DateTime.UtcNow;await Task.Delay(1100);
            aged.Name="asof-v2";Assert.IsFalse(history.Update(aged).IsError);
            var second=await history.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));Assert.IsFalse(second.IsError,second.ErrorCode+": "+second.Message);
            await Task.Delay(1100);var afterTwo=DateTime.UtcNow;
            using var reader2=CosmicSession.Create(manager,adapter,access).Result;
            var asOfOne=await reader2.FindAsOfAsync<Holon>(aged.Id,betweenOneAndTwo);
            Assert.IsFalse(asOfOne.IsError,asOfOne.ErrorCode+": "+asOfOne.Message);Assert.AreEqual("asof-v1",asOfOne.Result.Name);
            Assert.AreEqual("asof-v2",(await reader2.FindAsOfAsync<Holon>(aged.Id,afterTwo)).Result.Name);
        }
        finally { versioned.IsVersionControlEnabled=hadHistory; }
        checks++;
        // A15 set collections: a HashSet<T> owned collection round-trips as a set and is queryable natively.
        {
            var setBuilder=new CosmicModelBuilder();
            setBuilder.Entity<SetModelHolon>().OwnsMany(row=>row.Labels).HasMetadataKey("labels");
            var setModel=setBuilder.Build();Assert.IsFalse(setModel.IsError,setModel.Message);
            var withSet=new SetModelHolon{Id=Guid.NewGuid(),Name="set-holon",Labels=new HashSet<string>{"a","b","a"},
                CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(withSet.Id);
            using(var setWriter=CosmicSession.Create(manager,adapter,access,setModel.Result).Result)
            {
                Assert.IsFalse(setWriter.Add(withSet).IsError);
                var setSaved=await setWriter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
                Assert.IsFalse(setSaved.IsError,setSaved.ErrorCode+": "+setSaved.Message);
            }
            using var setReader=CosmicSession.Create(manager,adapter,access,setModel.Result).Result;
            var loadedSet=(await setReader.FindAsync<SetModelHolon>(withSet.Id)).Result;
            Assert.IsNotNull(loadedSet);Assert.IsTrue(loadedSet.Labels.SetEquals(new[]{"a","b"}));
            var setId=withSet.Id;
            var hasB=await setReader.Linq<SetModelHolon>(CosmicQueryTrackingMode.NoTracking).Result
                .Where(row=>row.Id==setId).Where(row=>CosmicQueryMetadata.Contains(row,"b","labels")).ToCosmicListAsync();
            Assert.IsFalse(hasB.IsError,hasB.ErrorCode+": "+hasB.Message);Assert.AreEqual(1,hasB.Result.Count);
        }
        checks++;
        // M24 default and computed values are applied at SaveChanges and stored natively.
        using(var gen=CosmicSession.Create(manager,adapter,access).Result)
        {
            Assert.IsFalse(gen.HasDefaultValue<Holon,string>(row=>row.Description,"default-description").IsError);
            Assert.IsFalse(gen.HasComputedValue<Holon,string>(row=>row.CustomKey,row=>(row.Name??"").ToUpperInvariant()).IsError);
            var plain=New("gen-plain");var given=New("gen-given");given.Description="explicit";
            foreach(var row in new[]{plain,given})Assert.IsFalse(gen.Add(row).IsError);
            var genSaved=await gen.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));Assert.IsFalse(genSaved.IsError,genSaved.ErrorCode+": "+genSaved.Message);
            var storedPlain=(await Stored(plain.Id)).Single();var storedGiven=(await Stored(given.Id)).Single();
            Assert.AreEqual("default-description",storedPlain.Description);Assert.AreEqual("explicit",storedGiven.Description);
            Assert.AreEqual("GEN-PLAIN",storedPlain.CustomKey);
            plain.Name="gen-renamed";Assert.IsFalse(gen.Update(plain).IsError);
            Assert.IsFalse((await gen.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            Assert.AreEqual("GEN-RENAMED",(await Stored(plain.Id)).Single().CustomKey,"Computed values follow every save.");
        }
        checks++;
        // M23 read-only views: named query, rows never tracked, so never saved.
        using(var viewSession=CosmicSession.Create(manager,adapter,access).Result)
        {
            var viewId=keep.Id;
            Assert.IsFalse(viewSession.HasView<Holon>("kept",root=>root.Where(row=>row.Id==viewId)).IsError);
            var viewRows=await viewSession.QueryViewAsync<Holon>("kept");
            Assert.IsFalse(viewRows.IsError,viewRows.ErrorCode+": "+viewRows.Message);
            Assert.AreEqual(1,viewRows.Result.Count);
            Assert.AreEqual(CosmicEntityState.Detached,viewSession.State(viewRows.Result[0]).Result,"View rows are not tracked.");
            Assert.AreEqual(0,viewSession.Statistics.EntityCount);
            Assert.IsTrue((await viewSession.QueryViewAsync<Holon>("missing")).IsError);
        }
        checks++;
        // S07 graph merge: a disconnected parent with an edited child and a new child merges atomically.
        {
            var parent=New("graph-parent");var child=New("graph-child");
            using(var setup=CosmicSession.Create(manager,adapter,access).Result)
            {
                Assert.IsFalse(setup.Add(parent).IsError);
                Assert.IsFalse((await setup.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
                child.ParentHolonId=parent.Id;Assert.IsFalse(setup.Add(child).IsError);
                Assert.IsFalse((await setup.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            }
            var fresh=New("graph-new-child");
            var detached=new Holon{Id=parent.Id,Name="graph-parent-edited",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,
                Children=new List<IHolon>{
                    new Holon{Id=child.Id,Name="graph-child-edited",ParentHolonId=parent.Id,CreatedByAvatarId=actor,IsActive=true,IsPublic=false},
                    fresh}};
            using(var merger=CosmicSession.Create(manager,adapter,access).Result)
            {
                var mergedGraph=await merger.MergeGraphAsync(detached);
                Assert.IsFalse(mergedGraph.IsError,mergedGraph.ErrorCode+": "+mergedGraph.Message);Assert.AreEqual(3,mergedGraph.Result);
                var graphSaved=await merger.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
                Assert.IsFalse(graphSaved.IsError,graphSaved.ErrorCode+": "+graphSaved.Message);
            }
            var graph=await Stored(parent.Id,child.Id,fresh.Id);
            Assert.AreEqual("graph-parent-edited",graph.Single(row=>row.Id==parent.Id).Name);
            Assert.AreEqual("graph-child-edited",graph.Single(row=>row.Id==child.Id).Name);
            Assert.AreEqual(parent.Id,graph.Single(row=>row.Id==fresh.Id).ParentHolonId);
        }
        checks++;
        // Q55 multi-branch text CASE: one string path, literal WHEN branches and ELSE, evaluated natively.
        using(var cases=CosmicSession.Create(manager,adapter,access).Result)
        {
            var gold=New("tier-gold");gold.MetaData=new Dictionary<string,object>{{"tier","g"}};
            var silver=New("tier-silver");silver.MetaData=new Dictionary<string,object>{{"tier","s"}};
            var other=New("tier-other");other.MetaData=new Dictionary<string,object>{{"tier","x"}};
            foreach(var row in new[]{gold,silver,other})Assert.IsFalse(cases.Add(row).IsError);
            Assert.IsFalse((await cases.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
            var tiers=new[]{gold.Id,silver.Id,other.Id};
            var labelled=await cases.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result.Where(row=>tiers.Contains(row.Id))
                .Select(row=>new{row.Name,Label=CosmicQueryMetadata.Value<string>(row,"tier")=="g"?"Gold"
                    :CosmicQueryMetadata.Value<string>(row,"tier")=="s"?"Silver":"Standard"}).ToCosmicListAsync();
            Assert.IsFalse(labelled.IsError,labelled.ErrorCode+": "+labelled.Message);
            var byName=labelled.Result.ToDictionary(row=>row.Name,row=>row.Label);
            Assert.AreEqual("Gold",byName["tier-gold"]);Assert.AreEqual("Silver",byName["tier-silver"]);Assert.AreEqual("Standard",byName["tier-other"]);
            Assert.ThrowsException<CosmicLinqException>(()=>cases.Linq<Holon>(CosmicQueryTrackingMode.NoTracking).Result
                .Select(row=>row.Name=="a"?"A":row.Description=="b"?"B":"C"),"Branches over different paths are refused.");
        }
        checks++;
        // M29 seed data: idempotent insert of fixed-identity rows in one unit of work.
        Holon[] Seed()=>new[]{
            new Holon{Id=seedIds[0],Name="seed-one",CreatedByAvatarId=actor,IsActive=true,IsPublic=false},
            new Holon{Id=seedIds[1],Name="seed-two",CreatedByAvatarId=actor,IsActive=true,IsPublic=false}};
        using(var seeder=CosmicSession.Create(manager,adapter,access).Result)
        {
            var seeded=await seeder.EnsureSeedDataAsync(Seed(),Guid.NewGuid());
            Assert.IsFalse(seeded.IsError,seeded.ErrorCode+": "+seeded.Message);Assert.AreEqual(2,seeded.Result);
        }
        using(var reseeder=CosmicSession.Create(manager,adapter,access).Result)
        {
            var again=await reseeder.EnsureSeedDataAsync(Seed(),Guid.NewGuid());
            Assert.IsFalse(again.IsError,again.Message);Assert.AreEqual(0,again.Result,"Seeding is idempotent.");
        }
        Assert.AreEqual(2,(await Stored(seedIds)).Count);checks++;
        return new NativeFixtureJourneyProof(checks,4,new Dictionary<string,bool>{
            ["mixedCommitVerified"]=true,["staleUpdateAbortsDeletionVerified"]=true,["saveFailedCallbackVerified"]=true,["cancellationVerified"]=true,["asyncStreamingVerified"]=true,["seedDataVerified"]=true,["dateArithmeticVerified"]=true,["notificationTrackingVerified"]=true,["collectionMembershipVerified"]=true,["stringTransformsVerified"]=true,["compiledQueryVerified"]=true,["asOfHistoryVerified"]=true,["setCollectionVerified"]=true,["generatedValuesVerified"]=true,["readOnlyViewsVerified"]=true,["graphMergeVerified"]=true,["multiBranchCaseVerified"]=true});
    }

    public sealed class SetModelHolon : Holon
    {
        public HashSet<string> Labels { get; set; } = new();
    }
}
