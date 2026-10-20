using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

public partial class PostgreSqlNativeAcceptanceTests
{
    private sealed class ComponentGraphOwner:Holon
    {
        public List<Holon> Components{get;set;}=new();
        public override List<IHolon> AllChildren=>base.AllChildren.Concat(Components).ToList();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }

    private sealed class GraphFixture:IAsyncDisposable
    {
        public readonly PostgreSQLOASIS Provider;
        public readonly NpgsqlConnection Connection;
        public readonly string Marker="owned-pg-graph-"+Guid.NewGuid().ToString("N");
        private readonly string _journal;
        private readonly Guid[] _ids;
        private readonly Dictionary<Guid,string> _observed=new();
        private bool _begun,_safe;
        public GraphFixture(string name,Guid[] ids){
            var text=Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING")!;
            var settings=new NpgsqlConnectionStringBuilder(text);Assert.AreEqual("127.0.0.1",settings.Host);Assert.AreEqual(15432,settings.Port);Assert.AreEqual("oasis_provider_test",settings.Database);
            _ids=ids;_journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_POSTGRES_TEST_WORK")!,name+".pending.json");
            Provider=new PostgreSQLOASIS(text){IsVersionControlEnabled=true};Connection=new NpgsqlConnection(text);
        }
        public async Task Begin(){
            Assert.IsFalse(File.Exists(_journal));await Connection.OpenAsync();Assert.IsFalse((await Provider.ActivateProviderAsync()).IsError);
            foreach(var id in _ids)Assert.IsNull(await Body(id));
            using var file=new FileStream(_journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=_ids,marker=Marker,table="oasis_holons",state="before-native-graph"}));file.Flush(true);
            _begun=true;
        }
        public void BeforeMutation()=>_safe=false;
        public async Task<string?> Body(Guid id){await using var read=new NpgsqlCommand("SELECT data_json FROM oasis_holons WHERE id=@id",Connection);read.Parameters.AddWithValue("id",id);return (string?)await read.ExecuteScalarAsync();}
        public async Task Observe(){
            foreach(var id in _ids){var body=await Body(id);if(body==null)continue;using var json=JsonDocument.Parse(body);Assert.AreEqual(id,json.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(Marker,json.RootElement.GetProperty("MetaData").GetProperty("owned-fixture").GetString());_observed[id]=body;}
            _safe=true;
        }
        public async ValueTask DisposeAsync(){
            // Read-back does not authorize deletion of an uncertain mutation. Only the
            // exact verified snapshots captured by successful/definite-outcome test code do.
            if(_begun&&_safe){
                foreach(var row in _observed){await using var cleanup=new NpgsqlCommand("DELETE FROM oasis_holons WHERE id=@id AND data_json=@observed",Connection);cleanup.Parameters.AddWithValue("id",row.Key);cleanup.Parameters.AddWithValue("observed",row.Value);Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}
                var absent=true;foreach(var id in _ids)absent&=await Body(id)==null;
                if(absent)File.Delete(_journal);
            }
            await Provider.DeActivateProviderAsync();await Connection.DisposeAsync();
        }
    }

    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)][DataRow(3,false)][DataRow(4,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(3,true)][DataRow(4,true)]
    public async Task NativeGraphSelectionPersistsOnlyPlannedNodes(int scenario,bool synchronous)
    {
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};
        await using var fixture=new GraphFixture($"native-graph-{scenario}-{synchronous}",ids);await fixture.Begin();
        var grandchild=new Holon{Id=ids[2],Name="grandchild",MetaData=new(){["owned-fixture"]=fixture.Marker}};
        var child=new Holon{Id=ids[1],Name="child",Children=new List<IHolon>{grandchild},MetaData=new(){["owned-fixture"]=fixture.Marker}};
        var root=new Holon{Id=ids[0],Name="root",Children=new List<IHolon>{child},MetaData=new(){["owned-fixture"]=fixture.Marker}};
        var children=scenario!=0;var recursive=scenario!=1;var depth=scenario is 2 or 4?1:0;var initial=scenario==4?1:0;
        fixture.BeforeMutation();var result=synchronous?fixture.Provider.SaveHolons(new[]{root},children,recursive,depth,initial,continueOnError:false):await fixture.Provider.SaveHolonsAsync(new[]{root},children,recursive,depth,initial,continueOnError:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());await fixture.Observe();
        var expected=scenario is 0 or 4?1:scenario is 1 or 2?2:3;
        var present=0;foreach(var id in ids)if(await fixture.Body(id)!=null)present++;Assert.AreEqual(expected,present);
        Assert.IsTrue(result.IsSaved);Assert.AreEqual(expected,result.SavedCount);Assert.AreEqual(1,root.Version);
        if(expected>1){Assert.AreEqual(root.Id,child.ParentHolonId);Assert.AreEqual(1,child.Version);}
        if(expected>2){Assert.AreEqual(child.Id,grandchild.ParentHolonId);Assert.AreEqual(1,grandchild.Version);}
        var read=await fixture.Provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);Assert.IsFalse(read.IsError,read.Message);
        Assert.AreEqual(expected>1?1:0,read.Result.Children.Count());if(expected>1)Assert.AreEqual(expected>2?1:0,read.Result.Children.Single().Children.Count());
        // Root-only edits preserve observed membership and child history/payload bytes.
        var childBody=await fixture.Body(child.Id);var manifest=((Holon)read.Result).ChildRoleManifestJson;
        root.Name="root-only";fixture.BeforeMutation();var scalar=await fixture.Provider.SaveHolonAsync(root,saveChildren:false);Assert.IsFalse(scalar.IsError,scalar.Exception?.ToString());await fixture.Observe();
        Assert.AreEqual(childBody,await fixture.Body(child.Id));Assert.AreEqual(manifest,root.ChildRoleManifestJson);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeBatchRollbackAndContinueAreExplicit(bool continueOnError,bool synchronous)
    {
        var suffix=Guid.NewGuid().ToString()[9..];var goodId=Guid.Parse("00000000-"+suffix);var badId=Guid.Parse("ffffffff-"+suffix);
        Assert.IsTrue(goodId.CompareTo(badId)<0);
        await using var fixture=new GraphFixture($"native-graph-rollback-{continueOnError}-{synchronous}",new[]{goodId,badId});await fixture.Begin();
        var bad=new Holon{Id=badId,Version=int.MaxValue,Name="exhausted",MetaData=new(){["owned-fixture"]=fixture.Marker}};
        fixture.Provider.IsVersionControlEnabled=false;fixture.BeforeMutation();var seed=await fixture.Provider.SaveHolonAsync(bad,saveChildren:false);Assert.IsFalse(seed.IsError,seed.Exception?.ToString());await fixture.Observe();
        var original=await fixture.Body(badId);fixture.Provider.IsVersionControlEnabled=true;
        var good=new Holon{Id=goodId,Name="independent",MetaData=new(){["owned-fixture"]=fixture.Marker}};bad.Name="must-not-commit";
        // In all-or-nothing mode stable UUID order writes good first, then the
        // real version-exhaustion failure must roll that first native write back.
        fixture.BeforeMutation();var result=synchronous?fixture.Provider.SaveHolons(new[]{bad,good},saveChildren:false,continueOnError:continueOnError):await fixture.Provider.SaveHolonsAsync(new[]{bad,good},saveChildren:false,continueOnError:continueOnError);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        Assert.IsInstanceOfType<InvalidDataException>(continueOnError?result.Exception.InnerException:result.Exception);await fixture.Observe();
        Assert.AreEqual(original,await fixture.Body(badId));Assert.AreEqual(continueOnError?1:0,result.SavedCount);
        if(continueOnError){Assert.IsNotNull(await fixture.Body(goodId));Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(1,good.Version);}
        else{Assert.IsNull(await fixture.Body(goodId));Assert.AreEqual(0,good.Version);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeNamedMembershipIsPreservedAndExplicitEmptyUnlinks(bool synchronous)
    {
        var rootId=Guid.NewGuid();var childId=Guid.NewGuid();
        await using var fixture=new GraphFixture($"native-graph-role-{synchronous}",new[]{rootId,childId});await fixture.Begin();
        var child=new Holon{Id=childId,Name="component",MetaData=new(){["owned-fixture"]=fixture.Marker}};
        var root=new ComponentGraphOwner{Id=rootId,Name="named-owner",Components=new(){child},MetaData=new(){["owned-fixture"]=fixture.Marker}};
        async Task Save(bool children){
            fixture.BeforeMutation();var saved=synchronous?fixture.Provider.SaveHolon(root,saveChildren:children):await fixture.Provider.SaveHolonAsync(root,saveChildren:children);
            Assert.IsFalse(saved.IsError,saved.Exception?.ToString());await fixture.Observe();Assert.IsTrue(saved.IsSaved);Assert.AreEqual(children&&root.Components.Count>0?2:1,saved.SavedCount);
        }
        await Save(true);var manifest=root.ChildRoleManifestJson;Assert.IsNotNull(manifest);
        var role=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Deserialize(manifest).Roles.Single(row=>row.Name=="Components");Assert.AreEqual(childId,role.ChildIds.Single());
        var childBody=await fixture.Body(childId);
        root.Components.Clear();root.Name="root-only";await Save(false);
        Assert.AreEqual(manifest,root.ChildRoleManifestJson);Assert.AreEqual(childBody,await fixture.Body(childId));
        var read=await fixture.Provider.LoadHolonAsync(rootId,loadChildren:false);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(manifest,((Holon)read.Result).ChildRoleManifestJson);
        await Save(true);Assert.AreEqual(0,NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Deserialize(root.ChildRoleManifestJson).Roles.Single(row=>row.Name=="Components").ChildIds.Count);
        Assert.AreEqual(childBody,await fixture.Body(childId));Assert.IsFalse((await fixture.Provider.LoadHolonAsync(childId,loadChildren:false)).IsError);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeOverlappingGraphsRejectLostUpdatesWithoutRetry(bool synchronous)
    {
        var suffix=Guid.NewGuid().ToString()[9..];var rootId=Guid.Parse("00000000-"+suffix);var childId=Guid.Parse("ffffffff-"+suffix);
        await using var fixture=new GraphFixture($"native-graph-concurrent-{synchronous}",new[]{rootId,childId});await fixture.Begin();
        var seed=new Holon{Id=rootId,Name="original",MetaData=new(){["owned-fixture"]=fixture.Marker},Children=new List<IHolon>{new Holon{Id=childId,Name="original-child",MetaData=new(){["owned-fixture"]=fixture.Marker}}}};
        fixture.BeforeMutation();var initial=await fixture.Provider.SaveHolonAsync(seed);Assert.IsFalse(initial.IsError,initial.Exception?.ToString());await fixture.Observe();
        var left=await fixture.Provider.LoadHolonAsync(rootId,loadChildrenFromProvider:true);var right=await fixture.Provider.LoadHolonAsync(rootId,loadChildrenFromProvider:true);
        Assert.IsFalse(left.IsError,left.Message);Assert.IsFalse(right.IsError,right.Message);
        left.Result.Name="left";left.Result.Children.Single().Name="left-child";right.Result.Name="right";right.Result.Children.Single().Name="right-child";
        await using var observer=new NpgsqlConnection(Environment.GetEnvironmentVariable("POSTGRESQLOASIS_CONNECTIONSTRING")!);await observer.OpenAsync();
        await using var gate=await fixture.Connection.BeginTransactionAsync();
        await using(var hold=new NpgsqlCommand("SELECT data_json FROM oasis_holons WHERE id=@id FOR UPDATE",fixture.Connection,gate)){hold.Parameters.AddWithValue("id",rootId);Assert.IsNotNull(await hold.ExecuteScalarAsync());}
        fixture.BeforeMutation();
        Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> Save(IHolon row)=>synchronous?Task.Run(()=>fixture.Provider.SaveHolon(row,continueOnError:false)):fixture.Provider.SaveHolonAsync(row,continueOnError:false);
        var first=Save(left.Result);var second=Save(right.Result);var released=false;
        try{
            // Observe both real native sessions waiting on their first row lock.
            // This waits for test synchronization, not a retry of either mutation.
            var waiting=0L;var deadline=DateTime.UtcNow.AddSeconds(10);
            while(DateTime.UtcNow<deadline){
                await using var inspect=new NpgsqlCommand("SELECT count(*) FROM pg_stat_activity WHERE datname=current_database() AND wait_event_type='Lock' AND query='SELECT data_json FROM oasis_holons WHERE id=$1 FOR UPDATE'",observer);
                waiting=(long)(await inspect.ExecuteScalarAsync())!;if(waiting>=2)break;await Task.Delay(25);
            }
            Assert.AreEqual(2L,waiting,"Both captured graph transactions must be blocked before the native lock is released.");
            await gate.CommitAsync();released=true;
            var outcomes=await Task.WhenAll(first,second);Assert.AreEqual(1,outcomes.Count(result=>!result.IsError));
            var winner=outcomes.Single(result=>!result.IsError);var loser=outcomes.Single(result=>result.IsError);
            Assert.IsTrue(winner.IsSaved);Assert.AreEqual(2,winner.SavedCount);Assert.IsFalse(loser.IsSaved);Assert.AreEqual(0,loser.SavedCount);
            Assert.IsInstanceOfType<PostgresException>(loser.Exception);Assert.AreEqual("40001",((PostgresException)loser.Exception).SqlState);
            await fixture.Observe();
            var current=await fixture.Provider.LoadHolonAsync(rootId,loadChildrenFromProvider:true);Assert.IsFalse(current.IsError,current.Message);
            Assert.AreEqual(2,current.Result.Version);Assert.AreEqual(winner.Result.Name,current.Result.Name);Assert.AreEqual(2,current.Result.Children.Single().Version);Assert.AreEqual(winner.Result.Name+"-child",current.Result.Children.Single().Name);
            var old=await fixture.Provider.LoadHolonAsync(rootId,loadChildren:false,version:1);Assert.IsFalse(old.IsError,old.Message);Assert.AreEqual("original",old.Result.Name);
            Assert.AreEqual(1,(ReferenceEquals(winner.Result,left.Result)?right.Result:left.Result).Version,"Rejected caller state must not be published as committed.");
        }finally{
            if(!released)await gate.RollbackAsync();await Task.WhenAll(first,second);
        }
    }
}
