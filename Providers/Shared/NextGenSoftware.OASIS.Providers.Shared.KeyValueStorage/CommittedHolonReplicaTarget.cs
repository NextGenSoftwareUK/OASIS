using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Explicit hosted full-graph receiver. Source enrollment is configuration, not caller authentication.</summary>
public sealed class CommittedHolonReplicaTarget : IHyperDriveIdempotentReplicationTarget
{
    public const string EntityType = "cosmic.committed-holon-graph.v1";
    public string ReplicationTargetId { get; }
    private readonly ICommittedHolonReplicaStorageProvider _provider;
    private readonly string _sourceId;
    private readonly ProviderType _sourceAuthority;
    private readonly ProviderType _targetAuthority;
    private readonly IHolonReplicationAuthorizationPolicy _policy;
    private readonly string _policyBinding;
    private readonly string _catalogId;
    private readonly Guid _catalogVersion;
    private readonly CosmicStorageIdentity _expectedStorage;

    public CommittedHolonReplicaTarget(string targetId, ICommittedHolonReplicaStorageProvider provider,
        ProviderType targetAuthority, string sourceId, ProviderType sourceAuthority)
    {
        if (string.IsNullOrWhiteSpace(targetId) || targetId != targetId.Trim()) throw new ArgumentException("A stable target id is required.", nameof(targetId));
        if (string.IsNullOrWhiteSpace(sourceId) || sourceId != sourceId.Trim() || sourceId.Length > 256)
            throw new ArgumentException("A stable source id is required.", nameof(sourceId));
        CosmicConcurrency.ValidateAuthority(sourceAuthority); CosmicConcurrency.ValidateAuthority(targetAuthority);
        ReplicationTargetId = targetId; _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _sourceId = sourceId; _sourceAuthority = sourceAuthority; _targetAuthority = targetAuthority;
    }

    /// <summary>Host-owned policy admission. The caller still owns durable endpoint/route binding.</summary>
    public CommittedHolonReplicaTarget(string targetId, ICommittedHolonReplicaStorageProvider provider,
        ProviderType targetAuthority, string sourceId, ProviderType sourceAuthority,
        IHolonReplicationAuthorizationPolicy policy, string catalogId, Guid catalogVersion, string expectedPolicyBinding = null)
        : this(targetId, provider, targetAuthority, sourceId, sourceAuthority, policy, catalogId,
            catalogVersion, expectedPolicyBinding, null) { }

    /// <summary>Native storage admission is enforced by the receiver transaction, not a separate preflight.</summary>
    public CommittedHolonReplicaTarget(string targetId, ICommittedHolonReplicaStorageProvider provider,
        ProviderType targetAuthority, string sourceId, ProviderType sourceAuthority,
        IHolonReplicationAuthorizationPolicy policy, string catalogId, Guid catalogVersion,
        string expectedPolicyBinding, CosmicStorageIdentity expectedStorage)
        : this(targetId, provider, targetAuthority, sourceId, sourceAuthority)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _policyBinding = expectedPolicyBinding ?? policy.BindingIdentity;
        HolonReplicationCatalog.Identity(_policyBinding);
        HolonReplicationCatalog.Identity(catalogId);
        if (catalogVersion == Guid.Empty) throw new ArgumentException("An explicit authorization context version is required.", nameof(catalogVersion));
        _catalogId = catalogId;
        _catalogVersion = catalogVersion;
        if (expectedStorage != null && (expectedStorage.Authority != targetAuthority || provider is not IBoundCommittedHolonReplicaStorageProvider))
            throw new ArgumentException("A bound route requires the matching authority and a transaction-bound receiver.", nameof(expectedStorage));
        _expectedStorage = expectedStorage;
    }

    public static HostedSyncFanOutItem CaptureItem(CommittedHolonReplicaRequest request)
        => ReadItem(CommittedHolonReplicaPayload.Capture(request));

    /// <summary>Build routing fields without re-encoding the original frozen transport bytes.</summary>
    public static HostedSyncFanOutItem ReadItem(string payload)
    {
        var frozen = CommittedHolonReplicaPayload.Read(payload);
        return new HostedSyncFanOutItem { OperationId = frozen.OperationId, AvatarId = frozen.ActorId,
            EntityId = frozen.Roots[0].Id, VersionId = frozen.Roots[0].VersionId, EntityType = EntityType,
            Kind = frozen.Operation == ConditionalHolonOperation.Save ? SyncOperationKind.Upsert : SyncOperationKind.Delete,
            PayloadJson = payload };
    }

    public Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item, CancellationToken cancellationToken)
        => _policy == null ? ApplyFrozenMutationAsync(item, cancellationToken) : ApplyAuthorizedMutationAsync(item, cancellationToken);

    private async Task<OASISResult<bool>> ApplyAuthorizedMutationAsync(HostedSyncFanOutItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();OASISResult<bool> authorized;
        HostedSyncFanOutItem frozen;
        try
        {
            if(item==null)throw new InvalidDataException("A committed holon graph item is required.");
            // The transport envelope is mutable. Capture every field before yielding to host policy;
            // authorization and the receiver must use the same payload and routing assertions.
            frozen=new HostedSyncFanOutItem{OperationId=item.OperationId,AvatarId=item.AvatarId,
                EntityId=item.EntityId,EntityType=item.EntityType,Kind=item.Kind,VersionId=item.VersionId,
                PayloadJson=item.PayloadJson,AttemptCount=item.AttemptCount};
            var mutation=CommittedHolonReplicaPayload.Read(frozen.PayloadJson);
            if(mutation.SourceId!=_sourceId || mutation.SourceAuthority!=_sourceAuthority)
                return Denied("COSMIC_REPLICA_SOURCE_NOT_ENROLLED","The source is not enrolled for this route.");
            if(_policy.BindingIdentity!=_policyBinding)
                return Denied("COSMIC_REPLICA_AUTHORIZATION_FAILED","Authorization binding changed without a new catalog route.");
            authorized=await _policy.AuthorizeAsync(new HolonReplicationAuthorizationContext(_catalogId,_catalogVersion,
                ReplicationTargetId,_targetAuthority,mutation),cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if(_policy.BindingIdentity!=_policyBinding)
                return Denied("COSMIC_REPLICA_AUTHORIZATION_FAILED","Authorization binding changed while authorizing delivery.");
            if(authorized==null || authorized.IsError || !authorized.Result)
                return Denied("COSMIC_REPLICA_AUTHORIZATION_DENIED","The explicit route policy did not authorize delivery.",authorized?.Exception);
        }
        catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){throw;}
        catch(Exception cause)
        {return Denied(cause is InvalidDataException || cause is ArgumentException ? "COSMIC_REPLICA_PAYLOAD_INVALID":"COSMIC_REPLICA_AUTHORIZATION_FAILED",cause.Message,cause);}
        // Reparse the original frozen bytes inside the receiver adapter; policy cannot rewrite a mutation.
        var result=await ApplyFrozenMutationAsync(frozen,cancellationToken).ConfigureAwait(false);
        if(authorized.IsWarning || authorized.Exception!=null)
        {
            result.IsWarning=true;result.WarningCount+=Math.Max(1,authorized.WarningCount);
            result.Exception=result.Exception==null ? authorized.Exception:authorized.Exception==null ? result.Exception:new AggregateException(authorized.Exception,result.Exception);
            result.InnerMessages.Add("Authorization completed with a retained policy warning.");
        }
        return result;
    }
    private static OASISResult<bool> Denied(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

    private async Task<OASISResult<bool>> ApplyFrozenMutationAsync(HostedSyncFanOutItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommittedHolonReplicaRequest request;
        CommittedHolonReplicaPlan plan;
        try
        {
            if (item == null || item.EntityType != EntityType) throw new InvalidDataException("A committed holon graph item is required.");
            request = CommittedHolonReplicaPayload.Read(item.PayloadJson);
            if (request.SourceId != _sourceId || request.SourceAuthority != _sourceAuthority)
                return Error("COSMIC_REPLICA_SOURCE_NOT_ENROLLED", "The payload source is not enrolled for this target.");
            if (item.OperationId != request.OperationId || item.AvatarId != request.ActorId
                || item.EntityId != request.Roots[0].Id || item.VersionId != request.Roots[0].VersionId
                || item.Kind != (request.Operation == ConditionalHolonOperation.Save ? SyncOperationKind.Upsert : SyncOperationKind.Delete))
                throw new InvalidDataException("Hosted routing fields disagree with the frozen graph payload.");
            plan = CommittedHolonReplicaPlan.Create(request, _targetAuthority);
        }
        catch (Exception error) when (error is InvalidDataException || error is ArgumentException)
        { return Error("COSMIC_REPLICA_PAYLOAD_INVALID", error.Message, error); }

        var applied = _expectedStorage == null
            ? await _provider.ApplyCommittedHolonReplicaAsync(request, cancellationToken).ConfigureAwait(false)
            : await ((IBoundCommittedHolonReplicaStorageProvider)_provider).ApplyBoundCommittedHolonReplicaAsync(
                request, _expectedStorage, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (applied == null) return Error("COSMIC_REPLICA_NO_RESULT", "The replica provider returned no acknowledgement.");
        var result = new OASISResult<bool> { IsError = applied.IsError, ErrorCode = applied.ErrorCode,
            ErrorCount = applied.ErrorCount, Exception = applied.Exception, Message = applied.Message,
            IsWarning = applied.IsWarning, WarningCount = applied.WarningCount,
            MetaData = applied.MetaData == null ? null : new Dictionary<string, string>(applied.MetaData) };
        result.InnerMessages.AddRange(applied.InnerMessages);
        if (applied.IsError) return result;
        try
        {
            if (applied.Result == null) throw new InvalidDataException("The replica provider returned no graph.");
            var graph = HolonGraphWritePlan.Create(applied.Result, request.SaveChildren, request.Recursive, request.MaxChildDepth, 0);
            if (!graph.Roots.Select(root => root.Id).SequenceEqual(plan.RootIds) || graph.Nodes.Count != plan.Nodes.Count)
                throw new InvalidDataException("The replica acknowledgement has different roots or node count.");
            foreach (var node in graph.Nodes)
            {
                plan.ValidateAcknowledgedRecord(node.Snapshot);
                if (!plan.ChildIds[node.Snapshot.Id].SequenceEqual(node.SelectedChildIds))
                    throw new InvalidDataException("The replica acknowledgement changed selected child edges.");
            }
            result.Result = true;
        }
        catch (Exception error) when (error is InvalidDataException || error is ArgumentException || error is KeyNotFoundException)
        {
            result.IsError = true; result.ErrorCount++; result.ErrorCode = "COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID";
            result.Message = error.Message;
            result.Exception = applied.Exception == null ? error : new AggregateException(applied.Exception, error);
        }
        return result;
    }

    private static OASISResult<bool> Error(string code, string message, Exception error = null)
        => new() { IsError = true, ErrorCode = code, ErrorCount = 1, Message = message, Exception = error };
}
