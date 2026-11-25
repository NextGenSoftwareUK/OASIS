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
            ["mixedCommitVerified"]=true,["staleUpdateAbortsDeletionVerified"]=true,["saveFailedCallbackVerified"]=true,["cancellationVerified"]=true,["asyncStreamingVerified"]=true,["seedDataVerified"]=true,["dateArithmeticVerified"]=true,["notificationTrackingVerified"]=true,["collectionMembershipVerified"]=true,["stringTransformsVerified"]=true,["compiledQueryVerified"]=true});
    }
}
