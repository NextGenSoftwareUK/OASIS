using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public enum HolonReplicaInitializationAuthorizationStage { ReadHeldSource, ApproveInitialization }

/// <summary>Required server-owned policy. BindingIdentity changes when grants or policy meaning change.
/// Regular replication grants do not implicitly grant initialization.</summary>
public interface IHolonReplicaInitializationPolicy
{
    string BindingIdentity {get;}
    Task<OASISResult<bool>> AuthorizeAsync(HolonReplicaInitializationAuthorizationContext context,CancellationToken cancellationToken);
}

public sealed class HolonReplicaInitializationAuthorizationContext
{
    public HolonReplicaInitializationAuthorizationStage Stage {get;}
    public Guid OperatorId {get;}
    public long Sequence {get;}
    public Guid InitializationOperationId {get;}
    public string RouteBinding {get;}
    public string SourceId {get;}
    public ProviderType SourceAuthority {get;}
    public string SourceStoreIdentity {get;}
    public string TargetId {get;}
    public ProviderType TargetAuthority {get;}
    public string TargetEndpointIdentity {get;}
    public CommittedHolonReplicaRequest Mutation {get;}
    internal HolonReplicaInitializationAuthorizationContext(HolonReplicaInitializationAuthorizationStage stage,Guid actor,long sequence,Guid operation,string route,
        string sourceId,ProviderType sourceAuthority,string sourceStore,string targetId,ProviderType targetAuthority,string targetEndpoint,CommittedHolonReplicaRequest mutation=null)
    {Stage=stage;OperatorId=actor;Sequence=sequence;InitializationOperationId=operation;RouteBinding=route;Mutation=mutation;
        SourceId=sourceId;SourceAuthority=sourceAuthority;SourceStoreIdentity=sourceStore;TargetId=targetId;TargetAuthority=targetAuthority;TargetEndpointIdentity=targetEndpoint;}
}

/// <summary>Reviewable metadata only. Preparation does not initialize a target, complete a delivery or promise cutover.</summary>
public sealed class HolonReplicaInitializationIntent
{
    public Guid OperatorId {get;}
    public Guid InitializationOperationId {get;}
    public Guid SourceOperationId {get;}
    public long Sequence {get;}
    public int AttemptCount {get;}
    public string SourcePayloadDigest {get;}
    public string RouteBinding {get;}
    public string PolicyBinding {get;}
    public string BaselineFingerprint {get;}
    public string Fingerprint {get;}
    public IReadOnlyList<Guid> RootIds {get;}
    internal HolonReplicaInitializationIntent(Guid actor,Guid operation,HolonReplicationObligation source,string route,string policy,CommittedHolonReplicaPlan plan)
    {
        OperatorId=actor;InitializationOperationId=operation;SourceOperationId=source.Inspection.OperationId;
        Sequence=source.Inspection.Sequence;AttemptCount=source.Inspection.AttemptCount;SourcePayloadDigest=source.PayloadDigest;
        RouteBinding=route;PolicyBinding=policy;BaselineFingerprint=plan.Fingerprint;RootIds=Array.AsReadOnly(plan.RootIds.ToArray());
        Fingerprint=HolonReplicationCatalog.Digest("replica-initialization-intent-v1",route,policy,actor.ToString("D"),operation.ToString("D"),
            SourceOperationId.ToString("D"),Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            AttemptCount.ToString(System.Globalization.CultureInfo.InvariantCulture),SourcePayloadDigest,plan.Fingerprint);
    }
}

/// <summary>Authorize and verify one held, immutable enrolled source graph before any target mutation.
/// Host route identities are configuration, never caller-supplied provider selection or authorization.</summary>
public sealed class HolonReplicaInitializationPreparation
{
    private readonly IHolonReplicationObligationReader _source;
    private readonly IHolonReplicaInitializationPolicy _policy;
    private readonly string _sourceId,_targetId,_policyBinding,_sourceStoreIdentity,_targetEndpointIdentity;
    private readonly ProviderType _sourceAuthority,_targetAuthority;
    public string RouteBinding {get;}
    public HolonReplicaInitializationPreparation(string catalogId,Guid catalogVersion,string sourceId,string sourceStoreIdentity,
        ProviderType sourceAuthority,IHolonReplicationObligationReader source,string targetId,string targetEndpointIdentity,
        ProviderType targetAuthority,IHolonReplicaInitializationPolicy policy)
    {
        foreach(var identity in new[]{catalogId,sourceId,sourceStoreIdentity,targetId,targetEndpointIdentity})HolonReplicationCatalog.Identity(identity);
        if(catalogVersion==Guid.Empty)throw new ArgumentException("A stable catalog version is required.");
        CosmicConcurrency.ValidateAuthority(sourceAuthority);CosmicConcurrency.ValidateAuthority(targetAuthority);
        _source=source ?? throw new ArgumentNullException(nameof(source));_policy=policy ?? throw new ArgumentNullException(nameof(policy));
        _policyBinding=policy.BindingIdentity;HolonReplicationCatalog.Identity(_policyBinding);
        _sourceId=sourceId;_targetId=targetId;_sourceAuthority=sourceAuthority;_targetAuthority=targetAuthority;
        _sourceStoreIdentity=sourceStoreIdentity;_targetEndpointIdentity=targetEndpointIdentity;
        RouteBinding=HolonReplicationCatalog.Digest("replica-initialization-route-v1",catalogId,catalogVersion.ToString("D"),sourceId,
            sourceStoreIdentity,sourceAuthority.ToString(),targetId,targetEndpointIdentity,targetAuthority.ToString());
    }

    public Task<OASISResult<HolonReplicaInitializationIntent>> PrepareAsync(Guid operatorId,long sequence,Guid initializationOperationId,CancellationToken token=default)
        =>PrepareCoreAsync(operatorId,sequence,initializationOperationId,false,token);

    internal Task<OASISResult<HolonReplicaInitializationIntent>> ReviewApplicationAsync(Guid operatorId,long sequence,Guid operation,bool recovering,CancellationToken token)
        =>PrepareCoreAsync(operatorId,sequence,operation,recovering,token);

    /// <summary>Reauthorize an already reviewed intent before application/recovery.
    /// A completed source is admitted only for exact receipt/acknowledgement recovery;
    /// it does not grant permission to initialize an unrelated target.</summary>
    public async Task<OASISResult<CommittedHolonReplicaRequest>> ReauthorizeApplicationAsync(
        HolonReplicaInitializationIntent intent,CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        OASISResult<CommittedHolonReplicaRequest> Failed(string code,string message,Exception cause=null)
            =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
        var reviewed=await PrepareCoreAsync(intent.OperatorId,intent.Sequence,intent.InitializationOperationId,true,token).ConfigureAwait(false);
        if(reviewed.IsError)return Failed(reviewed.ErrorCode,reviewed.Message,reviewed.Exception);
        if(reviewed.Result.Fingerprint!=intent.Fingerprint)
            return Failed("COSMIC_REPLICA_INITIALIZATION_INTENT_CHANGED","The current authorized source differs from the durable reviewed intent.");
        var source=await _source.ReadObligationAsync(intent.Sequence,token).ConfigureAwait(false);
        if(source==null || source.IsError || source.IsWarning || source.Exception!=null || source.Result==null
            || source.Result.PayloadDigest!=intent.SourcePayloadDigest
            || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.Result.PayloadJson)))!=intent.SourcePayloadDigest)
            return Failed("COSMIC_REPLICA_INITIALIZATION_SOURCE_CHANGED","A second authoritative source read must match the reviewed frozen payload.",source?.Exception);
        var original=CommittedHolonReplicaPayload.Read(source.Result.PayloadJson);
        return new(new CommittedHolonReplicaRequest(intent.InitializationOperationId,original.SourceId,original.SourceAuthority,
            original.ActorId,original.Operation,original.Roots,original.SaveChildren,original.Recursive,original.MaxChildDepth));
    }

    private async Task<OASISResult<HolonReplicaInitializationIntent>> PrepareCoreAsync(Guid operatorId,long sequence,Guid initializationOperationId,bool allowCompleted,CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            if(operatorId==Guid.Empty || sequence<=0 || initializationOperationId==Guid.Empty)
                return Error("COSMIC_REPLICA_INITIALIZATION_INVALID","An identified operator, sequence and stable initialization operation are required.");
            var inspectionAuthorization=await Authorized(Context(HolonReplicaInitializationAuthorizationStage.ReadHeldSource,operatorId,sequence,initializationOperationId),token).ConfigureAwait(false);
            if(inspectionAuthorization.IsError)return Error(inspectionAuthorization.ErrorCode,inspectionAuthorization.Message,inspectionAuthorization.Exception);
            if(!inspectionAuthorization.Result)
                return Error("COSMIC_REPLICA_INITIALIZATION_DENIED","The host policy did not authorize source inspection.");
            var observed=await _source.ReadObligationAsync(sequence,token).ConfigureAwait(false);
            if(observed==null || observed.IsError || observed.IsWarning || observed.Exception!=null || observed.Result==null)
                return Error("COSMIC_REPLICA_INITIALIZATION_SOURCE_UNAVAILABLE","A clean authoritative obligation observation is required.",observed?.Exception);
            var frozen=observed.Result;var inspection=frozen.Inspection;
            var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(frozen.PayloadJson)));
            if(inspection.Sequence!=sequence || inspection.TargetId!=_targetId || inspection.OperationId==Guid.Empty
                || inspection.OperationId==initializationOperationId || (inspection.State!=HolonDeliveryState.ReconciliationRequired
                    && !(allowCompleted && inspection.State==HolonDeliveryState.Completed))
                || inspection.AttemptCount<1 || inspection.ErrorCode!="COSMIC_REPLICA_LINEAGE_CONFLICT"
                || inspection.RecoveryEvidence==null || inspection.RecoveryEvidence.RequiresNativeReconciliation || frozen.PayloadDigest!=digest)
                return Error("COSMIC_REPLICA_INITIALIZATION_SOURCE_INVALID","Only a bound lineage-conflict obligation without unresolved native execution is eligible.");
            var original=CommittedHolonReplicaPayload.Read(frozen.PayloadJson);
            if(original.OperationId!=inspection.OperationId || original.SourceId!=_sourceId || original.SourceAuthority!=_sourceAuthority || original.ActorId==Guid.Empty)
                return Error("COSMIC_REPLICA_INITIALIZATION_SOURCE_INVALID","The immutable payload does not belong to this source binding.");
            var head=inspection.State==HolonDeliveryState.Completed ? new OASISResult<HolonDeliveryInspection>(inspection)
                :await _source.PeekNextAsync(_targetId,token).ConfigureAwait(false);
            if(head==null || head.IsError || head.IsWarning || head.Exception!=null || head.Result==null
                || head.Result.Sequence!=sequence || head.Result.OperationId!=inspection.OperationId || head.Result.TargetId!=_targetId
                || head.Result.State!=inspection.State || head.Result.AttemptCount!=inspection.AttemptCount || head.Result.ErrorCode!=inspection.ErrorCode
                || head.Result.RecoveryEvidence==null || head.Result.RecoveryEvidence.RequiresNativeReconciliation)
                return Error("COSMIC_REPLICA_INITIALIZATION_NOT_HEAD","Initialization must bind the current held target queue head.",head?.Exception);
            var baseline=new CommittedHolonReplicaRequest(initializationOperationId,original.SourceId,original.SourceAuthority,original.ActorId,
                original.Operation,original.Roots,original.SaveChildren,original.Recursive,original.MaxChildDepth);
            var plan=CommittedHolonReplicaPlan.CreateInitialBaseline(baseline,_targetAuthority);
            var beforePolicy=CommittedHolonReplicaPayload.Capture(baseline);
            var approval=await Authorized(Context(HolonReplicaInitializationAuthorizationStage.ApproveInitialization,operatorId,sequence,initializationOperationId,baseline),token).ConfigureAwait(false);
            if(approval.IsError)return Error(approval.ErrorCode,approval.Message,approval.Exception);
            if(!approval.Result)
                return Error("COSMIC_REPLICA_INITIALIZATION_DENIED","The host policy did not approve this exact baseline.");
            if(CommittedHolonReplicaPayload.Capture(baseline)!=beforePolicy)
                return Error("COSMIC_REPLICA_INITIALIZATION_POLICY_MUTATED_INTENT","Policy changed the source snapshot while authorizing it.");
            return new(new(operatorId,initializationOperationId,frozen,RouteBinding,_policyBinding,plan));
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}
        catch(Exception cause){return Error("COSMIC_REPLICA_INITIALIZATION_PREPARATION_FAILED","Initialization preparation failed; no target or queue mutation was requested.",cause);}
    }
    private HolonReplicaInitializationAuthorizationContext Context(HolonReplicaInitializationAuthorizationStage stage,Guid actor,long sequence,Guid operation,CommittedHolonReplicaRequest mutation=null)
        =>new(stage,actor,sequence,operation,RouteBinding,_sourceId,_sourceAuthority,_sourceStoreIdentity,_targetId,_targetAuthority,_targetEndpointIdentity,mutation);
    private async Task<OASISResult<bool>> Authorized(HolonReplicaInitializationAuthorizationContext context,CancellationToken token)
    {
        OASISResult<bool> Failed(string code,string message,Exception cause=null)=>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
        if(_policy.BindingIdentity!=_policyBinding)return Failed("COSMIC_REPLICA_INITIALIZATION_POLICY_CHANGED","Policy binding changed; a new reviewed route is required.");
        var decision=await _policy.AuthorizeAsync(context,token).ConfigureAwait(false);token.ThrowIfCancellationRequested();
        if(_policy.BindingIdentity!=_policyBinding)return Failed("COSMIC_REPLICA_INITIALIZATION_POLICY_CHANGED","Policy binding changed while authorizing preparation.",decision?.Exception);
        if(decision==null || decision.IsError || decision.IsWarning || decision.Exception!=null)
            return Failed("COSMIC_REPLICA_INITIALIZATION_AUTHORIZATION_FAILED","The explicit authorization policy did not complete cleanly.",decision?.Exception);
        return decision;
    }
    private static OASISResult<HolonReplicaInitializationIntent> Error(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}
