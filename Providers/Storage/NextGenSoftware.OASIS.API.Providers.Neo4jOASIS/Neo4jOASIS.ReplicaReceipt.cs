using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using HeadAuditProviderType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICommittedHolonReplicaReceiptStorageProvider
{
    public async Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != HeadAuditProviderType.Neo4jOASIS)
            return ReplicaReceiptError("COSMIC_REPLICA_RECEIPT_OPTIONS_INVALID", "An exact Neo physical binding and bounded read intent are required.");
        // Native read-committed transactions are not repeatable snapshots. Do not
        // acquire write locks or downgrade a strict request to make an audit pass.
        if (request.RequiredConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot)
            return ReplicaReceiptError("COSMIC_REPLICA_RECEIPT_CONSISTENCY_UNSUPPORTED",
                "This read-only Neo audit requires explicit ReadCommittedValidated opt-in; NativeSnapshot is unsupported.");
        await _lifecycleGate.WaitAsync(token).ConfigureAwait(false);
        IAsyncSession session = null; IAsyncTransaction transaction = null;
        OASISResult<CommittedHolonReplicaReceiptEvidence> result = null;
        Exception failure = null; bool cancelled = false; string code = "COSMIC_REPLICA_RECEIPT_UNVERIFIED";
        try
        {
            var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
            if (!IsProviderActivated || _driver == null || _storageDriverEndpoint == null)
                result = ReplicaReceiptError("COSMIC_REPLICA_RECEIPT_NOT_READY", "An activated driver with its captured native endpoint is required.");
            else
            {
                session = _driver.AsyncSession(options => options.WithDefaultAccessMode(AccessMode.Read));
                transaction = await session.BeginTransactionAsync().ConfigureAwait(false);
                await ValidateNativeStorageBindingAsync(transaction, request.ExpectedStorage, _storageDriverEndpoint,
                    token, "COSMIC_REPLICA_RECEIPT_STORAGE").ConfigureAwait(false);
                var receipts = await (await transaction.RunAsync(
                    "MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r LIMIT 2",
                    new { operation = plan.OperationId.ToString("D") }).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
                if (receipts.Count > 1) throw new InvalidDataException("Historical receipt identity is not unique.");
                string fingerprint = null, acknowledgement = null;
                if (receipts.Count == 1)
                {
                    var receipt = receipts[0]["r"].As<Neo4j.Driver.INode>();
                    if (!receipt.Properties.TryGetValue("Fingerprint", out var f) || f is not string
                        || !receipt.Properties.TryGetValue("ResultJson", out var json) || json is not string)
                        throw new InvalidDataException("Historical receipt is incomplete.");
                    fingerprint = (string)f; acknowledgement = (string)json;
                }
                var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan, fingerprint, acknowledgement,
                    HolonReplicaHeadReadConsistency.ReadCommittedValidated);
                token.ThrowIfCancellationRequested();
                result = new(evidence) { IsLoaded = true };
            }
        }
        catch (Exception cause)
        {
            failure = cause; cancelled = cause is OperationCanceledException && token.IsCancellationRequested;
            if (cause is NeoReplicaStorageBindingException binding) code = binding.Code;
        }
        finally
        {
            try { if(transaction != null)await transaction.RollbackAsync().ConfigureAwait(false); }
            catch(Exception cause){failure=failure==null ? cause:new AggregateException(failure,cause);}
            try { transaction?.Dispose(); }
            catch(Exception cause){failure=failure==null ? cause:new AggregateException(failure,cause);}
            try { session?.Dispose(); }
            catch(Exception cause){failure=failure==null ? cause:new AggregateException(failure,cause);}
            _lifecycleGate.Release();
        }
        if(failure==null)return result;
        if(result != null && !result.IsError)
        {
            result.IsWarning=true;result.WarningCount=1;result.Exception=failure;
            result.Message="Historical receipt evidence was observed, but read-only scope cleanup failed.";
            return result;
        }
        if(cancelled)throw new OperationCanceledException("Historical receipt read cancelled; cleanup evidence is retained.",failure,token);
        return ReplicaReceiptError(code,"Historical delivery could not be verified; no mutation was attempted.",failure);
    }
    private static OASISResult<CommittedHolonReplicaReceiptEvidence> ReplicaReceiptError(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}
