using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SqliteCosmicChainIndexStoreTests
{
    private string database=null!;
    private bool ownsFixture;
    private CosmicChainQuerySource source=null!;
    private readonly byte[] key=Enumerable.Range(1,32).Select(x=>(byte)x).ToArray();
    private readonly DateTimeOffset now=DateTimeOffset.UtcNow;
    [TestInitialize]
    public void Setup(){
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Scripts","TestHosts")))root=root.Parent;
        Assert.IsNotNull(root);var folder=Path.Combine(root.FullName,"TestResults","CosmicChainIndexStore");Directory.CreateDirectory(folder);
        database=Path.Combine(folder,"index.db");
        foreach(var path in new[]{database,database+"-wal",database+"-shm"})Assert.IsFalse(File.Exists(path),"Preserve an existing unknown fixture.");
        ownsFixture=true;
        source=new(ProviderType.SolanaOASIS,"controlled-network",ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),1,new string('A',64),0);
    }
    [TestCleanup]
    public void Cleanup(){
        if(!ownsFixture)return;
        foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);
    }
    private CosmicChainQueryPosition Position(long slot)=>new(slot,"block-"+slot);
    private CosmicChainQueryCheckpoint Checkpoint(long slot)=>new(source,Position(slot),0);
    private CosmicChainQueryReadRequirement Requirement(long slot)=>new(source,Position(slot),0,TimeSpan.FromMinutes(1));
    private CosmicChainQueryFinalizedHead Head(long slot)=>new(source.SourceProvider,source.NetworkIdentity,Position(slot),now);
    private CosmicChainIndexEntry Entry(Guid id,long slot,int version=1,bool deleted=false){
        var holon=new Holon{Id=id,Version=version,VersionId=Guid.NewGuid(),Name="controlled secret payload"};
        if(deleted)holon.DeletedDate=DateTime.UtcNow;
        var json=OasisJson.Serialize(holon);var identity=new CosmicChainQueryRecordIdentity(id,version,holon.VersionId,deleted,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
        var receipt=new CosmicChainQueryRecordReceipt(source,Position(slot),Guid.NewGuid().ToString("D"),identity);
        return new(receipt,new CosmicChainQueryRecordReceipt(source,Position(slot),receipt.CommitReference,identity),json);
    }
    private NextGenSoftware.OASIS.Common.OASISResult<bool> Publish(SqliteCosmicChainIndexStore store,long slot,CosmicChainQueryPosition? previous,params CosmicChainIndexEntry[] entries)
        =>store.Publish(Requirement(slot),Checkpoint(slot),Head(slot),now,previous,entries);
    private void Sql(string command){using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Pooling=false}.ToString());db.Open();using var q=db.CreateCommand();q.CommandText=command;q.ExecuteNonQuery();}

    [TestMethod]
    public void RestartPreservesEncryptedHistoryAndExactReplayDoesNotWrite(){
        var entry=Entry(Guid.NewGuid(),1);
        using(var store=new SqliteCosmicChainIndexStore(database,source,key)){
            var saved=Publish(store,1,null,entry);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreEqual(1,saved.SavedCount);
        }
        using var restarted=new SqliteCosmicChainIndexStore(database,source,key);
        var snapshot=restarted.ReadSnapshot();Assert.IsFalse(snapshot.IsError,snapshot.Exception?.ToString());Assert.AreEqual(1,snapshot.Result.History.Count);
        Assert.AreEqual(entry.CanonicalJson,snapshot.Result.History[0].CanonicalJson);
        var replay=Publish(restarted,1,null,entry);Assert.IsFalse(replay.IsError);Assert.IsFalse(replay.IsSaved);Assert.AreEqual(0,replay.SavedCount);
        Assert.IsFalse(Encoding.UTF8.GetString(File.ReadAllBytes(database)).Contains("controlled secret payload"));
    }

    [TestMethod]
    public void FailedSecondInsertRollsBackFirstInsertAndCheckpoint(){
        using var store=new SqliteCosmicChainIndexStore(database,source,key);
        Assert.IsFalse(Publish(store,1,null,Entry(Guid.NewGuid(),1)).IsError);
        var first=Entry(Guid.Parse("00000001-0000-4000-8000-000000000000"),2);
        var second=Entry(Guid.Parse("00000002-0000-4000-8000-000000000000"),2);
        Sql("CREATE TRIGGER FailSecond BEFORE INSERT ON CosmicChainIndexHistory WHEN NEW.Id='"+second.Receipt.Record.Id+"' BEGIN SELECT RAISE(ABORT,'controlled write failure'); END;");
        var failed=Publish(store,2,Position(1),first,second);Assert.IsTrue(failed.IsError);Assert.IsFalse(failed.IsSaved);
        var snapshot=store.ReadSnapshot();Assert.IsFalse(snapshot.IsError);Assert.AreEqual(1L,snapshot.Result.Checkpoint.Position.Slot);Assert.AreEqual(1,snapshot.Result.History.Count);
    }

    [TestMethod]
    public async Task IndependentWritersCannotBothAdvanceTheSameCheckpoint(){
        using var a=new SqliteCosmicChainIndexStore(database,source,key);using var b=new SqliteCosmicChainIndexStore(database,source,key);
        Assert.IsFalse(Publish(a,1,null,Entry(Guid.NewGuid(),1)).IsError);
        using var barrier=new Barrier(2);
        Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> Run(SqliteCosmicChainIndexStore store)=>Task.Run(()=>{var entry=Entry(Guid.NewGuid(),2);barrier.SignalAndWait();return Publish(store,2,Position(1),entry);});
        var results=await Task.WhenAll(Run(a),Run(b));Assert.AreEqual(1,results.Count(r=>!r.IsError));
        Assert.AreEqual("CQL_INDEX_CHECKPOINT_CONFLICT",results.Single(r=>r.IsError).ErrorCode);
        Assert.AreEqual(2,a.ReadSnapshot().Result.History.Count);
    }

    [TestMethod]
    public void TombstoneRetainsHistoryAndConflictingRevisionCannotReplaceIt(){
        using var store=new SqliteCosmicChainIndexStore(database,source,key);var id=Guid.NewGuid();
        Assert.IsFalse(Publish(store,1,null,Entry(id,1)).IsError);
        var deleted=Entry(id,2,2,true);Assert.IsFalse(Publish(store,2,Position(1),deleted).IsError);
        Assert.AreEqual("CQL_INDEX_REVISION_CONFLICT",Publish(store,3,Position(2),Entry(id,3,2)).ErrorCode);
        var snapshot=store.ReadSnapshot();Assert.AreEqual(2,snapshot.Result.History.Count);Assert.IsTrue(snapshot.Result.History[1].Receipt.Record.IsDeleted);
    }

    [TestMethod]
    public void AlteredCanonicalPayloadAndPreviouslyCoveredNewCommitReject(){
        using var store=new SqliteCosmicChainIndexStore(database,source,key);var entry=Entry(Guid.NewGuid(),1);
        Assert.AreEqual("CQL_SOURCE_RECORD_MISMATCH",Publish(store,1,null,entry with{CanonicalJson=entry.CanonicalJson.Replace("controlled secret","changed secret")}).ErrorCode);
        Assert.IsFalse(Publish(store,1,null,entry).IsError);
        Assert.AreEqual("CQL_INDEX_COVERAGE_CONFLICT",Publish(store,2,Position(1),Entry(Guid.NewGuid(),1)).ErrorCode);
        Assert.AreEqual(1,store.ReadSnapshot().Result.History.Count);
    }

    [DataTestMethod]
    [DataRow("DELETE FROM CosmicChainIndexHistory")]
    [DataRow("UPDATE CosmicChainIndexState SET Envelope='broken'")]
    [DataRow("UPDATE CosmicChainIndexHistory SET Id='forged'")]
    public void CorruptOrIncompleteStorageNeverReturnsAPartialSnapshot(string mutation){
        using var store=new SqliteCosmicChainIndexStore(database,source,key);Assert.IsFalse(Publish(store,1,null,Entry(Guid.NewGuid(),1)).IsError);Sql(mutation);
        var snapshot=store.ReadSnapshot();Assert.IsTrue(snapshot.IsError);Assert.IsNull(snapshot.Result);
    }

    [DataTestMethod,DataRow(false),DataRow(true)]
    public void AnotherSourceOrKeyCannotReadTheIndex(bool wrongKey){
        using var store=new SqliteCosmicChainIndexStore(database,source,key);Assert.IsFalse(Publish(store,1,null,Entry(Guid.NewGuid(),1)).IsError);
        var other=wrongKey?source:new CosmicChainQuerySource(source.SourceProvider,"different-network",source.ExecutionProvider,source.IndexId,1,source.CatalogueHash,0);
        using var reader=new SqliteCosmicChainIndexStore(database,other,wrongKey?Enumerable.Repeat((byte)255,32).ToArray():key);
        Assert.IsTrue(reader.ReadSnapshot().IsError);
        Assert.AreEqual(1,store.ReadSnapshot().Result.History.Count);
    }

    [TestMethod]
    public void DisposedStoreCannotAcknowledgeReadsOrPublication(){
        var store=new SqliteCosmicChainIndexStore(database,source,key);store.Dispose();
        Assert.IsTrue(store.ReadSnapshot().IsError);Assert.IsTrue(Publish(store,1,null).IsError);Assert.IsFalse(File.Exists(database));
    }
}
