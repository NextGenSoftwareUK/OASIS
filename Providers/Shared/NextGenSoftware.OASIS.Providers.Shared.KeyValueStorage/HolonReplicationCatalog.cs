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
    internal CosmicStorageIdentity StorageIdentity {get;}
    /// <summary>Binds the catalog route to a native identity checked inside receiver commit.</summary>
    public HolonReplicationDestination(string name, CosmicStorageIdentity storageIdentity,
        IBoundCommittedHolonReplicaStorageProvider provider, IHolonReplicationAuthorizationPolicy policy)
        : this(name, storageIdentity?.BindingFingerprint ?? throw new ArgumentNullException(nameof(storageIdentity)),
            storageIdentity.Authority, provider, policy)
    { StorageIdentity = storageIdentity; }

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
        private readonly CommittedHolonReplicaTarget _target;
        internal AuthorizedTarget(HolonReplicationCatalog catalog,HolonReplicationDestination destination)
        {
            ReplicationTargetId="cosmic:"+catalog.Version.ToString("N")+":"+Digest(destination.StorageIdentity == null ? "catalog-route-v1" : "catalog-bound-route-v1",catalog.CatalogId,catalog.SourceId,
                catalog.StoreIdentity,catalog.SourceAuthority.ToString(),destination.Name,destination.EndpointIdentity,
                destination.Authority.ToString(),destination.PolicyBinding);
            _target=new CommittedHolonReplicaTarget(ReplicationTargetId,destination.Provider,destination.Authority,catalog.SourceId,catalog.SourceAuthority,
                destination.Policy,catalog.CatalogId,catalog.Version,destination.PolicyBinding,destination.StorageIdentity);
        }
        public Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,CancellationToken cancellationToken)
            => _target.ApplyReplicatedMutationAsync(item,cancellationToken);
    }
}
