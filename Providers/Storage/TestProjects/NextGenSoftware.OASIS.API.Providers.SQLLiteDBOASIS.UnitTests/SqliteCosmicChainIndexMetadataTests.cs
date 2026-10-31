using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests;

public partial class SqliteCosmicChainIndexRowsTests
{
    private void MetadataSource()=>source=new(source.SourceProvider,source.NetworkIdentity,source.ExecutionProvider,
        source.IndexId,3,source.CatalogueHash,source.CoverageStartSlot);
    private CosmicChainIndexEntry MetadataEntry(string name,Guid owner,bool isPublic,bool deleted=false)
    {
        var entry=Entry(name,owner,isPublic,deleted);
        var row=OasisJson.DeserializeStored<Holon>(entry.CanonicalJson);
        var status=new Dictionary<string,object>{["category"]=name=="B"?"tea":"coffee",
            ["active"]=name!="B",["stock"]=name=="B"?4:12,["amount"]=name=="B"?9007199254740992L:9007199254740993L};
        if(name!="B")status["optional"]=null!;
        row.MetaData=new(){["Status"]=status,["secret"]="never-export-this-secret",
            ["STARNETHolonId"]="unselected-system-key",["__oasis_enc__"]="opaque-envelope-do-not-index"};
        var json=OasisJson.Serialize(row);
        var identity=new CosmicChainQueryRecordIdentity(row.Id,row.Version,row.VersionId,deleted,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
        var receipt=new CosmicChainQueryRecordReceipt(source,position,entry.Receipt.CommitReference,identity);
        return new(receipt,receipt,json);
    }
    private CosmicQueryPlan<Holon> MetadataPlan(SqliteCosmicChainIndexStore store,CosmicQuery<Holon> query)
    {
        var plan=CosmicQueryPlanner.Plan(query,source.SourceProvider,new[]{new CosmicQueryTarget(source.SourceProvider,store.IndexedRowCapabilities,source)});
        Assert.IsFalse(plan.IsError,plan.Message);return plan.Result;
    }

    [DataTestMethod]
    [DataRow("equality")] [DataRow("nested")] [DataRow("Boolean")] [DataRow("Int64")]
    [DataRow("null")] [DataRow("exists")] [DataRow("missing")] [DataRow("membership")]
    [DataRow("Int32")] [DataRow("nullable-int")] [DataRow("nullable-long")] [DataRow("nullable-bool")]
    [DataRow("nullable-null")] [DataRow("integer-order")] [DataRow("Boolean-order")]
    [DataRow("ordering")] [DataRow("zero")] [DataRow("tamper-json")] [DataRow("tamper-proof")]
    public async Task Schema3MetadataRowsAndCardinalityUseOneAuthenticatedNativeProjection(string mode)
    {
        MetadataSource();var policy=new HolonQueryableMetadataPolicy(new[]{"Status"});
        var a=MetadataEntry("A",actor,false);var b=MetadataEntry("B",Guid.NewGuid(),true);
        var hidden=MetadataEntry("hidden",Guid.NewGuid(),false);var deleted=MetadataEntry("deleted",actor,true,true);
        using(var initial=new SqliteCosmicChainIndexStore(database,source,key,policy))Publish(initial,a,b,hidden,deleted);
        using var store=new SqliteCosmicChainIndexStore(database,source,key,policy); // Real close/reopen.
        var query=Query();var values=new[]{"coffee"};
        query=(mode switch {
            "Boolean"=>query.Where(row=>CosmicQueryMetadata.Value<bool>(row,"Status","active")==true),
            "Int64"=>query.Where(row=>CosmicQueryMetadata.Value<long>(row,"Status","amount")>9007199254740992L),
            "Int32"=>query.Where(row=>CosmicQueryMetadata.Value<int>(row,"Status","stock")>8),
            "nullable-int"=>query.Where(row=>CosmicQueryMetadata.Value<int?>(row,"Status","stock")>8),
            "nullable-long"=>query.Where(row=>CosmicQueryMetadata.Value<long?>(row,"Status","amount")>9007199254740992L),
            "nullable-bool"=>query.Where(row=>CosmicQueryMetadata.Value<bool?>(row,"Status","active")==true),
            "nullable-null"=>query.Where(row=>CosmicQueryMetadata.Value<int?>(row,"Status","optional")==null),
            "integer-order"=>query.OrderByDescending(row=>CosmicQueryMetadata.Value<long>(row,"Status","amount")).Take(1),
            "Boolean-order"=>query.OrderByDescending(row=>CosmicQueryMetadata.Value<bool>(row,"Status","active")).Take(1),
            "null"=>query.Where(row=>CosmicQueryMetadata.IsNull(row,"Status","optional")),
            "exists"=>query.Where(row=>CosmicQueryMetadata.Exists(row,"Status","optional")),
            "missing"=>query.Where(row=>CosmicQueryMetadata.IsNull(row,"Status","absent")),
            "membership"=>query.Where(row=>values.Contains(CosmicQueryMetadata.Value<string>(row,"Status","category"))),
            "ordering"=>query.OrderBy(row=>CosmicQueryMetadata.Value<string>(row,"Status","category")).Take(1),
            _=>query.Where(row=>CosmicQueryMetadata.Value<string>(row,"Status","category")=="coffee")
        }).Result;
        if(mode=="zero")query=query.Take(0).Result;
        if(mode=="tamper-json")Sql("UPDATE CosmicChainIndexRows SET MetaDataJson='{}'");
        if(mode=="tamper-proof")Sql("UPDATE CosmicChainIndexRows SET MetaDataProof='altered'");
        var observer=new Observer(source,position,new[]{a,b,hidden,deleted},"rows");
        var rows=await store.QueryRowsAsync(MetadataPlan(store,query),observer);
        var scalarPlan=CosmicScalarPlanner.Plan(query.LongCount().Result,source.SourceProvider,new[]{
            new CosmicScalarTarget(source.SourceProvider,store.IndexedRowCapabilities,store.IndexedScalarCapabilities,source)});
        Assert.IsFalse(scalarPlan.IsError,scalarPlan.Message);
        var count=await CosmicIndexedScalarExecutor.ExecuteAsync(scalarPlan.Result,store,observer);
        if(mode.StartsWith("tamper-"))
        {
            Assert.IsTrue(rows.IsError);Assert.IsTrue(count.IsError);
            Assert.AreEqual(0,observer.RecordCalls);Assert.AreEqual(0,observer.HeadCalls);return;
        }
        Assert.IsFalse(rows.IsError,rows.Exception?.ToString());Assert.IsFalse(count.IsError,count.Exception?.ToString());
        var expected=mode is "missing" or "zero"?0:1;
        Assert.AreEqual(expected,rows.Result.Rows.Count);Assert.AreEqual((long)expected,count.Result.Value);
        if(expected==1)Assert.AreEqual("A",rows.Result.Rows.Single().Name);
        using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Pooling=false}.ToString());db.Open();
        using var sql=db.CreateCommand();sql.CommandText="SELECT MetaDataJson FROM CosmicChainIndexRows";
        using var reader=sql.ExecuteReader();while(reader.Read())
        {
            var plaintext=reader.GetString(0);StringAssert.Contains(plaintext,"Status");
            Assert.IsFalse(plaintext.Contains("secret"));Assert.IsFalse(plaintext.Contains("__oasis_enc__"));
            Assert.IsFalse(plaintext.Contains("STARNETHolonId"));
        }
    }

    [TestMethod]
    public void Schema3RequiresExplicitPolicyAndRejectsProfileMismatchWithoutMigration()
    {
        Assert.ThrowsException<ArgumentException>(()=>new SqliteCosmicChainIndexStore(database,source,key,new HolonQueryableMetadataPolicy(new[]{"Status"})));
        MetadataSource();
        Assert.ThrowsException<ArgumentException>(()=>new SqliteCosmicChainIndexStore(database,source,key,canonicalRowProjection:true));
        using(var initial=new SqliteCosmicChainIndexStore(database,source,key,new HolonQueryableMetadataPolicy(new[]{"Status"})))
            Publish(initial,MetadataEntry("A",actor,false));
        using var changed=new SqliteCosmicChainIndexStore(database,source,key,new HolonQueryableMetadataPolicy(new[]{"Name"}));
        Assert.IsTrue(changed.ReadSnapshot().IsError);
    }

    [TestMethod]
    public void Schema3UnenrolledMetadataRejectsBeforeCreatingDatabase()
    {
        MetadataSource();using var store=new SqliteCosmicChainIndexStore(database,source,key,new HolonQueryableMetadataPolicy(new[]{"Status"}));
        var query=Query().Where(row=>CosmicQueryMetadata.Value<string>(row,"secret")=="value").Result;
        var plan=CosmicQueryPlanner.Plan(query,source.SourceProvider,new[]{new CosmicQueryTarget(source.SourceProvider,store.IndexedRowCapabilities,source)});
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED",plan.ErrorCode);Assert.IsFalse(File.Exists(database));
    }
    [TestMethod]
    public void MetadataProjectionFailureCannotAdvanceHistoryOrCheckpoint()
    {
        MetadataSource();var policy=new HolonQueryableMetadataPolicy(new[]{"Status"});
        using var store=new SqliteCosmicChainIndexStore(database,source,key,policy);
        var initial=MetadataEntry("A",actor,false);Publish(store,initial);
        Sql("CREATE TRIGGER RejectMetadata BEFORE INSERT ON CosmicChainIndexRows BEGIN SELECT RAISE(ABORT,'metadata failure'); END;");
        var row=OasisJson.DeserializeStored<Holon>(initial.CanonicalJson);row.Version=2;row.VersionId=Guid.NewGuid();
        row.MetaData["Status"]=new Dictionary<string,object>{["category"]="changed"};
        var json=OasisJson.Serialize(row);var next=new CosmicChainQueryPosition(2,"block-2");
        var identity=new CosmicChainQueryRecordIdentity(row.Id,row.Version,row.VersionId,false,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
        var receipt=new CosmicChainQueryRecordReceipt(source,next,"metadata-next",identity);
        var result=store.Publish(new(source,next,0,TimeSpan.FromMinutes(1)),new(source,next,0),
            new(source.SourceProvider,source.NetworkIdentity,next,DateTimeOffset.UtcNow),DateTimeOffset.UtcNow,position,
            new[]{new CosmicChainIndexEntry(receipt,receipt,json)});
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        var snapshot=store.ReadSnapshot();Assert.IsFalse(snapshot.IsError,snapshot.Exception?.ToString());
        Assert.AreEqual(1,snapshot.Result.History.Count);Assert.AreEqual(position.Slot,snapshot.Result.Checkpoint.Position.Slot);
        using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=database,Pooling=false}.ToString());db.Open();
        using var command=db.CreateCommand();command.CommandText="SELECT MetaDataJson FROM CosmicChainIndexRows";
        StringAssert.Contains((string)command.ExecuteScalar()!,"coffee");
    }

    private sealed class MetadataKeyResolver(byte[] bytes):IHolonMetadataKeyResolver
    {
        public int Encryptions,Decryptions;
        public NextGenSoftware.OASIS.Common.OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            Assert.AreEqual("schema3-demo",request.ContextId);Assert.AreEqual("metadata-key",request.KeyId);
            if(request.Purpose==HolonMetadataKeyPurpose.Encrypt)Encryptions++;
            else if(request.Purpose==HolonMetadataKeyPurpose.Decrypt)Decryptions++;
            else Assert.Fail("Unexpected key operation.");
            return new(new HolonMetadataKeyMaterial(bytes));
        }
    }
    private CosmicChainIndexEntry AuthenticatedMetadataEntry(CosmicChainIndexEntry entry,HolonMetadataEncryptionPolicy policy)
    {
        var row=OasisJson.DeserializeStored<Holon>(entry.CanonicalJson);
        row.MetaData.Remove("secret");
        row.MetaData["__oasis_enc__"]=HolonMetadataCipher.Encrypt(row.Id,
            NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(new Dictionary<string,object>{["secret"]="private-value"}),policy);
        var json=OasisJson.Serialize(row);
        var receipt=new CosmicChainQueryRecordReceipt(source,position,entry.Receipt.CommitReference,
            new(row.Id,row.Version,row.VersionId,false,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))));
        return new(receipt,receipt,json);
    }

    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public async Task SharedMetadataWalkthroughUsesV2ManagerLinqAndCleanTracking(bool authorizedContext)
    {
        MetadataSource();using var store=new SqliteCosmicChainIndexStore(database,source,key,new HolonQueryableMetadataPolicy(new[]{"Status"}));
        var resolver=new MetadataKeyResolver(key);
        var encryption=new HolonMetadataEncryptionPolicy("schema3-demo","metadata-key",resolver);
        var a=AuthenticatedMetadataEntry(MetadataEntry("A",actor,false),encryption);
        var hidden=AuthenticatedMetadataEntry(MetadataEntry("hidden",Guid.NewGuid(),false),encryption);
        Publish(store,a,hidden);
        var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2}};
        var manager=new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null,dna,new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna));
        if(authorizedContext)manager.MetadataEncryptionPolicy=encryption;
        var execution=CosmicCqlDemo.ChainIndexedMetadataWalkthrough.RunAsync(manager,store,
            new Observer(source,position,new[]{a,hidden},"rows"),Requirement,CosmicQueryReadAccess.OwnerOrPublic(actor).Result,TextWriter.Null);
        if(!authorizedContext)
        {
            var rejected=await Assert.ThrowsExceptionAsync<InvalidOperationException>(async()=>{await execution;});
            StringAssert.Contains(rejected.Message,"HOLON_METADATA_DECRYPTION_CONFIG_MISSING");
            Assert.AreEqual(0,resolver.Decryptions);return;
        }
        var report=await execution;
        Assert.AreEqual(1,report.Rows.Rows.Count);Assert.AreEqual(1L,report.Count);Assert.IsTrue(report.Any);Assert.AreEqual(0L,report.EmptyWindow);
        Assert.AreEqual(a.Receipt.Record.Id,report.Rows.Rows.Single().Id);
        Assert.IsFalse(HolonChangeTracking.HasChanges(report.Rows.Rows.Single()));
        Assert.AreEqual(a.Receipt.CommitReference,report.Rows.Receipts.Single().CommitReference);
        Assert.AreEqual("private-value",Convert.ToString(report.Rows.Rows.Single().MetaData["secret"]));
        Assert.AreEqual(2,resolver.Encryptions);Assert.AreEqual(1,resolver.Decryptions);
    }

    [DataTestMethod]
    [DataRow("string")] [DataRow("Int64")] [DataRow("Int32")] [DataRow("Boolean")]
    [DataRow("nullable-int")] [DataRow("nullable-long")] [DataRow("nullable-bool")]
    [DataRow("null")] [DataRow("missing")] [DataRow("wrong-type")] [DataRow("private")]
    public async Task SelectedMetadataCellsUseNativeDomainsAndIndependentSourceWitnesses(string mode)
    {
        MetadataSource();using var store=new SqliteCosmicChainIndexStore(database,source,key,new HolonQueryableMetadataPolicy(new[]{"Status"}));
        var entry=MetadataEntry("A",actor,false);Publish(store,entry);
        var observer=new Observer(source,position,new[]{entry},"rows");
        var id=entry.Receipt.Record.Id;
        var query=Query().Where(row=>row.Id==id).Result;
        var targets=new[]{new CosmicProjectionTarget(source.SourceProvider,store.IndexedRowCapabilities,store.IndexedProjectionCapabilities,source)};
        async Task<(bool Error,object? Value)> Select<TResult>(CosmicProjectionQuery<Holon,TResult> selection)
        {
            var plan=CosmicProjectionPlanner.Plan(selection,source.SourceProvider,targets);
            if(plan.IsError){Assert.AreEqual("private",mode,plan.ErrorCode+": "+plan.Message);Assert.AreEqual(0,observer.HeadCalls);return(true,null);}
            var result=await CosmicIndexedProjectionExecutor.ExecuteAsync(plan.Result,store,observer);
            return(result.IsError,result.IsError?null:result.Result.Rows.Single());
        }
        var result=mode switch {
            "Int64"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<long>(row,"Status","amount")).Result),
            "Int32"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<int>(row,"Status","stock")).Result),
            "Boolean"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<bool>(row,"Status","active")).Result),
            "nullable-int"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<int?>(row,"Status","stock")).Result),
            "nullable-long"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<long?>(row,"Status","amount")).Result),
            "nullable-bool"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<bool?>(row,"Status","active")).Result),
            "null"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<int?>(row,"Status","optional")).Result),
            "missing"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<string>(row,"Status","absent")).Result),
            "wrong-type"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<long>(row,"Status","category")).Result),
            "private"=>await Select(query.Select(row=>CosmicQueryMetadata.Value<string>(row,"secret")).Result),
            _=>await Select(query.Select(row=>CosmicQueryMetadata.Value<string>(row,"Status","category")).Result)
        };
        if(mode is "private" or "wrong-type"){Assert.IsTrue(result.Error);Assert.IsNull(result.Value);return;}
        Assert.IsFalse(result.Error);
        if(mode is "Int64" or "nullable-long")Assert.AreEqual(9007199254740993L,result.Value);
        else if(mode is "Int32" or "nullable-int")Assert.AreEqual(12,result.Value);
        else if(mode is "Boolean" or "nullable-bool")Assert.AreEqual(true,result.Value);
        else if(mode is "null" or "missing")Assert.IsNull(result.Value);
        else Assert.AreEqual("coffee",result.Value);
    }

    [TestMethod]
    public async Task MixedCanonicalAndMetadataSelectionPreservesColumnOrderAndExactValues()
    {
        MetadataSource();using var store=new SqliteCosmicChainIndexStore(database,source,key,new HolonQueryableMetadataPolicy(new[]{"Status"}));
        var entry=MetadataEntry("A",actor,false);Publish(store,entry);
        var selected=Query().Select(row=>new{row.Id,
            Category=CosmicQueryMetadata.Value<string>(row,"Status","category"),
            Amount=CosmicQueryMetadata.Value<long>(row,"Status","amount")}).Result;
        var planned=CosmicProjectionPlanner.Plan(selected,source.SourceProvider,new[]{
            new CosmicProjectionTarget(source.SourceProvider,store.IndexedRowCapabilities,store.IndexedProjectionCapabilities,source)});
        Assert.IsFalse(planned.IsError,planned.Message);
        var result=await CosmicIndexedProjectionExecutor.ExecuteAsync(planned.Result,store,new Observer(source,position,new[]{entry},"rows"));
        Assert.IsFalse(result.IsError,result.Exception?.ToString());
        var row=result.Result.Rows.Single();Assert.AreEqual(entry.Receipt.Record.Id,row.Id);
        Assert.AreEqual("coffee",row.Category);Assert.AreEqual(9007199254740993L,row.Amount);
    }

}
