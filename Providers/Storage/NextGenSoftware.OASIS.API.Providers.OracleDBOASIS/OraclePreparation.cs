using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS : IHolonWritePreparationStorageProvider
{
    // All conditional mutation and preparation owners take this SAME per-operation row lock.
    // No table-wide lock, advisory-lock privilege, process mutex or guessed absence.
    private static async Task AcquireWriteOperationLockAsync(OracleConnection connection,OracleTransaction transaction,Guid operation,CancellationToken token)
    {
        try {
            await using var insert=CreateCommand("INSERT INTO OASIS_WRITE_OPERATION_LOCKS(OPERATION_ID) VALUES(:Id)",connection);
            insert.Transaction=transaction;insert.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();
            if(await insert.ExecuteNonQueryAsync(token)!=1)throw new InvalidDataException("Oracle operation guard did not insert exactly one row.");
        } catch(OracleException cause) when(cause.Number==1) {
            // Oracle rolls back the failed statement, not the transaction. A concurrent
            // insert's uniqueness wait resolves before selecting its committed lock row.
        }
        await using var read=CreateCommand("SELECT OPERATION_ID FROM OASIS_WRITE_OPERATION_LOCKS WHERE OPERATION_ID=:Id FOR UPDATE",connection);
        read.Transaction=transaction;read.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();
        var value=await read.ExecuteScalarAsync(token);
        if(value is not byte[] bytes||new Guid(bytes)!=operation)throw new InvalidDataException("Oracle operation guard identity differs from its requested operation.");
    }
    public OASISResult<HolonWritePreparation> ClaimHolonWritePreparation(HolonWritePreparation candidate)
        =>ClaimHolonWritePreparationAsync(candidate).GetAwaiter().GetResult();
    public async Task<OASISResult<HolonWritePreparation>> ClaimHolonWritePreparationAsync(
        HolonWritePreparation candidate,CancellationToken cancellationToken=default)
    {
        var commitEntered=false;
        try {
            cancellationToken.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(candidate);
            if(candidate.Authority!=Core.Enums.ProviderType.OracleDBOASIS)
                return PreparationFailure("COSMIC_AUTHORITY_MISMATCH","The preparation belongs to another authority.");
            var payload=ProtectedPreparationJson.Write(candidate);
            await using var connection=new OracleConnection(_connectionString);await connection.OpenAsync(cancellationToken);
            await using var transaction=(OracleTransaction)await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,cancellationToken);
            await AcquireWriteOperationLockAsync(connection,transaction,candidate.OperationId,cancellationToken);
            await using(var read=CreateCommand("SELECT ACTOR_ID,DBMS_LOB.GETLENGTH(PAYLOAD_JSON),PAYLOAD_JSON FROM OASIS_WRITE_PREPARATIONS WHERE OPERATION_ID=:Id",connection)) {
                read.Transaction=transaction;read.Parameters.Add("Id",OracleDbType.Raw).Value=candidate.OperationId.ToByteArray();
                await using var reader=await read.ExecuteReaderAsync(cancellationToken);
                if(await reader.ReadAsync(cancellationToken)) {
                    if(new Guid((byte[])reader.GetValue(0))!=candidate.ActorId)
                        return PreparationFailure("COSMIC_PREPARATION_ACTOR_MISMATCH","The operation is owned by another actor.");
                    if(reader.IsDBNull(1)||reader.GetInt64(1)>ProtectedPreparationJson.MaximumPayloadCharacters)
                        throw new InvalidDataException("Oracle protected preparation exceeds its transport limit.");
                    var winner=ProtectedPreparationJson.Read(candidate,reader.GetString(2));
                    // This is an acknowledged read, not a new claim or holon mutation.
                    return new(winner);
                }
            }
            await using(var receipt=CreateCommand("SELECT COUNT(*) FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",connection)) {
                receipt.Transaction=transaction;receipt.Parameters.Add("Id",OracleDbType.Raw).Value=candidate.OperationId.ToByteArray();
                if(Convert.ToInt32(await receipt.ExecuteScalarAsync(cancellationToken))!=0)
                    return PreparationFailure("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",
                        "This operation has a receipt but no original protected preparation.",
                        new OraclePreparationReconciliationException(candidate.OperationId,new InvalidDataException("Original protected preparation is unavailable.")));
            }
            await using(var insert=CreateCommand("INSERT INTO OASIS_WRITE_PREPARATIONS(OPERATION_ID,ACTOR_ID,PAYLOAD_JSON) VALUES(:Id,:Actor,:Payload)",connection)) {
                insert.Transaction=transaction;insert.Parameters.Add("Id",OracleDbType.Raw).Value=candidate.OperationId.ToByteArray();
                insert.Parameters.Add("Actor",OracleDbType.Raw).Value=candidate.ActorId.ToByteArray();
                insert.Parameters.Add("Payload",OracleDbType.Clob).Value=payload;
                if(await insert.ExecuteNonQueryAsync(cancellationToken)!=1)throw new InvalidDataException("Oracle preparation did not insert exactly one row.");
            }
            cancellationToken.ThrowIfCancellationRequested();commitEntered=true;await CommitLifecycleTransactionAsync(transaction);
            return new(candidate);
        } catch(OracleException cause) when(!commitEntered&&cause.Number==1013&&cancellationToken.IsCancellationRequested) {
            return PreparationFailure("COSMIC_PREPARATION_CANCELLED","Oracle protected preparation was cancelled.",
                new OperationCanceledException("Oracle protected preparation was cancelled.",cause,cancellationToken));
        } catch(Exception cause) {
            return PreparationFailure(commitEntered?"COSMIC_PREPARATION_RECONCILIATION_REQUIRED":
                cause is OperationCanceledException?"COSMIC_PREPARATION_CANCELLED":"COSMIC_PREPARATION_FAILED",
                "Oracle protected preparation claim failed.",commitEntered?
                    new OraclePreparationReconciliationException(candidate?.OperationId??Guid.Empty,cause):cause);
        }
    }
    private static OASISResult<HolonWritePreparation> PreparationFailure(string code,string message,Exception? cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}

public sealed class OraclePreparationReconciliationException:Exception,INativeMutationReconciliationRequired
{
    public Guid OperationId{get;}public string OriginalTransactionId=>string.Empty;
    public OraclePreparationReconciliationException(Guid operationId,Exception cause)
        :base("Recover the original Oracle preparation before another holon mutation.",cause)=>OperationId=operationId;
}
