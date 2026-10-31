using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public partial class OraclePreparationNativeTests
{
    private sealed class Owner:OracleDBOASIS
    {
        public int Commits;public bool LoseAck;
        public Owner(string connection):base(connection){}
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction) {
            await base.CommitLifecycleTransactionAsync(transaction);Commits++;
            if(LoseAck){LoseAck=false;throw new IOException("owned ACK loss after Oracle preparation commit");}
        }
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeOpaqueClaimOwnershipAndReceiptCutover(bool synchronous,bool loseAck)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"preparation-{synchronous}-{loseAck}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var op=Guid.NewGuid();var blocked=Guid.NewGuid();var actor=Guid.NewGuid();var id=Guid.NewGuid();
        HolonWritePreparation Candidate(Guid operation,Guid owner,string suffix)=>new(operation,owner,ProviderType.OracleDBOASIS,
            new[]{new ProtectedHolonPreparation(id,"oasis-preparation1:owned-opaque-"+suffix)});
        var provider=new Owner(connection){LoseAck=loseAck};
        await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
        async Task<object?> Read(string sql,Guid operation) {
            await using var command=new OracleCommand(sql,verifier){BindByName=true};
            command.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();return await command.ExecuteScalarAsync();
        }
        async Task Count(string table,Guid operation,int expected)=>Assert.AreEqual(expected,
            Convert.ToInt32(await Read("SELECT COUNT(*) FROM "+table+" WHERE OPERATION_ID=:Id",operation)));
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var operation in new[]{op,blocked})foreach(var table in new[]{"OASIS_WRITE_PREPARATIONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS"})
                await Count(table,operation,0);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{op,blocked,actor,id,state="before-opaque-oracle-claim"}));file.Flush(true);
            }
            var candidate=Candidate(op,actor,"first");
            var first=synchronous?provider.ClaimHolonWritePreparation(candidate):await provider.ClaimHolonWritePreparationAsync(candidate);
            if(loseAck) {
                Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",first.ErrorCode);
                Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(first.Exception);
                first=await provider.ClaimHolonWritePreparationAsync(Candidate(op,actor,"different"));
            }
            Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.AreEqual(candidate.Records[0].Envelope,first.Result.Records[0].Envelope);
            Assert.AreEqual(1,provider.Commits);
            foreach(var operation in new[]{op,blocked}) {
                await using var insert=new OracleCommand("INSERT INTO OASIS_WRITE_RECEIPTS(OPERATION_ID,REQUEST_HASH,RESULT_JSON) VALUES(:Id,:Hash,:Json)",verifier){BindByName=true};
                insert.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();insert.Parameters.Add("Hash",OracleDbType.Varchar2).Value=new string('A',64);
                insert.Parameters.Add("Json",OracleDbType.Clob).Value="owned receipt gate";Assert.AreEqual(1,await insert.ExecuteNonQueryAsync());
            }
            var replay=await provider.ClaimHolonWritePreparationAsync(Candidate(op,actor,"must-not-replace"));
            Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(candidate.Records[0].Envelope,replay.Result.Records[0].Envelope);
            var denied=await provider.ClaimHolonWritePreparationAsync(Candidate(op,Guid.NewGuid(),"other-actor"));
            Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",denied.ErrorCode);Assert.IsNull(denied.Result);
            var refused=await provider.ClaimHolonWritePreparationAsync(Candidate(blocked,actor,"must-not-claim"));
            Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",refused.ErrorCode);
            Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(refused.Exception);Assert.IsNull(refused.Result);
            await Count("OASIS_WRITE_PREPARATIONS",blocked,0);await Count("OASIS_WRITE_OPERATION_LOCKS",blocked,0);
            Assert.AreEqual(1,provider.Commits);
            var payload=(await Read("SELECT PAYLOAD_JSON FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id",op))!.ToString()!;
            Assert.AreEqual(candidate.Records[0].Envelope,ProtectedPreparationJson.Read(candidate,payload).Records[0].Envelope);
            await using(var transaction=(OracleTransaction)await verifier.BeginTransactionAsync()) {
                await using(var remove=new OracleCommand("DELETE FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id AND ACTOR_ID=:Actor AND DBMS_LOB.COMPARE(PAYLOAD_JSON,:Payload)=0",verifier){BindByName=true,Transaction=transaction}) {
                    remove.Parameters.Add("Id",OracleDbType.Raw).Value=op.ToByteArray();remove.Parameters.Add("Actor",OracleDbType.Raw).Value=actor.ToByteArray();
                    remove.Parameters.Add("Payload",OracleDbType.Clob).Value=payload;Assert.AreEqual(1,await remove.ExecuteNonQueryAsync());
                }
                foreach(var operation in new[]{op,blocked}) {
                    await using var remove=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash AND DBMS_LOB.COMPARE(RESULT_JSON,:Payload)=0",verifier){BindByName=true,Transaction=transaction};
                    remove.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();remove.Parameters.Add("Hash",OracleDbType.Varchar2).Value=new string('A',64);
                    remove.Parameters.Add("Payload",OracleDbType.Clob).Value="owned receipt gate";Assert.AreEqual(1,await remove.ExecuteNonQueryAsync());
                }
                await using(var remove=new OracleCommand("DELETE FROM OASIS_WRITE_OPERATION_LOCKS WHERE OPERATION_ID=:Id",verifier){BindByName=true,Transaction=transaction}) {
                    remove.Parameters.Add("Id",OracleDbType.Raw).Value=op.ToByteArray();Assert.AreEqual(1,await remove.ExecuteNonQueryAsync());
                }
                await transaction.CommitAsync();
            }
            foreach(var operation in new[]{op,blocked})foreach(var table in new[]{"OASIS_WRITE_PREPARATIONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS"})
                await Count(table,operation,0);
            File.Delete(journal);
        } finally {await provider.DeActivateProviderAsync();}
    }
}
