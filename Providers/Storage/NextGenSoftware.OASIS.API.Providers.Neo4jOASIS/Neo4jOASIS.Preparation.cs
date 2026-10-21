using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed class Neo4jPreparationReconciliationException(Guid operationId,Exception cause)
    : Exception("Reconcile the original Neo4j preparation before dispatching another mutation.",cause),INativeMutationReconciliationRequired
{
    public Guid OperationId { get; }=operationId;
    public string OriginalTransactionId=>string.Empty;
}

public partial class Neo4jOASIS : IHolonWritePreparationStorageProvider
{
    public OASISResult<HolonWritePreparation> ClaimHolonWritePreparation(HolonWritePreparation candidate)
        =>ClaimHolonWritePreparationAsync(candidate).GetAwaiter().GetResult();

    public async Task<OASISResult<HolonWritePreparation>> ClaimHolonWritePreparationAsync(
        HolonWritePreparation candidate,CancellationToken cancellationToken=default)
    {
        var result=new OASISResult<HolonWritePreparation>();
        if(cancellationToken.IsCancellationRequested)return PreparationFailure(result,"COSMIC_PREPARATION_CANCELLED",new OperationCanceledException(cancellationToken));
        if(candidate==null)return PreparationFailure(result,"COSMIC_PREPARATION_INVALID",new ArgumentNullException(nameof(candidate)));
        if(candidate.Authority!=Core.Enums.ProviderType.Neo4jOASIS)
            return PreparationFailure(result,"COSMIC_AUTHORITY_MISMATCH",new ArgumentException("Preparation belongs to another authority."));
        if(!IsProviderActivated||_driver==null)
            return PreparationFailure(result,"COSMIC_PROVIDER_NOT_READY",new InvalidOperationException("Activate Neo4j before claiming preparation."));

        IAsyncSession session=null;IAsyncTransaction transaction=null;
        var commitDispatched=false;var rollbackFailed=false;var known=false;Exception failure=null;
        try
        {
            var payload=ProtectedPreparationJson.Write(candidate);
            var claim=Guid.NewGuid().ToString("D");
            session=_driver.AsyncSession();
            transaction=await session.BeginTransactionAsync(config=>config.WithMetadata(new System.Collections.Generic.Dictionary<string,object>{
                ["COSMIC.OperationId"]=candidate.OperationId.ToString("D")})).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            // The native receipt uniqueness constraint and node write lock serialize preparation
            // and conditional mutation. Preparation fields survive acknowledgement publication.
            var row=await(await transaction.RunAsync("MERGE (r:CosmicWriteReceipt {OperationId:$operation}) "
                +"ON CREATE SET r.__cosmicPreparationClaim=$claim SET r.__cosmicLock=true REMOVE r.__cosmicLock RETURN r",
                new{operation=candidate.OperationId.ToString("D"),claim}).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            var node=row["r"].As<Neo4j.Driver.INode>();
            if(node.Properties.ContainsKey("PreparationJson"))
            {
                RequirePreparationOwner(node,candidate.ActorId);
                result.Result=ProtectedPreparationJson.Read(candidate,(string)node.Properties["PreparationJson"]);
                known=true;
                await transaction.RollbackAsync().ConfigureAwait(false);
            }
            else
            {
                if(node.Properties.ContainsKey("ResultJson"))
                    throw new PreparationReceiptCutoverException();
                if(!node.Properties.TryGetValue("__cosmicPreparationClaim",out var owner)||owner is not string text||text!=claim)
                    throw new InvalidDataException("The operation has neither an original preparation nor a valid new claim.");
                cancellationToken.ThrowIfCancellationRequested();
                var saved=await(await transaction.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) "
                    +"WHERE r.__cosmicPreparationClaim=$claim SET r.PreparationActorId=$actor,r.PreparationFormat=1,"
                    +"r.PreparationJson=$payload,r.PreparationCreatedAtUtc=$now REMOVE r.__cosmicPreparationClaim RETURN count(r) AS count",
                    new{operation=candidate.OperationId.ToString("D"),claim,actor=candidate.ActorId.ToString("D"),payload,now=DateTime.UtcNow.ToString("O")})
                    .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
                if(saved["count"].As<long>()!=1)throw new InvalidDataException("Preparation acknowledgement is incomplete.");
                cancellationToken.ThrowIfCancellationRequested();
                commitDispatched=true;
                await transaction.CommitAsync().ConfigureAwait(false);
                result.Result=candidate;known=true;
            }
        }
        catch(Exception cause)
        {
            failure=cause;
            if(transaction!=null&&!commitDispatched&&!known)
                try{await transaction.RollbackAsync().ConfigureAwait(false);}
                catch(Exception cleanup){rollbackFailed=true;failure=new AggregateException(cause,cleanup);}
        }
        finally
        {
            try{transaction?.Dispose();}catch(Exception cleanup){failure=failure==null?cleanup:new AggregateException(failure,cleanup);}
            try{session?.Dispose();}catch(Exception cleanup){failure=failure==null?cleanup:new AggregateException(failure,cleanup);}
        }
        if(failure==null)return result;
        if(known){result.IsWarning=true;result.WarningCount++;result.Exception=failure;result.Message="Original preparation is known, but Neo4j cleanup failed.";return result;}
        var uncertain=commitDispatched||rollbackFailed;
        return PreparationFailure(result,uncertain?"COSMIC_PREPARATION_RECONCILIATION_REQUIRED"
            :failure is PreparationReceiptCutoverException?"COSMIC_PREPARATION_RECONCILIATION_REQUIRED"
            :failure is PreparationActorException?"COSMIC_PREPARATION_ACTOR_MISMATCH"
            :failure is OperationCanceledException?"COSMIC_PREPARATION_CANCELLED"
            :failure is InvalidDataException?"COSMIC_PREPARATION_INVALID"
            :failure is TransientException {Code:"Neo.TransientError.Transaction.DeadlockDetected" or "Neo.TransientError.Transaction.LockAcquisitionTimeout"}
                ?"COSMIC_PREPARATION_CONCURRENT_CLAIM":"COSMIC_PREPARATION_FAILED",
            uncertain?new Neo4jPreparationReconciliationException(candidate.OperationId,failure):failure);
    }

    private sealed class PreparationReceiptCutoverException():InvalidOperationException("An existing receipt has no original protected preparation; operator reconciliation is required.");
    private sealed class PreparationActorException():InvalidOperationException("Preparation belongs to another actor.");

    private static void RequirePreparationOwner(Neo4j.Driver.INode node,Guid actor)
    {
        if(!node.Properties.TryGetValue("PreparationActorId",out var stored)||stored is not string owner||owner!=actor.ToString("D"))
            throw new PreparationActorException();
        if(!node.Properties.TryGetValue("PreparationFormat",out var format)||format is not long version||version!=1
            ||!node.Properties.TryGetValue("PreparationJson",out var payload)||payload is not string
            ||!node.Properties.TryGetValue("PreparationCreatedAtUtc",out var created)||created is not string timestamp
            ||!DateTime.TryParseExact(timestamp,"O",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind,out var time)
            ||time.Kind!=DateTimeKind.Utc)
            throw new InvalidDataException("Stored preparation schema is invalid.");
    }
    private static OASISResult<HolonWritePreparation> PreparationFailure(OASISResult<HolonWritePreparation> result,string code,Exception cause)
    {result.Result=null;result.IsError=true;result.ErrorCount++;result.ErrorCode=code;result.Exception=cause;result.Message=cause.Message;return result;}
}
