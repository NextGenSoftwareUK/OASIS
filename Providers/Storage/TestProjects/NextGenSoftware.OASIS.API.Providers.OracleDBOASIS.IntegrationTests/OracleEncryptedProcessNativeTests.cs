using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class OracleEncryptedProcessNativeTests
{
    private sealed class Resolver:IHolonMetadataKeyResolver
    {
        public bool UsedOldPreparation;
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request) {
            if(request.KeyId is not ("key-a" or "key-b"))return new(){IsError=true};
            if(request.KeyId=="key-a"&&request.Purpose==HolonMetadataKeyPurpose.DecryptPreparation)UsedOldPreparation=true;
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
    private sealed record Intent(Guid Operation,Guid Actor,Guid Root,Guid Child,int WriterProcess);
    private sealed record Proof(Guid RootVersion,Guid ChildVersion,Guid? RootRevision,Guid? ChildRevision,
        string RootHash,string ChildHash,string ReceiptHash,string PreparationHash);
    private static string Work=>Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
    private static string Journal(bool sync)=>Path.Combine(Work,$"encrypted-process-{sync}.pending.json");
    private static string ProofPath(bool sync)=>Path.Combine(Work,$"encrypted-process-{sync}.written.json");
    private static string Connection=>Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
    private static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    private static async Task Persist<T>(string path,T value) {
        using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
        await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(value));file.Flush(true);
    }
    private static Holon Input(Intent intent)=>new(){Id=intent.Root,Name="owned Oracle process root",IsActive=true,
        MetaData=new(){{"private-note","owned Oracle root secret"}},
        Children=new List<IHolon>{new Holon{Id=intent.Child,ParentHolonId=intent.Root,Name="owned Oracle process child",IsActive=true,
            MetaData=new(){{"private-note","owned Oracle child secret"}}}}};
    private static HolonManager Manager(Owner provider,Resolver keys,string active) {
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}
        }};
        var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        return new HolonManager(provider,dna,providers){MetadataEncryptionPolicy=new HolonMetadataEncryptionPolicy("oracle-process-context",active,keys,Array.Empty<string>())};
    }
    private static async Task<OASISResult<IHolon>> Save(Owner provider,Resolver keys,string active,Intent intent,Holon input,bool synchronous) {
        var manager=Manager(provider,keys,active);
        var options=new CosmicWriteOptions(intent.Operation,new Dictionary<Guid,Guid?>{{intent.Root,null},{intent.Child,null}});
        return synchronous?manager.SaveHolon(input,intent.Actor,options,providerType:ProviderType.OracleDBOASIS):
            await manager.SaveHolonAsync(input,intent.Actor,options,providerType:ProviderType.OracleDBOASIS);
    }
    private static async Task<object?> Scalar(OracleConnection connection,string sql,Guid id) {
        await using var command=new OracleCommand(sql,connection){BindByName=true};
        command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();return await command.ExecuteScalarAsync();
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task PhaseOneCommitEncryptedOperation(bool synchronous) {
        Assert.IsFalse(File.Exists(Journal(synchronous)));Assert.IsFalse(File.Exists(ProofPath(synchronous)));
        var intent=new Intent(Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),Environment.ProcessId);
        var provider=new Owner(Connection);await using var verifier=new OracleConnection(Connection);await verifier.OpenAsync();
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var id in new[]{intent.Root,intent.Child})Assert.AreEqual(0,Convert.ToInt32(await Scalar(verifier,"SELECT COUNT(*) FROM OASIS_HOLONS WHERE ID=:Id",id)));
            foreach(var table in new[]{"OASIS_WRITE_PREPARATIONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS"})
                Assert.AreEqual(0,Convert.ToInt32(await Scalar(verifier,"SELECT COUNT(*) FROM "+table+" WHERE OPERATION_ID=:Id",intent.Operation)));
            await Persist(Journal(synchronous),intent);var input=Input(intent);
            var saved=await Save(provider,new Resolver(),"key-a",intent,input,synchronous);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.AreEqual(2,provider.Commits);
            var root=(await Scalar(verifier,"SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id",intent.Root))!.ToString()!;
            var child=(await Scalar(verifier,"SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id",intent.Child))!.ToString()!;
            Assert.IsFalse(root.Contains("owned Oracle root secret"));Assert.IsFalse(child.Contains("owned Oracle child secret"));
            await Persist(ProofPath(synchronous),new Proof(input.VersionId,input.Children.Single().VersionId,
                CosmicConcurrency.GetProviderRevisionId(input,ProviderType.OracleDBOASIS),CosmicConcurrency.GetProviderRevisionId(input.Children.Single(),ProviderType.OracleDBOASIS),
                Hash(root),Hash(child),Hash((await Scalar(verifier,"SELECT RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",intent.Operation))!.ToString()!),
                Hash((await Scalar(verifier,"SELECT PAYLOAD_JSON FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id",intent.Operation))!.ToString()!)));
        } finally {await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task PhaseTwoRecoverInDifferentProcess(bool synchronous) {
        var intent=JsonSerializer.Deserialize<Intent>(await File.ReadAllTextAsync(Journal(synchronous)))!;
        var proof=JsonSerializer.Deserialize<Proof>(await File.ReadAllTextAsync(ProofPath(synchronous)))!;
        Assert.AreNotEqual(intent.WriterProcess,Environment.ProcessId,"A genuinely different testhost process is required.");
        var provider=new Owner(Connection);await using var verifier=new OracleConnection(Connection);await verifier.OpenAsync();
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);var input=Input(intent);var keys=new Resolver();
            var replay=await Save(provider,keys,"key-b",intent,input,synchronous);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.AreEqual(0,provider.Commits);Assert.IsTrue(keys.UsedOldPreparation);
            Assert.AreEqual(proof.RootVersion,input.VersionId);Assert.AreEqual(proof.ChildVersion,input.Children.Single().VersionId);
            Assert.AreEqual(proof.RootRevision,CosmicConcurrency.GetProviderRevisionId(input,ProviderType.OracleDBOASIS));
            Assert.AreEqual(proof.ChildRevision,CosmicConcurrency.GetProviderRevisionId(input.Children.Single(),ProviderType.OracleDBOASIS));
            Assert.AreEqual("owned Oracle root secret",input.MetaData["private-note"].ToString());
            Assert.AreEqual("owned Oracle child secret",input.Children.Single().MetaData["private-note"].ToString());
            var root=(await Scalar(verifier,"SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id",intent.Root))!.ToString()!;
            var child=(await Scalar(verifier,"SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id",intent.Child))!.ToString()!;
            var receipt=(await Scalar(verifier,"SELECT RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",intent.Operation))!.ToString()!;
            var preparation=(await Scalar(verifier,"SELECT PAYLOAD_JSON FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id",intent.Operation))!.ToString()!;
            Assert.AreEqual(proof.RootHash,Hash(root));Assert.AreEqual(proof.ChildHash,Hash(child));
            Assert.AreEqual(proof.ReceiptHash,Hash(receipt));Assert.AreEqual(proof.PreparationHash,Hash(preparation));
            await using(var transaction=(OracleTransaction)await verifier.BeginTransactionAsync()) {
                await Remove("OASIS_HOLONS","ID","DATA_JSON",intent.Root,root,transaction);
                await Remove("OASIS_HOLONS","ID","DATA_JSON",intent.Child,child,transaction);
                await Remove("OASIS_WRITE_RECEIPTS","OPERATION_ID","RESULT_JSON",intent.Operation,receipt,transaction);
                await using(var remove=new OracleCommand("DELETE FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id AND ACTOR_ID=:Actor AND DBMS_LOB.COMPARE(PAYLOAD_JSON,:Payload)=0",verifier){BindByName=true,Transaction=transaction}) {
                    remove.Parameters.Add("Id",OracleDbType.Raw).Value=intent.Operation.ToByteArray();remove.Parameters.Add("Actor",OracleDbType.Raw).Value=intent.Actor.ToByteArray();
                    remove.Parameters.Add("Payload",OracleDbType.Clob).Value=preparation;Assert.AreEqual(1,await remove.ExecuteNonQueryAsync());
                }
                await transaction.CommitAsync();
            }
            foreach(var id in new[]{intent.Root,intent.Child})Assert.AreEqual(0,Convert.ToInt32(await Scalar(verifier,"SELECT COUNT(*) FROM OASIS_HOLONS WHERE ID=:Id",id)));
            foreach(var table in new[]{"OASIS_WRITE_PREPARATIONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS"})
                Assert.AreEqual(0,Convert.ToInt32(await Scalar(verifier,"SELECT COUNT(*) FROM "+table+" WHERE OPERATION_ID=:Id",intent.Operation)));
            await Persist(Path.Combine(Work,$"encrypted-process-{synchronous}.verified.json"),new {
                intent.Operation,writerProcess=intent.WriterProcess,readerProcess=Environment.ProcessId,verifiedUtc=DateTime.UtcNow,
                proofHash=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(ProofPath(synchronous))))
            });
            File.Delete(Journal(synchronous));
        } finally {await provider.DeActivateProviderAsync();}
        async Task Remove(string table,string identity,string payload,Guid id,string expected,OracleTransaction transaction) {
            await using var command=new OracleCommand("DELETE FROM "+table+" WHERE "+identity+"=:Id AND DBMS_LOB.COMPARE("+payload+",:Payload)=0",verifier){BindByName=true,Transaction=transaction};
            command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();command.Parameters.Add("Payload",OracleDbType.Clob).Value=expected;
            Assert.AreEqual(1,await command.ExecuteNonQueryAsync());
        }
    }
}
