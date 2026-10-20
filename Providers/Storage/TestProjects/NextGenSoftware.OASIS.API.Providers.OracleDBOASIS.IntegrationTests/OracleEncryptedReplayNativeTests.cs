using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class OracleEncryptedReplayNativeTests
{
    private sealed class Resolver:IHolonMetadataKeyResolver
    {
        public bool DenyOldPreparation;public bool UsedOldPreparation;
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request) {
            if(request.KeyId=="key-a"&&request.Purpose==HolonMetadataKeyPurpose.DecryptPreparation) {
                UsedOldPreparation=true;if(DenyOldPreparation)return new(){IsError=true,ErrorCode="OWNED_OLD_KEY_DENIED"};
            }
            if(request.KeyId is not ("key-a" or "key-b"))return new(){IsError=true};
            return new(new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId=="key-a"?(byte)17:(byte)34,32).ToArray()));
        }
    }
    private sealed class Owner(string connection):OracleDBOASIS(connection)
    {
        public int Commits;
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction) {
            await base.CommitLifecycleTransactionAsync(transaction);Commits++;
        }
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeV2EncryptedGraphAndBatchReplayAcrossFreshManagerAndKeyRotation(bool synchronous,bool batch)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"encrypted-replay-{synchronous}-{batch}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var op=Guid.NewGuid();var actor=Guid.NewGuid();var ids=Enumerable.Range(0,batch?4:2).Select(_=>Guid.NewGuid()).ToArray();
        const decimal exact=1.000000000000000000000000001m;
        HolonMetadataEncryptionPolicy Policy(Resolver keys,string active="key-a",string context="oracle-root-context")
            =>new(context,active,keys,new[]{"search-tag"});
        Holon[] Inputs(Resolver keys)=>Enumerable.Range(0,batch?2:1).Select(index=>new Holon {
            Id=ids[index*2],Name="owned Oracle encrypted root "+index,IsActive=true,
            MetaData=new(){{"private-note","root private value"},{"search-tag","public-search-tag"},{"exact",exact}},
            Children=new List<IHolon>{new Holon {
                Id=ids[index*2+1],ParentHolonId=ids[index*2],Name="owned Oracle encrypted child",IsActive=true,
                MetadataEncryptionOverride=Policy(keys,"key-b","oracle-child-context"),
                MetaData=new(){{"private-note","child private value"},{"exact",exact}}
            }}
        }).ToArray();
        HolonManager Manager(Owner provider,Resolver keys,string active="key-a") {
            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}
            }};
            var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
            return new HolonManager(provider,dna,providers){MetadataEncryptionPolicy=Policy(keys,active)};
        }
        async Task<OASISResult<IEnumerable<IHolon>>> Save(HolonManager manager,Holon[] roots) {
            var options=new CosmicWriteOptions(op,ids.ToDictionary(id=>id,_=>(Guid?)null));
            if(batch)return synchronous?manager.SaveHolons(roots,actor,options,providerType:ProviderType.OracleDBOASIS):
                await manager.SaveHolonsAsync(roots,actor,options,providerType:ProviderType.OracleDBOASIS);
            var result=synchronous?manager.SaveHolon(roots[0],actor,options,providerType:ProviderType.OracleDBOASIS):
                await manager.SaveHolonAsync(roots[0],actor,options,providerType:ProviderType.OracleDBOASIS);
            return new(result.Result==null?null:new[]{result.Result}){IsError=result.IsError,Message=result.Message,Exception=result.Exception,ErrorCode=result.ErrorCode};
        }
        await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
        async Task<object?> Scalar(string sql,Guid id) {
            await using var command=new OracleCommand(sql,verifier){BindByName=true};
            command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();return await command.ExecuteScalarAsync();
        }
        var first=new Owner(connection){IsVersionControlEnabled=true};Guid[] versions;Guid?[] revisions;
        try {
            Assert.IsFalse((await first.ActivateProviderAsync()).IsError);
            foreach(var id in ids)Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM OASIS_HOLONS WHERE ID=:Id",id)));
            foreach(var table in new[]{"OASIS_WRITE_PREPARATIONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS"})
                Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM "+table+" WHERE OPERATION_ID=:Id",op)));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,op,actor,synchronous,batch,state="before-encrypted-oracle-v2-save"}));file.Flush(true);
            }
            var keys=new Resolver();var input=Inputs(keys);var saved=await Save(Manager(first,keys),input);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.AreEqual(2,first.Commits);
            versions=input.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>row.VersionId).ToArray();
            revisions=input.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>CosmicConcurrency.GetProviderRevisionId(row,ProviderType.OracleDBOASIS)).ToArray();
        } finally {await first.DeActivateProviderAsync();}
        var stored=new Dictionary<Guid,string>();
        foreach(var id in ids) {
            var json=(await Scalar("SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id",id))!.ToString()!;
            Assert.IsTrue(json.Contains("oasis2:"));Assert.IsFalse(json.Contains("root private value")||json.Contains("child private value"));stored.Add(id,json);
        }
        var preparation=(await Scalar("SELECT PAYLOAD_JSON FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id",op))!.ToString()!;
        Assert.IsTrue(preparation.Contains("oasis-preparation1:"));
        Assert.IsFalse(preparation.Contains("root private value")||preparation.Contains("child private value"));
        var receipt=(await Scalar("SELECT RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",op))!.ToString()!;
        var restarted=new Owner(connection){IsVersionControlEnabled=true};
        try {
            Assert.IsFalse((await restarted.ActivateProviderAsync()).IsError);
            var keys=new Resolver();var manager=Manager(restarted,keys,"key-b");var fresh=Inputs(keys);
            var changed=Inputs(keys);changed[0].MetaData["private-note"]="changed private intent";
            var denied=await Save(manager,changed);Assert.IsTrue(denied.IsError);Assert.AreEqual(0,restarted.Commits);
            var deniedKeys=new Resolver{DenyOldPreparation=true};
            Assert.IsTrue((await Save(Manager(restarted,deniedKeys,"key-b"),Inputs(deniedKeys))).IsError);Assert.AreEqual(0,restarted.Commits);
            var replay=await Save(manager,fresh);Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
            Assert.AreEqual(0,restarted.Commits);Assert.IsTrue(keys.UsedOldPreparation);
            CollectionAssert.AreEqual(versions,fresh.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>row.VersionId).ToArray());
            CollectionAssert.AreEqual(revisions,fresh.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>CosmicConcurrency.GetProviderRevisionId(row,ProviderType.OracleDBOASIS)).ToArray());
            foreach(var root in fresh) {
                Assert.AreEqual("root private value",root.MetaData["private-note"].ToString());
                Assert.AreEqual("child private value",root.Children.Single().MetaData["private-note"].ToString());
                using var json=JsonDocument.Parse(StoredRecordJson.Serialize(root.MetaData));
                Assert.AreEqual(exact,json.RootElement.GetProperty("exact").GetDecimal());Assert.IsFalse(HolonChangeTracking.HasChanges(root));
            }
            foreach(var id in ids)Assert.AreEqual(stored[id],(await Scalar("SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id",id))!.ToString());
            Assert.AreEqual(preparation,(await Scalar("SELECT PAYLOAD_JSON FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id",op))!.ToString());
            Assert.AreEqual(receipt,(await Scalar("SELECT RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",op))!.ToString());
            Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM OASIS_WRITE_OPERATION_LOCKS WHERE OPERATION_ID=:Id",op)));
            await using(var transaction=(OracleTransaction)await verifier.BeginTransactionAsync()) {
                foreach(var id in ids)await Remove("OASIS_HOLONS","ID","DATA_JSON",id,stored[id],transaction);
                await Remove("OASIS_WRITE_RECEIPTS","OPERATION_ID","RESULT_JSON",op,receipt,transaction);
                await using(var remove=new OracleCommand("DELETE FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id AND ACTOR_ID=:Actor AND DBMS_LOB.COMPARE(PAYLOAD_JSON,:Payload)=0",verifier){BindByName=true,Transaction=transaction}) {
                    remove.Parameters.Add("Id",OracleDbType.Raw).Value=op.ToByteArray();remove.Parameters.Add("Actor",OracleDbType.Raw).Value=actor.ToByteArray();
                    remove.Parameters.Add("Payload",OracleDbType.Clob).Value=preparation;Assert.AreEqual(1,await remove.ExecuteNonQueryAsync());
                }
                await transaction.CommitAsync();
            }
            foreach(var id in ids)Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM OASIS_HOLONS WHERE ID=:Id",id)));
            foreach(var table in new[]{"OASIS_WRITE_PREPARATIONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS"})
                Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM "+table+" WHERE OPERATION_ID=:Id",op)));
            File.Delete(journal);
        } finally {await restarted.DeActivateProviderAsync();}
        async Task Remove(string table,string identity,string payload,Guid id,string expected,OracleTransaction transaction) {
            await using var command=new OracleCommand("DELETE FROM "+table+" WHERE "+identity+"=:Id AND DBMS_LOB.COMPARE("+payload+",:Payload)=0",verifier){BindByName=true,Transaction=transaction};
            command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();command.Parameters.Add("Payload",OracleDbType.Clob).Value=expected;
            Assert.AreEqual(1,await command.ExecuteNonQueryAsync());
        }
    }
}
