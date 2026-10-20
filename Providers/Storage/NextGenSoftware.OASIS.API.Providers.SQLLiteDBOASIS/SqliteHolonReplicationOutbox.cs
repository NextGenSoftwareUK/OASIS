using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

/// <summary>Lease-fenced delivery state for the existing atomic SQLite holon outbox. Does not initialize a database.</summary>
public sealed partial class SqliteHolonReplicationOutbox : IHolonReplicationReadinessOutbox
{
    private readonly string _connectionString;
    private readonly IHyperDriveClock _clock;
    public SqliteHolonReplicationOutbox(string connectionString,IHyperDriveClock clock=null)
    {
        if(string.IsNullOrWhiteSpace(connectionString))throw new ArgumentException("An existing SQLite connection is required.",nameof(connectionString));
        var builder=new SqliteConnectionStringBuilder(connectionString);
        if(string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource==":memory:" || builder.Mode==SqliteOpenMode.Memory || builder.Mode==SqliteOpenMode.ReadOnly)
            throw new ArgumentException("A durable writable SQLite database is required.",nameof(connectionString));
        builder.Mode=SqliteOpenMode.ReadWrite;
        _connectionString=builder.ToString();_clock=clock ?? new SystemHyperDriveClock();
    }

    public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId,string workerId,TimeSpan leaseDuration,CancellationToken cancellationToken=default)
        =>Execute(()=>Claim(targetId,workerId,leaseDuration,cancellationToken),cancellationToken);

    private OASISResult<HolonDeliveryClaim> Claim(string targetId,string workerId,TimeSpan leaseDuration,CancellationToken token)
    {
        ValidateIdentity(targetId);ValidateIdentity(workerId);ValidateDuration(leaseDuration);
        using var connection=Open();using var transaction=connection.BeginTransaction(deferred:false);
        var now=Now();var until=now.Add(leaseDuration);
        long sequence;Guid operation;string payload;int attempts;
        using(var query=connection.CreateCommand())
        {
            query.Transaction=transaction;
            query.CommandText="SELECT Sequence,OperationId,PayloadJson,PayloadDigest,AttemptCount,DeliveryState,LeaseUntilUtcTicks,NextAttemptUtcTicks,SourceId,SourceAuthority,ActorId FROM CosmicHolonOutbox WHERE TargetId=$target AND DeliveryState<>'Completed' ORDER BY Sequence LIMIT 1";
            query.Parameters.AddWithValue("$target",targetId);using var reader=query.ExecuteReader();
            if(!reader.Read())return new OASISResult<HolonDeliveryClaim>{Message="No unfinished obligation exists for this target."};
            var state=reader.GetString(5);
            if(state=="ReconciliationRequired")return new OASISResult<HolonDeliveryClaim>{Message="The oldest target obligation requires reconciliation."};
            if(state!="Pending" && state!="InFlight")throw new InvalidDataException("Invalid outbox delivery state.");
            if(state=="InFlight" && reader.GetInt64(6)>now.Ticks || reader.GetInt64(7)>now.Ticks)
                return new OASISResult<HolonDeliveryClaim>{Message="The oldest target obligation is leased or not yet due."};
            sequence=reader.GetInt64(0);operation=Guid.Parse(reader.GetString(1));payload=reader.GetString(2);
            ValidateFrozenPayload(payload,reader.GetString(3),operation,reader.GetString(8),reader.GetString(9),reader.GetString(10));
            if(reader.GetInt32(4)<0)throw new InvalidDataException("Invalid outbox attempt count.");
            attempts=checked(reader.GetInt32(4)+1);
        }
        var lease=Guid.NewGuid();using var command=connection.CreateCommand();command.Transaction=transaction;
        command.CommandText="UPDATE CosmicHolonOutbox SET DeliveryState='InFlight',LeaseToken=$lease,LeaseWorkerId=$worker,LeaseUntilUtcTicks=$until,AttemptCount=$attempts WHERE Sequence=$sequence";
        command.Parameters.AddWithValue("$lease",lease.ToString("D"));command.Parameters.AddWithValue("$worker",workerId);
        command.Parameters.AddWithValue("$until",until.Ticks);command.Parameters.AddWithValue("$attempts",attempts);command.Parameters.AddWithValue("$sequence",sequence);
        if(command.ExecuteNonQuery()!=1)throw new InvalidDataException("The selected outbox obligation disappeared.");
        token.ThrowIfCancellationRequested();transaction.Commit();
        return new OASISResult<HolonDeliveryClaim>(new HolonDeliveryClaim(sequence,operation,targetId,workerId,lease,payload,attempts,until));
    }

    public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim,TimeSpan leaseDuration,CancellationToken cancellationToken=default)
        =>Execute(()=>{
            ValidateClaim(claim);ValidateDuration(leaseDuration);
            using var connection=Open();using var transaction=connection.BeginTransaction(deferred:false);
            var now=Now();var until=now.Add(leaseDuration);using var command=OwnedCommand(connection,claim,now);command.Transaction=transaction;
            command.CommandText="UPDATE CosmicHolonOutbox SET LeaseUntilUtcTicks=MAX(LeaseUntilUtcTicks,$until) "+command.CommandText;
            command.Parameters.AddWithValue("$until",until.Ticks);
            var result=OwnedResult(command.ExecuteNonQuery());cancellationToken.ThrowIfCancellationRequested();transaction.Commit();return result;
        },cancellationToken);

    public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,string errorCode=null,
        string message=null,DateTime? nextAttemptUtc=null,CancellationToken cancellationToken=default)
        =>FinishCoreAsync(claim,outcome,null,errorCode,message,nextAttemptUtc,cancellationToken);

    public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,
        HolonDeliveryRecoveryEvidence recoveryEvidence,string errorCode=null,string message=null,DateTime? nextAttemptUtc=null,
        CancellationToken cancellationToken=default)
        =>FinishCoreAsync(claim,outcome,recoveryEvidence ?? throw new ArgumentNullException(nameof(recoveryEvidence)),errorCode,message,nextAttemptUtc,cancellationToken);

    private Task<OASISResult<bool>> FinishCoreAsync(HolonDeliveryClaim claim,HolonDeliveryOutcome outcome,
        HolonDeliveryRecoveryEvidence recoveryEvidence,string errorCode,string message,DateTime? nextAttemptUtc,CancellationToken cancellationToken)
        =>Execute(()=>{
            ValidateClaim(claim);
            if(recoveryEvidence?.RequiresNativeReconciliation==true && outcome!=HolonDeliveryOutcome.ReconciliationRequired)
                throw new ArgumentException("Native reconciliation evidence cannot be completed or retried automatically.");
            var evidenceJson=recoveryEvidence==null ? null:HolonRecoveryEvidenceJson.Write(recoveryEvidence);
            using var connection=Open();using var transaction=connection.BeginTransaction(deferred:false);
            var now=Now();
            if(!Enum.IsDefined(typeof(HolonDeliveryOutcome),outcome))throw new ArgumentException("A defined delivery outcome is required.");
            if(outcome==HolonDeliveryOutcome.Completed && (errorCode!=null || message!=null || nextAttemptUtc!=null)
                || outcome!=HolonDeliveryOutcome.Completed && (string.IsNullOrWhiteSpace(errorCode) || errorCode.Length>128 || message?.Length>4096)
                || outcome==HolonDeliveryOutcome.Retry && (!nextAttemptUtc.HasValue || nextAttemptUtc.Value.Kind!=DateTimeKind.Utc || nextAttemptUtc.Value<=now)
                || outcome==HolonDeliveryOutcome.ReconciliationRequired && nextAttemptUtc.HasValue)
                throw new ArgumentException("Delivery diagnostics and retry time must match the declared outcome.");
            using var command=OwnedCommand(connection,claim,now);command.Transaction=transaction;
            command.CommandText="UPDATE CosmicHolonOutbox SET DeliveryState=$state,LeaseToken='',LeaseWorkerId='',LeaseUntilUtcTicks=0,NextAttemptUtcTicks=$next,LastErrorCode=$code,LastErrorMessage=$message,CompletedAtUtcTicks=$completed,RecoveryEvidenceJson=$evidence,RecoveryEvidenceDigest=$digest "+command.CommandText;
            command.Parameters.AddWithValue("$evidence",(object)evidenceJson ?? DBNull.Value);
            command.Parameters.AddWithValue("$digest",evidenceJson==null ? DBNull.Value:(object)HolonRecoveryEvidenceJson.Digest(evidenceJson));
            command.Parameters.AddWithValue("$state",outcome==HolonDeliveryOutcome.Retry ? "Pending":outcome.ToString());
            command.Parameters.AddWithValue("$next",nextAttemptUtc?.Ticks ?? 0);
            command.Parameters.AddWithValue("$code",(object)errorCode ?? DBNull.Value);command.Parameters.AddWithValue("$message",(object)message ?? DBNull.Value);
            command.Parameters.AddWithValue("$completed",outcome==HolonDeliveryOutcome.Completed ? now.Ticks:0);
            var result=OwnedResult(command.ExecuteNonQuery());cancellationToken.ThrowIfCancellationRequested();transaction.Commit();return result;
        },cancellationToken);

    private static void ValidateFrozenPayload(string payload,string digest,Guid operation,string source,string authority,string actor)
    {
        if(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))!=digest)
            throw new InvalidDataException("The frozen outbox payload digest differs.");
        var delivery=CommittedHolonReplicaPayload.Read(payload);
        if(delivery.OperationId!=operation || delivery.SourceId!=source || delivery.SourceAuthority!=ProviderType.SQLLiteDBOASIS
            || authority!=ProviderType.SQLLiteDBOASIS.ToString() || delivery.ActorId.ToString("D")!=actor)
            throw new InvalidDataException("The frozen outbox payload disagrees with its enrollment.");
    }

    private static SqliteCommand OwnedCommand(SqliteConnection connection,HolonDeliveryClaim claim,DateTime now)
    {
        var command=connection.CreateCommand();
        command.CommandText="WHERE Sequence=$sequence AND OperationId=$operation AND TargetId=$target AND DeliveryState='InFlight' AND LeaseToken=$lease AND LeaseWorkerId=$worker AND LeaseUntilUtcTicks>$now";
        command.Parameters.AddWithValue("$sequence",claim.Sequence);command.Parameters.AddWithValue("$operation",claim.OperationId.ToString("D"));
        command.Parameters.AddWithValue("$target",claim.TargetId);command.Parameters.AddWithValue("$lease",claim.LeaseToken.ToString("D"));
        command.Parameters.AddWithValue("$worker",claim.WorkerId);command.Parameters.AddWithValue("$now",now.Ticks);return command;
    }
    private static OASISResult<bool> OwnedResult(int count)=>count==1 ? new OASISResult<bool>(true)
        :new OASISResult<bool>{IsError=true,ErrorCount=1,ErrorCode="COSMIC_OUTBOX_LEASE_LOST",Message="The obligation is no longer owned by this unexpired lease."};
    private SqliteConnection Open(){var connection=new SqliteConnection(_connectionString);try{connection.Open();return connection;}catch{connection.Dispose();throw;}}
    private DateTime Now(){var now=_clock.UtcNow;if(now.Kind!=DateTimeKind.Utc)throw new InvalidOperationException("The outbox clock must return UTC.");return now;}
    private static void ValidateDuration(TimeSpan duration){if(duration<=TimeSpan.Zero || duration>TimeSpan.FromHours(1))throw new ArgumentOutOfRangeException(nameof(duration),"A positive lease of at most one hour is required.");}
    private static void ValidateIdentity(string value){if(string.IsNullOrWhiteSpace(value)||value!=value.Trim()||value.Length>256)throw new ArgumentException("A stable identity of at most 256 characters is required.");}
    private static void ValidateClaim(HolonDeliveryClaim claim)
    {
        if(claim==null || claim.Sequence<=0 || claim.OperationId==Guid.Empty || claim.LeaseToken==Guid.Empty)throw new ArgumentException("A valid fenced delivery claim is required.");
        ValidateIdentity(claim.TargetId);ValidateIdentity(claim.WorkerId);
    }
    private static Task<OASISResult<T>> Execute<T>(Func<OASISResult<T>> action,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try{return Task.FromResult(action());}
        catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}
        catch(Exception error){return Task.FromResult(new OASISResult<T>{IsError=true,ErrorCount=1,ErrorCode=error is InvalidDataException ? "COSMIC_OUTBOX_CORRUPT":"COSMIC_OUTBOX_OPERATION_FAILED",Message=error.Message,Exception=error});}
    }
}
