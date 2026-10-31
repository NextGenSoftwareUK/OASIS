using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public sealed class HolonReplicationAuthorizationContext
{
    public string CatalogId {get;}
    public Guid CatalogVersion {get;}
    public string TargetId {get;}
    public ProviderType TargetAuthority {get;}
    public CommittedHolonReplicaRequest Mutation {get;}
    internal HolonReplicationAuthorizationContext(string catalogId,Guid catalogVersion,string targetId,ProviderType authority,CommittedHolonReplicaRequest mutation)
    {CatalogId=catalogId;CatalogVersion=catalogVersion;TargetId=targetId;TargetAuthority=authority;Mutation=mutation;}
}

/// <summary>Server-owned authorization. BindingIdentity must change when policy meaning or grants change.</summary>
public interface IHolonReplicationAuthorizationPolicy
{
    string BindingIdentity {get;}
    Task<OASISResult<bool>> AuthorizeAsync(HolonReplicationAuthorizationContext context,CancellationToken cancellationToken);
}

/// <summary>Explicit expiring actor allowlist. Tenant, record and encryption rules require an appropriate host policy.</summary>
public sealed class HolonReplicationActorGrant : IHolonReplicationAuthorizationPolicy
{
    private readonly HashSet<Guid> _actors;
    private readonly DateTime _expires;
    private readonly IHyperDriveClock _clock;
    public string BindingIdentity {get;}
    public HolonReplicationActorGrant(IEnumerable<Guid> actors,DateTime expiresAtUtc,IHyperDriveClock clock=null)
    {
        _actors=new HashSet<Guid>(actors ?? throw new ArgumentNullException(nameof(actors)));
        if(_actors.Count==0 || _actors.Count>4096 || _actors.Contains(Guid.Empty) || expiresAtUtc.Kind!=DateTimeKind.Utc)
            throw new ArgumentException("A bounded nonempty actor grant and explicit UTC expiry are required.");
        _expires=expiresAtUtc;_clock=clock ?? new SystemHyperDriveClock();
        BindingIdentity=HolonReplicationCatalog.Digest("actor-grant-v1",_expires.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture),
            string.Join(",",_actors.OrderBy(id=>id).Select(id=>id.ToString("D"))));
    }
    public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicationAuthorizationContext context,CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(context);
        var now=_clock.UtcNow;if(now.Kind!=DateTimeKind.Utc)throw new InvalidOperationException("Authorization clock must return UTC.");
        return Task.FromResult(new OASISResult<bool>(_actors.Contains(context.Mutation.ActorId) && now<_expires));
    }
}

public sealed class HolonReplicationDestination
{
    public string Name {get;}
    public string EndpointIdentity {get;}
    public ProviderType Authority {get;}
    internal ICommittedHolonReplicaStorageProvider Provider {get;}
    internal IHolonReplicationAuthorizationPolicy Policy {get;}
    internal string PolicyBinding {get;}
    public HolonReplicationDestination(string name,string endpointIdentity,ProviderType authority,
        ICommittedHolonReplicaStorageProvider provider,IHolonReplicationAuthorizationPolicy policy)
    {
        HolonReplicationCatalog.Identity(name);HolonReplicationCatalog.Identity(endpointIdentity);CosmicConcurrency.ValidateAuthority(authority);
        Name=name;EndpointIdentity=endpointIdentity;Authority=authority;
        Provider=provider ?? throw new ArgumentNullException(nameof(provider));Policy=policy ?? throw new ArgumentNullException(nameof(policy));
        PolicyBinding=policy.BindingIdentity;HolonReplicationCatalog.Identity(PolicyBinding);
    }
}

/// <summary>Immutable source/target binding; endpoint identities are trusted host configuration, never payload-supplied.</summary>
public sealed class HolonReplicationCatalog
{
    public string CatalogId {get;}
    public Guid Version {get;}
    public string SourceId {get;}
    public string StoreIdentity {get;}
    public ProviderType SourceAuthority {get;}
    public IHolonReplicationRecoveryOutbox Store {get;}
    public IReadOnlyList<IHyperDriveIdempotentReplicationTarget> Targets {get;}
    public IReadOnlyList<string> TargetIds {get;}
    public HolonReplicationEnrollment Enrollment {get;}
    public HolonReplicationCatalog(string catalogId,Guid version,string sourceId,string storeIdentity,ProviderType sourceAuthority,
        IHolonReplicationRecoveryOutbox store,IEnumerable<HolonReplicationDestination> destinations)
    {
        Identity(catalogId);Identity(sourceId);Identity(storeIdentity);CosmicConcurrency.ValidateAuthority(sourceAuthority);
        if(version==Guid.Empty)throw new ArgumentException("An explicit catalog version is required.",nameof(version));
        Store=store ?? throw new ArgumentNullException(nameof(store));
        var items=destinations?.ToArray() ?? throw new ArgumentNullException(nameof(destinations));
        if(items.Length==0 || items.Length>64 || items.Any(item=>item==null) || items.Select(item=>item.Name).Distinct(StringComparer.Ordinal).Count()!=items.Length)
            throw new ArgumentException("One to 64 uniquely named destinations are required.",nameof(destinations));
        CatalogId=catalogId;Version=version;SourceId=sourceId;StoreIdentity=storeIdentity;SourceAuthority=sourceAuthority;
        var targets=items.Select(item=>(IHyperDriveIdempotentReplicationTarget)new AuthorizedTarget(this,item)).OrderBy(item=>item.ReplicationTargetId,StringComparer.Ordinal).ToArray();
        Targets=Array.AsReadOnly(targets);TargetIds=Array.AsReadOnly(targets.Select(item=>item.ReplicationTargetId).ToArray());
        Enrollment=new HolonReplicationEnrollment(sourceId,TargetIds);
    }
    internal static void Identity(string value)
    {if(string.IsNullOrWhiteSpace(value) || value!=value.Trim() || value.Length>256)throw new ArgumentException("A stable bounded identity is required.");}
    internal static string Digest(params string[] fields)
    {
        // Length prefixes prevent delimiter collisions in opaque endpoint or policy identities.
        var bytes=Encoding.UTF8.GetBytes(string.Concat(fields.Select(field=>Encoding.UTF8.GetByteCount(field).ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+field)));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
    private sealed class AuthorizedTarget : IHyperDriveIdempotentReplicationTarget
    {
        public string ReplicationTargetId {get;}
        private readonly HolonReplicationCatalog _catalog;
        private readonly HolonReplicationDestination _destination;
        private readonly CommittedHolonReplicaTarget _target;
        internal AuthorizedTarget(HolonReplicationCatalog catalog,HolonReplicationDestination destination)
        {
            _catalog=catalog;_destination=destination;
            ReplicationTargetId="cosmic:"+catalog.Version.ToString("N")+":"+Digest("catalog-route-v1",catalog.CatalogId,catalog.SourceId,
                catalog.StoreIdentity,catalog.SourceAuthority.ToString(),destination.Name,destination.EndpointIdentity,
                destination.Authority.ToString(),destination.PolicyBinding);
            _target=new CommittedHolonReplicaTarget(ReplicationTargetId,destination.Provider,destination.Authority,catalog.SourceId,catalog.SourceAuthority);
        }
        public async Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,CancellationToken cancellationToken)
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
                if(mutation.SourceId!=_catalog.SourceId || mutation.SourceAuthority!=_catalog.SourceAuthority)
                    return Denied("COSMIC_REPLICA_SOURCE_NOT_ENROLLED","The source is not enrolled for this route.");
                if(_destination.Policy.BindingIdentity!=_destination.PolicyBinding)
                    return Denied("COSMIC_REPLICA_AUTHORIZATION_FAILED","Authorization binding changed without a new catalog route.");
                authorized=await _destination.Policy.AuthorizeAsync(new HolonReplicationAuthorizationContext(_catalog.CatalogId,_catalog.Version,
                    ReplicationTargetId,_destination.Authority,mutation),cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if(_destination.Policy.BindingIdentity!=_destination.PolicyBinding)
                    return Denied("COSMIC_REPLICA_AUTHORIZATION_FAILED","Authorization binding changed while authorizing delivery.");
                if(authorized==null || authorized.IsError || !authorized.Result)
                    return Denied("COSMIC_REPLICA_AUTHORIZATION_DENIED","The explicit route policy did not authorize delivery.",authorized?.Exception);
            }
            catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){throw;}
            catch(Exception cause)
            {return Denied(cause is InvalidDataException || cause is ArgumentException ? "COSMIC_REPLICA_PAYLOAD_INVALID":"COSMIC_REPLICA_AUTHORIZATION_FAILED",cause.Message,cause);}
            // Reparse the original frozen bytes inside the receiver adapter; policy cannot rewrite a mutation.
            var result=await _target.ApplyReplicatedMutationAsync(frozen,cancellationToken).ConfigureAwait(false);
            if(authorized.IsWarning || authorized.Exception!=null)
            {
                result.IsWarning=true;result.WarningCount+=Math.Max(1,authorized.WarningCount);
                result.Exception=result.Exception==null ? authorized.Exception:authorized.Exception==null ? result.Exception:new AggregateException(authorized.Exception,result.Exception);
                result.InnerMessages.Add("Authorization completed with a retained policy warning.");
            }
            return result;
        }
        private static OASISResult<bool> Denied(string code,string message,Exception cause=null)
            =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
    }
}
