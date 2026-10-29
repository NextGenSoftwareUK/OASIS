using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using HeadAuditProviderType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : IHolonReplicaHeadAuditStorageProvider
{
    public async Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(
        HolonReplicaHeadReadRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != HeadAuditProviderType.Neo4jOASIS)
            return HeadAuditError("COSMIC_REPLICA_HEAD_AUDIT_OPTIONS_INVALID", "An exact Neo physical binding and bounded read intent are required.");
        // Native read-committed transactions are not repeatable snapshots. Do not
        // acquire write locks or downgrade a strict request to make an audit pass.
        if (request.RequiredConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot)
            return HeadAuditError("COSMIC_REPLICA_HEAD_AUDIT_CONSISTENCY_UNSUPPORTED",
                "This read-only Neo audit requires explicit ReadCommittedValidated opt-in; NativeSnapshot is unsupported.");
        await _lifecycleGate.WaitAsync(token).ConfigureAwait(false);
        IAsyncSession session = null; IAsyncTransaction transaction = null;
        OASISResult<HolonReplicaHeadReadResult> result = null;
        Exception failure = null; bool cancelled = false; string code = "COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED";
        try
        {
            if (!IsProviderActivated || _driver == null || _storageDriverEndpoint == null)
                result = HeadAuditError("COSMIC_REPLICA_HEAD_AUDIT_NOT_READY", "An activated driver with its captured native endpoint is required.");
            else
            {
                session = _driver.AsyncSession(options => options.WithDefaultAccessMode(AccessMode.Read));
                transaction = await session.BeginTransactionAsync().ConfigureAwait(false);
                await ValidateNativeStorageBindingAsync(transaction, request.ExpectedStorage, _storageDriverEndpoint,
                    token, "COSMIC_REPLICA_HEAD_AUDIT_STORAGE").ConfigureAwait(false);
                var cursor = await transaction.RunAsync(
                    "UNWIND $ids AS requestedId OPTIONAL MATCH (r:CosmicReplicaHead {HolonId:requestedId}) "
                    + "WITH requestedId,collect(r) AS heads OPTIONAL MATCH (h:Holon {Id:requestedId}) "
                    + "RETURN requestedId,heads,collect(h) AS records",
                    new { ids = request.HolonIds.Select(id => id.ToString("D")).ToArray() }).ConfigureAwait(false);
                var observed = new Dictionary<Guid, HolonReplicaHeadObservation>();
                while (await cursor.FetchAsync().ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    var row = cursor.Current;
                    if (row["requestedId"] is not string text || !Guid.TryParseExact(text,"D",out var id)
                        || !request.HolonIds.Contains(id) || observed.ContainsKey(id) || observed.Count >= request.HolonIds.Count)
                        throw new InvalidDataException("The native replica observation identity set is invalid.");
                    var heads = row["heads"].As<List<Neo4j.Driver.INode>>();
                    var records = row["records"].As<List<Neo4j.Driver.INode>>();
                    if (heads.Count > 1 || records.Count > 1)
                        throw new InvalidDataException("The native replica head or current identity is not unique.");
                    var head = heads.Count == 0 ? null : ReadProtectedReplicaHead(heads[0], id).Evidence;
                    var current = records.Count == 0 ? null : ReadHolonNode(records[0]);
                    if (head != null)
                        NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonReplicaHeadRecord.ValidateCurrent(
                            id, HeadAuditProviderType.Neo4jOASIS, current, head);
                    observed.Add(id,new(id,current != null,head));
                }
                if (observed.Count != request.HolonIds.Count)
                    throw new InvalidDataException("The native audit omitted a requested identity.");
                token.ThrowIfCancellationRequested();
                result = new(new(request.ExpectedStorage, request.HolonIds.Select(id => observed[id]),
                    HolonReplicaHeadReadConsistency.ReadCommittedValidated)) { IsLoaded = true };
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
            result.Message="Native evidence was observed, but read-only scope cleanup failed.";
            return result;
        }
        if(cancelled)throw new OperationCanceledException("Head evidence read cancelled; cleanup evidence is retained.",failure,token);
        return HeadAuditError(code,"Protected target evidence could not be verified; no partial observations are returned.",failure);
    }
    private static OASISResult<HolonReplicaHeadReadResult> HeadAuditError(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}
