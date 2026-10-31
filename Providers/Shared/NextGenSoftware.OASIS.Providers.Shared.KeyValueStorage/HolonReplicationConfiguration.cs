using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public sealed class HolonReplicationConfiguredTarget
{
    public string Name {get;}
    public string EndpointIdentity {get;}
    public ProviderType Provider {get;}
    public IReadOnlyList<Guid> Actors {get;}
    public DateTime ExpiresAtUtc {get;}
    internal HolonReplicationConfiguredTarget(string name,string endpoint,ProviderType provider,Guid[] actors,DateTime expiry)
    {Name=name;EndpointIdentity=endpoint;Provider=provider;Actors=System.Array.AsReadOnly(actors);ExpiresAtUtc=expiry;}
}
public sealed class HolonReplicationConfiguredCatalog
{
    public string Id {get;}
    public Guid Version {get;}
    public string SourceId {get;}
    public string StoreIdentity {get;}
    public ProviderType SourceProvider {get;}
    public IReadOnlyList<HolonReplicationConfiguredTarget> Targets {get;}
    internal HolonReplicationConfiguredCatalog(string id,Guid version,string source,string store,ProviderType provider,HolonReplicationConfiguredTarget[] targets)
    {Id=id;Version=version;SourceId=source;StoreIdentity=store;SourceProvider=provider;Targets=System.Array.AsReadOnly(targets);}
}

/// <summary>Strict trusted deployment input, not a request-body API. Credentials stay in existing provider configuration.</summary>
public sealed class HolonReplicationConfiguration
{
    public bool Enabled {get;}
    public HolonReplicationHostOptions Options {get;}
    public IReadOnlyList<HolonReplicationConfiguredCatalog> Catalogs {get;}
    internal IHyperDriveClock Clock {get;}
    private HolonReplicationConfiguration(bool enabled,HolonReplicationHostOptions options,HolonReplicationConfiguredCatalog[] catalogs,IHyperDriveClock clock)
    {Enabled=enabled;Options=options;Catalogs=System.Array.AsReadOnly(catalogs);Clock=clock;}
    public static HolonReplicationConfiguration Parse(string json,IHyperDriveClock clock=null)
    {
        clock??=new SystemHyperDriveClock();
        if(json==null)return new(false,null,System.Array.Empty<HolonReplicationConfiguredCatalog>(),clock);
        if(string.IsNullOrWhiteSpace(json) || json.Length>1024*1024)throw new InvalidDataException("A bounded configuration JSON object is required.");
        try
        {
            using var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None};
            var root=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if(reader.Read())throw new InvalidDataException("Trailing configuration content.");
            if(root["enabled"]?.Type!=JTokenType.Boolean)throw new InvalidDataException("An explicit boolean enabled flag is required.");
            if(!root.Value<bool>("enabled")){Fields(root,"enabled");return new(false,null,System.Array.Empty<HolonReplicationConfiguredCatalog>(),clock);}
            Fields(root,"enabled","workerId","catalogs");var options=new HolonReplicationHostOptions(Text(root,"workerId"));
            var catalogs=new List<HolonReplicationConfiguredCatalog>();var routeCount=0;var actorCount=0;var now=clock.UtcNow;
            if(now.Kind!=DateTimeKind.Utc)throw new InvalidDataException("Configuration clock must be UTC.");
            foreach(var token in Array(root,"catalogs",16))
            {
                var item=Object(token);Fields(item,"id","version","sourceId","storeIdentity","sourceProvider","targets");
                var id=Text(item,"id");var version=GuidValue(Text(item,"version"));var source=Text(item,"sourceId");var store=Text(item,"storeIdentity");
                var provider=Provider(Text(item,"sourceProvider"));var targets=new List<HolonReplicationConfiguredTarget>();
                foreach(var value in Array(item,"targets",64))
                {
                    var target=Object(value);Fields(target,"name","endpointIdentity","provider","actors","expiresAtUtc");
                    var targetProvider=Provider(Text(target,"provider"));if(targetProvider==provider)throw new InvalidDataException("A registry source cannot replicate to itself.");
                    var actors=Array(target,"actors",4096).Select(actor=>GuidValue(String(actor))).ToArray();
                    var expiryText=Text(target,"expiresAtUtc");
                    if(!expiryText.EndsWith("Z",StringComparison.Ordinal) || !DateTime.TryParse(expiryText,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out var expiry)
                        || expiry.Kind!=DateTimeKind.Utc || expiry<=now)throw new InvalidDataException("Every enabled target requires an unexpired explicit UTC grant.");
                    targets.Add(new(Text(target,"name"),Text(target,"endpointIdentity"),targetProvider,actors,expiry));actorCount+=actors.Length;
                }
                if(targets.Select(target=>target.Name).Distinct(StringComparer.Ordinal).Count()!=targets.Count)throw new InvalidDataException("Destination names must be unique inside a catalog.");
                routeCount+=targets.Count;catalogs.Add(new(id,version,source,store,provider,targets.ToArray()));
            }
            if(routeCount>64 || actorCount>4096 || catalogs.Select(catalog=>(catalog.Id,catalog.Version)).Distinct().Count()!=catalogs.Count)
                throw new InvalidDataException("Catalogs, routes and grants exceed bounds or repeat a version.");
            foreach(var group in catalogs.GroupBy(catalog=>catalog.SourceId,StringComparer.Ordinal))
                if(group.Select(catalog=>(catalog.SourceProvider,catalog.StoreIdentity)).Distinct().Count()!=1)throw new InvalidDataException("A source identity has one provider/store binding.");
            foreach(var group in catalogs.GroupBy(catalog=>catalog.StoreIdentity,StringComparer.Ordinal))
                if(group.Select(catalog=>(catalog.SourceProvider,catalog.SourceId)).Distinct().Count()!=1)throw new InvalidDataException("A store identity has one source binding.");
            foreach(var group in catalogs.GroupBy(catalog=>catalog.SourceProvider))
                if(group.Select(catalog=>(catalog.SourceId,catalog.StoreIdentity)).Distinct().Count()!=1)throw new InvalidDataException("A registered provider has one source/store binding.");
            foreach(var group in catalogs.SelectMany(catalog=>catalog.Targets).GroupBy(target=>target.Provider))
                if(group.Select(target=>target.EndpointIdentity).Distinct(StringComparer.Ordinal).Count()!=1)throw new InvalidDataException("A registered target provider has one endpoint descriptor.");
            return new(true,options,catalogs.ToArray(),clock);
        }
        catch(Exception cause)when(cause is JsonException || cause is ArgumentException)
        {throw new InvalidDataException("Invalid COSMIC replication configuration.",cause);}
    }
    private static JObject Object(JToken token)=>token as JObject ?? throw new InvalidDataException("Configuration members must be objects.");
    private static JArray Array(JObject item,string key,int maximum)
    {if(item[key] is not JArray values || values.Count==0 || values.Count>maximum)throw new InvalidDataException("A bounded nonempty array is required: "+key);return values;}
    private static void Fields(JObject item,params string[] names)
    {if(item.Properties().Count()!=names.Length || names.Any(name=>item.Property(name,StringComparison.Ordinal)==null))throw new InvalidDataException("Missing or unknown configuration fields.");}
    private static string String(JToken token)=>token?.Type==JTokenType.String ? token.Value<string>():throw new InvalidDataException("Configuration identities must be strings.");
    private static string Text(JObject item,string key){var value=String(item[key]);HolonReplicationCatalog.Identity(value);return value;}
    private static Guid GuidValue(string value)=>Guid.TryParseExact(value,"D",out var id) && id!=Guid.Empty ? id:throw new InvalidDataException("A nonempty canonical GUID is required.");
    private static ProviderType Provider(string value)
    {
        if(!Enum.TryParse<ProviderType>(value,out var type) || type.ToString()!=value)throw new InvalidDataException("A named provider authority is required.");
        CosmicConcurrency.ValidateAuthority(type);return type;
    }
}

public static class HolonReplicationCatalogFactory
{
    public static async Task<IReadOnlyList<HolonReplicationCatalog>> BuildAsync(HolonReplicationConfiguration configuration,
        Func<ProviderType,IOASISStorageProvider> resolver,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(configuration);ArgumentNullException.ThrowIfNull(resolver);cancellationToken.ThrowIfCancellationRequested();
        if(!configuration.Enabled)return System.Array.Empty<HolonReplicationCatalog>();
        if(configuration.Clock.UtcNow.Kind!=DateTimeKind.Utc || configuration.Catalogs.SelectMany(catalog=>catalog.Targets).Any(target=>target.ExpiresAtUtc<=configuration.Clock.UtcNow))
            throw new InvalidOperationException("A configured grant expired before provider activation.");
        var required=configuration.Catalogs.Select(catalog=>catalog.SourceProvider).Concat(configuration.Catalogs.SelectMany(catalog=>catalog.Targets.Select(target=>target.Provider))).Distinct().OrderBy(type=>(int)type).ToArray();
        var providers=new Dictionary<ProviderType,IOASISStorageProvider>();
        foreach(var type in required)
        {
            var provider=resolver(type);
            if(provider==null || provider.ProviderType?.Value!=type)throw new InvalidOperationException("The requested COSMIC provider authority is not registered exactly.");
            providers.Add(type,provider);
        }
        foreach(var catalog in configuration.Catalogs)
        {
            var source=providers[catalog.SourceProvider];
            if(source is not IAtomicHolonReplicationStorageProvider || source is not IHolonReplicationOutboxSource)
                throw new InvalidOperationException("The configured source lacks atomic native outbox capability.");
            foreach(var target in catalog.Targets)
                if(ReferenceEquals(source,providers[target.Provider]) || providers[target.Provider] is not ICommittedHolonReplicaStorageProvider)
                    throw new InvalidOperationException("The configured target lacks an independent committed-receiver capability.");
        }
        // Validate every registry binding/capability before opening the first provider. Never resolve a default/failover substitute.
        foreach(var provider in providers.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(!provider.IsProviderActivated)
            {
                var result=await provider.ActivateProviderAsync().ConfigureAwait(false);cancellationToken.ThrowIfCancellationRequested();
                if(result==null || result.IsError || result.IsWarning || !result.Result || !provider.IsProviderActivated)
                    throw new InvalidOperationException("Pinned COSMIC provider activation failed.",result?.Exception);
            }
        }
        if(providers.Any(entry=>entry.Value.ProviderType?.Value!=entry.Key))throw new InvalidOperationException("Pinned provider authority changed during activation.");
        if(configuration.Catalogs.SelectMany(catalog=>catalog.Targets).Any(target=>target.ExpiresAtUtc<=configuration.Clock.UtcNow))
            throw new InvalidOperationException("A configured grant expired while activating providers.");
        var stores=new Dictionary<ProviderType,IHolonReplicationReadinessOutbox>();
        var catalogs=new List<HolonReplicationCatalog>();
        foreach(var item in configuration.Catalogs)
        {
            if(!stores.TryGetValue(item.SourceProvider,out var store))
            {
                var captured=((IHolonReplicationOutboxSource)providers[item.SourceProvider]).GetHolonReplicationOutbox();
                if(captured==null || captured.IsError || captured.IsWarning || captured.Result==null)
                    throw new InvalidOperationException("Pinned COSMIC source outbox capture failed.",captured?.Exception);
                store=captured.Result;stores.Add(item.SourceProvider,store);
            }
            var destinations=item.Targets.Select(target=>new HolonReplicationDestination(target.Name,target.EndpointIdentity,target.Provider,
                (ICommittedHolonReplicaStorageProvider)providers[target.Provider],new HolonReplicationActorGrant(target.Actors,target.ExpiresAtUtc,configuration.Clock))).ToArray();
            var catalog=new HolonReplicationCatalog(item.Id,item.Version,item.SourceId,item.StoreIdentity,item.SourceProvider,store,destinations);
            foreach(var target in catalog.TargetIds)
            {
                var head=await store.PeekNextAsync(target,cancellationToken).ConfigureAwait(false);
                if(head==null || head.IsError || head.IsWarning || head.Result!=null && head.Result.TargetId!=target)throw new InvalidOperationException("Pinned COSMIC source readiness failed before startup.",head?.Exception);
            }
            catalogs.Add(catalog);
        }
        // Also enforce the complete cross-catalog native-store/route invariant before publishing the snapshot.
        using var validated=new HolonReplicationHostedService(catalogs,configuration.Options,Microsoft.Extensions.Logging.Abstractions.NullLogger<HolonReplicationHostedService>.Instance);
        if(configuration.Catalogs.SelectMany(catalog=>catalog.Targets).Any(target=>target.ExpiresAtUtc<=configuration.Clock.UtcNow))
            throw new InvalidOperationException("A configured grant expired during source readiness validation.");
        return System.Array.AsReadOnly(catalogs.ToArray());
    }
}
