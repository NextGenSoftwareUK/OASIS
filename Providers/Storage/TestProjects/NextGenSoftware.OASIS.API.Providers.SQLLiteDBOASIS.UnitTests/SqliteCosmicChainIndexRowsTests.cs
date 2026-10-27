using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SqliteCosmicChainIndexRowsTests
{
    private string database=null!;private bool owned;
    private readonly byte[] key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private CosmicChainQuerySource source=null!;private readonly Guid actor=Guid.NewGuid();
    private readonly CosmicChainQueryPosition position=new(1,"block-1");
    [TestInitialize]
    public void Setup(){
        var root=new DirectoryInfo(AppContext.BaseDirectory);while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);var folder=Path.Combine(root.FullName,"TestResults","CosmicChainQuery");Directory.CreateDirectory(folder);
        database=Path.Combine(folder,"index.db");foreach(var path in new[]{database,database+"-wal",database+"-shm"})Assert.IsFalse(File.Exists(path),"Preserve unknown fixture.");owned=true;
        source=new(ProviderType.SolanaOASIS,"controlled-network",ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),2,new string('A',64),0);
    }
    [TestCleanup]public void Cleanup(){if(owned)foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);}
    private CosmicChainQueryReadRequirement Requirement=>new(source,position,0,TimeSpan.FromMinutes(1));
    private CosmicChainIndexEntry Entry(string name,Guid owner,bool isPublic,bool deleted=false){
        var row=new Holon{Id=Guid.NewGuid(),Version=1,VersionId=Guid.NewGuid(),Name=name,CreatedByAvatarId=owner,IsPublic=isPublic};
        if(deleted)row.DeletedDate=DateTime.UtcNow;
        var json=OasisJson.Serialize(row);var identity=new CosmicChainQueryRecordIdentity(row.Id,1,row.VersionId,deleted,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
        var receipt=new CosmicChainQueryRecordReceipt(source,position,Guid.NewGuid().ToString("D"),identity);return new(receipt,receipt,json);
    }
    private void Publish(SqliteCosmicChainIndexStore store,params CosmicChainIndexEntry[] rows){
        var result=store.Publish(Requirement,new(source,position,0),new(source.SourceProvider,source.NetworkIdentity,position,DateTimeOffset.UtcNow),DateTimeOffset.UtcNow,null,rows);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());
    }
    private CosmicQuery<Holon> Query(CosmicQueryReadAccess? access=null)=>CosmicQuery<Holon>.Create(CosmicQueryScope.Named(source.SourceProvider).Result)
        .WithIndexedSource(Requirement).Result.WithReadAccess(access??CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
    private CosmicQueryPlan<Holon> Plan(CosmicQuery<Holon> query){
        var plan=CosmicQueryPlanner.Plan(query,source.SourceProvider,new[]{new CosmicQueryTarget(source.SourceProvider,SqliteCosmicChainIndexStore.CanonicalRowCapabilities,source)});
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }
    private void Sql(string text){using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Pooling=false}.ToString());db.Open();using var command=db.CreateCommand();command.CommandText=text;command.ExecuteNonQuery();}

    [DataTestMethod]
    [DataRow("page")] [DataRow("catalogue")] [DataRow("injection")] [DataRow("rows")] [DataRow("empty")] [DataRow("cancelled")] [DataRow("stale")]
    [DataRow("source-missing")] [DataRow("source-changed")]
    [DataRow("boolean-tamper")] [DataRow("real-version-tamper")] [DataRow("projection-type-tamper")] [DataRow("projection-tamper")] [DataRow("projection-delete")] [DataRow("history-tamper")]
    public async Task NativeSqlVisibilityOrderingPagingAndSourceProofsShareOneCheckpoint(string mode){
        using var store=new SqliteCosmicChainIndexStore(database,source,key,canonicalRowProjection:true);
        var a=Entry("A",actor,false);var b=Entry("B",Guid.NewGuid(),true);var hidden=Entry("0-hidden",Guid.NewGuid(),false);var tombstone=Entry("0-deleted",actor,true,true);
        Publish(store,a,b,hidden,tombstone);
        ICosmicChainQueryIndexReader reader=store;
        Assert.AreSame(source,reader.Source);
        Assert.AreSame(SqliteCosmicChainIndexStore.CanonicalRowCapabilities,reader.IndexedRowCapabilities);
        var observer=new Observer(source,position,new[]{a,b,hidden,tombstone},mode);
        if(mode=="boolean-tamper")Sql("PRAGMA ignore_check_constraints=ON;UPDATE CosmicChainIndexRows SET IsPublic=2 WHERE Name='0-hidden'");
        if(mode=="real-version-tamper")Sql("UPDATE CosmicChainIndexHistory SET Version=1.5 WHERE Id='"+hidden.Receipt.Record.Id+"'");
        if(mode=="projection-type-tamper")Sql("UPDATE CosmicChainIndexRows SET Name=CAST(Name AS BLOB) WHERE Name='0-hidden'");
        if(mode=="projection-tamper")Sql("UPDATE CosmicChainIndexRows SET IsPublic=1 WHERE Name='0-hidden'");
        if(mode=="projection-delete")Sql("DELETE FROM CosmicChainIndexRows WHERE Name='0-hidden'");
        if(mode=="history-tamper")Sql("UPDATE CosmicChainIndexHistory SET CommitReference='forged' WHERE Id='"+hidden.Receipt.Record.Id+"'");
        var baseQuery=Query(mode=="catalogue"?CosmicQueryReadAccess.AuthoritativeRecords(actor,source.SourceProvider,new[]{hidden.Receipt.Record.Id}).Result:null);
        var name=mode=="injection"?"x' OR 1=1 --":"absent";
        var filtered=mode is "empty" or "injection"?baseQuery.Where(row=>row.Name==name):baseQuery.Where(row=>row.Version>=1);
        Assert.IsFalse(filtered.IsError,filtered.Message);var query=filtered.Result;
        query=query.OrderBy(row=>row.Name).ThenBy(row=>row.Id).Result;
        query=mode=="page"?query.Skip(1).Take(1).Result:query.Take(2).Result;
        using var cancellation=new CancellationTokenSource();if(mode=="cancelled")cancellation.Cancel();
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2}};
        var manager=new HolonManager(null,dna,new ProviderManager(null,dna));
        var result=await manager.ExecuteIndexedQueryAsync(Plan(query),reader,observer,cancellationToken:cancellation.Token);
        if(mode is "rows" or "empty" or "injection" or "catalogue" or "page"){
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(mode is "empty" or "injection"?0:mode is "catalogue" or "page"?1:2,result.Result.Rows.Count);
            Assert.AreEqual(source.IndexId,result.Result.Checkpoint.Source.IndexId);Assert.AreEqual(ProviderType.SQLLiteDBOASIS,result.Result.Checkpoint.Source.ExecutionProvider);
            Assert.AreEqual(1,observer.HeadCalls);Assert.AreEqual(mode is "empty" or "injection"?0:mode is "catalogue" or "page"?1:2,observer.RecordCalls);
            Assert.IsTrue(result.Result.Rows.All(row=>!HolonChangeTracking.HasChanges(row)),"Typed indexed rows must start clean.");
            if(mode=="rows")CollectionAssert.AreEqual(new[]{"A","B"},result.Result.Rows.Select(r=>r.Name).ToArray());
            if(mode=="page")Assert.AreEqual("B",result.Result.Rows.Single().Name);
        }else{
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
            if(mode is "boolean-tamper" or "real-version-tamper" or "projection-type-tamper" or "projection-tamper" or "projection-delete" or "history-tamper" or "cancelled")Assert.AreEqual(0,observer.RecordCalls);
        }
    }

    [TestMethod]public async Task HistoryOnlyAndUnsupportedMetadataNeverBecomeImplicitIndexedQueries(){
        source=new(source.SourceProvider,source.NetworkIdentity,source.ExecutionProvider,source.IndexId,1,source.CatalogueHash,0);
        using var store=new SqliteCosmicChainIndexStore(database,source,key);
        var result=await store.QueryRowsAsync(Plan(Query()),new Observer(source,position,Array.Empty<CosmicChainIndexEntry>(),"rows"));
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",result.ErrorCode);Assert.IsFalse(File.Exists(database));
        var metadata=Query().Where(row=>CosmicQueryMetadata.Value<string>(row,"secret")=="value").Result;
        var plan=CosmicQueryPlanner.Plan(metadata,source.SourceProvider,new[]{new CosmicQueryTarget(source.SourceProvider,SqliteCosmicChainIndexStore.CanonicalRowCapabilities,source)});
        Assert.IsTrue(plan.IsError);Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",plan.ErrorCode);Assert.IsFalse(File.Exists(database));
    }

    [TestMethod]public async Task ProjectionInsertFailureRollsBackHistoryAndCheckpoint(){
        using var store=new SqliteCosmicChainIndexStore(database,source,key,canonicalRowProjection:true);
        var initial=Entry("original",actor,false);Publish(store,initial);
        Sql("CREATE TRIGGER RejectProjection BEFORE INSERT ON CosmicChainIndexRows BEGIN SELECT RAISE(ABORT,'controlled projection failure'); END;");
        var holon=OasisJson.DeserializeStored<Holon>(initial.CanonicalJson);holon.Version=2;holon.VersionId=Guid.NewGuid();holon.Name="changed";
        var json=OasisJson.Serialize(holon);var position2=new CosmicChainQueryPosition(2,"block-2");
        var identity=new CosmicChainQueryRecordIdentity(holon.Id,2,holon.VersionId,false,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
        var receipt=new CosmicChainQueryRecordReceipt(source,position2,"new-commit",identity);
        var result=store.Publish(new(source,position2,0,TimeSpan.FromMinutes(1)),new(source,position2,0),new(source.SourceProvider,source.NetworkIdentity,position2,DateTimeOffset.UtcNow),DateTimeOffset.UtcNow,position,new[]{new CosmicChainIndexEntry(receipt,receipt,json)});
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        var snapshot=store.ReadSnapshot();Assert.IsFalse(snapshot.IsError);Assert.AreEqual(1,snapshot.Result.History.Count);Assert.AreEqual(1L,snapshot.Result.Checkpoint.Position.Slot);
        var rows=await store.QueryRowsAsync(Plan(Query()),new Observer(source,position,new[]{initial},"rows"));
        Assert.IsFalse(rows.IsError,rows.Exception?.ToString());Assert.AreEqual("original",rows.Result.Rows.Single().Name);
    }

    private sealed class Observer(CosmicChainQuerySource source,CosmicChainQueryPosition position,CosmicChainIndexEntry[] entries,string mode):ICosmicChainQuerySourceObserver
    {
        public int HeadCalls,RecordCalls;
        public Task<OASISResult<CosmicChainQueryObservedRange>> ObserveRangeAsync(CosmicChainQuerySource selected,CancellationToken token)=>throw new InvalidOperationException("A query must not ingest or scan the source.");
        public Task<OASISResult<CosmicChainQueryObservedRecord>> ObserveRecordAsync(CosmicChainQuerySource selected,string commit,CancellationToken token){
            RecordCalls++;if(mode=="source-missing")return Task.FromResult(new OASISResult<CosmicChainQueryObservedRecord>{IsError=true,ErrorCode="source-absent"});
            var row=entries.Single(e=>e.Receipt.CommitReference==commit);
            return Task.FromResult(new OASISResult<CosmicChainQueryObservedRecord>(new(row.Receipt,mode=="source-changed"?"altered":row.CanonicalJson)));
        }
        public Task<OASISResult<CosmicChainQueryFinalizedHead>> ObserveHeadAsync(CosmicChainQuerySource selected,CancellationToken token){
            HeadCalls++;return Task.FromResult(new OASISResult<CosmicChainQueryFinalizedHead>(new(source.SourceProvider,source.NetworkIdentity,position,DateTimeOffset.UtcNow-(mode=="stale"?TimeSpan.FromHours(1):TimeSpan.Zero))));
        }
    }
}
