using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public partial class OraclePreparationNativeTests
{
    private sealed class HeldOwner(string connection):OracleDBOASIS(connection)
    {
        public readonly TaskCompletionSource<int> Locked=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int commits;
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction) {
            if(Interlocked.Increment(ref commits)==1) {
                await using var sid=new OracleCommand("SELECT SYS_CONTEXT('USERENV','SID') FROM DUAL",transaction.Connection){Transaction=transaction};
                Locked.TrySetResult(int.Parse((await sid.ExecuteScalarAsync())!.ToString()!));
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            await base.CommitLifecycleTransactionAsync(transaction);
        }
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativePreparationAndConditionalReceiptShareOperationLock(bool writerFirst)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"preparation-race-{writerFirst}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),Name="owned preparation receipt race"};
        var candidate=new HolonWritePreparation(operation,actor,ProviderType.OracleDBOASIS,
            new[]{new ProtectedHolonPreparation(root.Id,"oasis-preparation1:owned-native-race")});
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.OracleDBOASIS,
            new[]{new HolonWriteCondition(root.Id,null)}),ConditionalHolonOperation.Save,new[]{root},false);
        var provider=new HeldOwner(connection);Task<OASISResult<IEnumerable<IHolon>>>? write=null;
        Task<OASISResult<HolonWritePreparation>>? claim=null;
        await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
        async Task<object?> Scalar(string sql) {
            await using var command=new OracleCommand(sql,verifier){BindByName=true};
            command.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();return await command.ExecuteScalarAsync();
        }
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var table in new[]{"OASIS_WRITE_PREPARATIONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS"})
                Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM "+table+" WHERE OPERATION_ID=:Id")));
            await using(var absent=new OracleCommand("SELECT COUNT(*) FROM OASIS_HOLONS WHERE ID=:Id",verifier){BindByName=true}) {
                absent.Parameters.Add("Id",OracleDbType.Raw).Value=root.Id.ToByteArray();Assert.AreEqual(0,Convert.ToInt32(await absent.ExecuteScalarAsync()));
            }
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{operation,actor,id=root.Id,writerFirst,state="before-native-preparation-race"}));file.Flush(true);
            }
            if(writerFirst)write=provider.ExecuteConditionalHolonWriteAsync(request);else claim=provider.ClaimHolonWritePreparationAsync(candidate);
            var blocker=await provider.Locked.Task.WaitAsync(TimeSpan.FromSeconds(15));
            if(writerFirst)claim=provider.ClaimHolonWritePreparationAsync(candidate);else write=provider.ExecuteConditionalHolonWriteAsync(request);
            var deadline=DateTime.UtcNow.AddSeconds(15);var blocked=false;
            do {
                await using var wait=new OracleCommand("SELECT COUNT(*) FROM V$SESSION WHERE BLOCKING_SESSION=:Sid AND EVENT='enq: TX - row lock contention'",verifier){BindByName=true};
                wait.Parameters.Add("Sid",OracleDbType.Int32).Value=blocker;blocked=Convert.ToInt32(await wait.ExecuteScalarAsync())>0;
                if(!blocked)await Task.Delay(100);
            } while(!blocked&&DateTime.UtcNow<deadline);
            Assert.IsTrue(blocked,"Actual Oracle contender must wait on the original operation's transaction.");
            Assert.IsFalse(write!.IsCompleted);Assert.IsFalse(claim!.IsCompleted);
            provider.Release.TrySetResult();await Task.WhenAll(write,claim).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.IsFalse(write.Result.IsError,write.Result.Exception?.ToString());
            if(writerFirst) {
                Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",claim.Result.ErrorCode);
                Assert.IsNull(claim.Result.Result);
            } else {
                Assert.IsFalse(claim.Result.IsError,claim.Result.Exception?.ToString());
                Assert.AreEqual(candidate.Records[0].Envelope,claim.Result.Result.Records[0].Envelope);
            }
            Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM OASIS_WRITE_OPERATION_LOCKS WHERE OPERATION_ID=:Id")));
            var receipt=(await Scalar("SELECT RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id"))!.ToString()!;
            Assert.AreEqual(root.Id,ConditionalHolonReceipt.Read(receipt,ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS)).Single().Id);
            string json;
            await using(var read=new OracleCommand("SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:Id",verifier){BindByName=true}) {
                read.Parameters.Add("Id",OracleDbType.Raw).Value=root.Id.ToByteArray();json=(await read.ExecuteScalarAsync())!.ToString()!;
                using var document=JsonDocument.Parse(json);Assert.AreEqual(root.Id,document.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(root.Name,document.RootElement.GetProperty("Name").GetString());Assert.AreEqual(1,document.RootElement.GetProperty("Version").GetInt32());
            }
            await using(var transaction=(OracleTransaction)await verifier.BeginTransactionAsync()) {
                await using(var remove=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Payload)=0",verifier){BindByName=true,Transaction=transaction}) {
                    remove.Parameters.Add("Id",OracleDbType.Raw).Value=root.Id.ToByteArray();remove.Parameters.Add("Payload",OracleDbType.Clob).Value=json;
                    Assert.AreEqual(1,await remove.ExecuteNonQueryAsync());
                }
                await using(var remove=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash AND DBMS_LOB.COMPARE(RESULT_JSON,:Payload)=0",verifier){BindByName=true,Transaction=transaction}) {
                    remove.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();
                    remove.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(request,ProviderType.OracleDBOASIS).Fingerprint;
                    remove.Parameters.Add("Payload",OracleDbType.Clob).Value=receipt;Assert.AreEqual(1,await remove.ExecuteNonQueryAsync());
                }
                await using(var remove=new OracleCommand("DELETE FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id AND ACTOR_ID=:Actor AND DBMS_LOB.COMPARE(PAYLOAD_JSON,:Payload)=0",verifier){BindByName=true,Transaction=transaction}) {
                    remove.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();remove.Parameters.Add("Actor",OracleDbType.Raw).Value=actor.ToByteArray();
                    remove.Parameters.Add("Payload",OracleDbType.Clob).Value=ProtectedPreparationJson.Write(candidate);
                    Assert.AreEqual(writerFirst?0:1,await remove.ExecuteNonQueryAsync());
                }
                await transaction.CommitAsync();
            }
            foreach(var table in new[]{"OASIS_WRITE_PREPARATIONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS"})
                Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM "+table+" WHERE OPERATION_ID=:Id")));
            await using(var absent=new OracleCommand("SELECT COUNT(*) FROM OASIS_HOLONS WHERE ID=:Id",verifier){BindByName=true}) {
                absent.Parameters.Add("Id",OracleDbType.Raw).Value=root.Id.ToByteArray();Assert.AreEqual(0,Convert.ToInt32(await absent.ExecuteScalarAsync()));
            }
            File.Delete(journal);
        } finally {
            provider.Release.TrySetResult();
            if(write!=null&&claim!=null)await Task.WhenAll(write,claim).WaitAsync(TimeSpan.FromSeconds(30));
            else if(write!=null)await write.WaitAsync(TimeSpan.FromSeconds(30));
            else if(claim!=null)await claim.WaitAsync(TimeSpan.FromSeconds(30));
            await provider.DeActivateProviderAsync();
        }
    }
}
