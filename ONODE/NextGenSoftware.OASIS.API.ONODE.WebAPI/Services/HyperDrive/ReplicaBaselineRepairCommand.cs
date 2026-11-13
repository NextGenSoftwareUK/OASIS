using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NeoProvider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using NeoOutbox=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.NeoHolonReplicationOutbox;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;

/// <summary>Trusted shell command for reviewed cross-provider routes among
/// MongoDB, SQLite and Neo4j. The request may name only enrolled profiles; endpoints and
/// credentials come exclusively from host DNA. No HTTP/background host is started.</summary>
internal static class ReplicaBaselineRepairCommand
{
    internal sealed record Request(Guid OperatorId, Guid InitializationOperationId, Guid CatalogVersion,
        long Sequence, Guid SourceOperationId, Guid SourceActorId, Guid RootId,
        string SourcePayloadDigest, string SourceStoreIdentity, string TargetFile,
        [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string SourceProvider=null,
        [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string TargetProvider=null,
        [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string TargetStoreIdentity=null);

    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if(args.Length!=2 || Environment.GetEnvironmentVariable("OASIS_SANDBOX")!="true")
                throw new InvalidOperationException("Explicit demo shell context required.");
            var path=Path.GetFullPath(args[1]);
            var request=await ReadReviewedRequestAsync(path);
            var dna=Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>(Environment.GetEnvironmentVariable("OASIS_DNA_JSON")
                ?? throw new InvalidOperationException("Configured DNA required."));
            var result=await ExecuteAsync(dna,request,path+".application.json");
            Console.WriteLine(JsonSerializer.Serialize(new{success=!result.IsError && result.Result,
                code=result.ErrorCode,sequence=request.Sequence,initializationOperationId=request.InitializationOperationId}));
            return !result.IsError && result.Result ? 0:1;
        }
        catch(Exception)
        {
            // Native exception text can contain configured credentials or payload.
            Console.Error.WriteLine("COSMIC_REPLICA_REPAIR_COMMAND_FAILED: preserve request/journal and inspect sanitized evidence.");
            return 1;
        }
    }

    /// <summary>One bounded, unambiguous reviewed intent. Duplicate properties
    /// must not let a reader approve one identity while execution uses another.</summary>
    internal static async Task<Request> ReadReviewedRequestAsync(string path)
    {
        const int maximumBytes=8192;
        await using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        var bytes=new byte[maximumBytes+1];var length=0;
        while(length<bytes.Length)
        {
            var read=await stream.ReadAsync(bytes.AsMemory(length,bytes.Length-length));
            if(read==0)break;
            length+=read;
        }
        if(length>maximumBytes)throw new InvalidDataException("Reviewed repair request exceeds its byte budget.");
        return ParseReviewedRequest(bytes.AsMemory(0,length));
    }

    internal static Request ParseReviewedRequest(ReadOnlyMemory<byte> bytes)
    {
        if(bytes.Length>8192)throw new InvalidDataException("Reviewed repair request exceeds its byte budget.");
        using var document=JsonDocument.Parse(bytes,new JsonDocumentOptions{MaxDepth=4});
        if(document.RootElement.ValueKind!=JsonValueKind.Object)
            throw new InvalidDataException("A reviewed repair object is required.");
        var names=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach(var property in document.RootElement.EnumerateObject())
            if(!names.Add(property.Name))throw new InvalidDataException("Duplicate reviewed repair field.");
        return document.RootElement.Deserialize<Request>(new JsonSerializerOptions{
            UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow})
            ?? throw new InvalidDataException("A reviewed repair object is required.");
    }

    internal static string StoreIdentity(string uri,string database)
    {
        var endpoint=MongoUrl.Create(uri);
        return "mongo-store:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join(",",endpoint.Servers.Select(server=>server.ToString()).OrderBy(value=>value,StringComparer.Ordinal))+"|"+database)));
    }

    internal static ProviderType SourceProfile(string name)=>name switch {
        null or "MongoDBOASIS"=>ProviderType.MongoDBOASIS,
        "Neo4jOASIS"=>ProviderType.Neo4jOASIS,
        "SQLLiteDBOASIS"=>ProviderType.SQLLiteDBOASIS,
        _=>throw new ArgumentException("Only explicit MongoDB, Neo4j or SQLite source profiles are supported.")
    };

    internal static ProviderType TargetProfile(string name)=>name switch {
        null or "SQLLiteDBOASIS"=>ProviderType.SQLLiteDBOASIS,
        "MongoDBOASIS"=>ProviderType.MongoDBOASIS,
        "Neo4jOASIS"=>ProviderType.Neo4jOASIS,
        _=>throw new ArgumentException("Only explicit SQLite, MongoDB or Neo4j target profiles are supported.")
    };

    internal static void ValidateRoute(ProviderType source,ProviderType target)
    {
        if(source==target || source is not (ProviderType.MongoDBOASIS or ProviderType.SQLLiteDBOASIS or ProviderType.Neo4jOASIS)
            || target is not (ProviderType.MongoDBOASIS or ProviderType.SQLLiteDBOASIS or ProviderType.Neo4jOASIS))
            throw new ArgumentException("This reviewed source/target route is not enrolled.");
    }

    private static SqliteConnectionStringBuilder ExistingSqlite(string text)
    {
        var connection=new SqliteConnectionStringBuilder(text);
        if(string.IsNullOrWhiteSpace(connection.DataSource) || connection.DataSource==":memory:" || connection.Mode==SqliteOpenMode.Memory
            || !System.IO.File.Exists(Path.GetFullPath(connection.DataSource)))
            throw new InvalidOperationException("An existing configured SQLite file is required; no provisioning.");
        connection.Mode=SqliteOpenMode.ReadWrite;return connection;
    }

    internal static string SqliteStoreIdentity(string text)
    {
        var connection=ExistingSqlite(text);
        return "sqlite-store:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(connection.DataSource))));
    }

    internal static string NeoStoreIdentity(string host,string username)
    {
        if(!Uri.TryCreate(host,UriKind.Absolute,out var endpoint) || string.IsNullOrWhiteSpace(username)
            || endpoint.UserInfo.Length!=0 || endpoint.Query.Length!=0 || endpoint.Fragment.Length!=0
            || endpoint.AbsolutePath is not ("" or "/")
            || !new[]{"bolt","bolt+s","bolt+ssc","neo4j","neo4j+s","neo4j+ssc"}.Contains(endpoint.Scheme,StringComparer.Ordinal))
            throw new ArgumentException("An explicit configured Neo4j endpoint and principal are required.");
        return "neo4j-store:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new[]{endpoint.AbsoluteUri,username}))));
    }

    internal static async Task<OASISResult<bool>> ExecuteAsync(OASISDNA dna,Request request,string protectedJournal)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(dna?.OASIS?.HyperDriveMode!=HyperDriveModes.V2 || request.OperatorId==Guid.Empty
            || request.InitializationOperationId==Guid.Empty || request.CatalogVersion==Guid.Empty || request.Sequence<=0
            || request.SourceOperationId==Guid.Empty || request.SourceActorId==Guid.Empty || request.RootId==Guid.Empty
            || request.SourcePayloadDigest?.Length!=64 || !request.SourcePayloadDigest.All(Uri.IsHexDigit))
            throw new ArgumentException("A complete reviewed V2 repair request is required.");
        var sourceType=SourceProfile(request.SourceProvider);
        var targetType=TargetProfile(request.TargetProvider);ValidateRoute(sourceType,targetType);
        var settings=dna.OASIS.StorageProviders;
        var configuredSource=sourceType switch {
            ProviderType.MongoDBOASIS=>StoreIdentity(settings.MongoDBOASIS.ConnectionString,settings.MongoDBOASIS.DBName),
            ProviderType.Neo4jOASIS=>NeoStoreIdentity(settings.Neo4jOASIS.ConnectionString,settings.Neo4jOASIS.Username),
            ProviderType.SQLLiteDBOASIS=>SqliteStoreIdentity(settings.SQLLiteDBOASIS.ConnectionString),
            _=>throw new InvalidOperationException("Unresolved source profile.")
        };
        var targetIdentity=targetType switch {
            ProviderType.SQLLiteDBOASIS=>Path.GetFullPath(ExistingSqlite(settings.SQLLiteDBOASIS.ConnectionString).DataSource),
            ProviderType.MongoDBOASIS=>StoreIdentity(settings.MongoDBOASIS.ConnectionString,settings.MongoDBOASIS.DBName),
            ProviderType.Neo4jOASIS=>NeoStoreIdentity(settings.Neo4jOASIS.ConnectionString,settings.Neo4jOASIS.Username),
            _=>throw new InvalidOperationException("Unresolved target profile.")
        };
        if(configuredSource!=request.SourceStoreIdentity || (targetType==ProviderType.SQLLiteDBOASIS
            ? request.TargetStoreIdentity!=null || targetIdentity!=Path.GetFullPath(request.TargetFile)
            : request.TargetFile!=null || targetIdentity!=request.TargetStoreIdentity))
            throw new InvalidOperationException("Configured endpoints differ from the reviewed request.");
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(60));
        if(targetType==ProviderType.MongoDBOASIS)
        {
            // Activation may create collections. Require the reviewed database to
            // already exist, rather than replacing a missing environment by accident.
            using var names=await new MongoClient(settings.MongoDBOASIS.ConnectionString).ListDatabaseNamesAsync(cancellationToken:deadline.Token);
            if(!(await names.ToListAsync(deadline.Token)).Contains(settings.MongoDBOASIS.DBName,StringComparer.Ordinal))
                throw new InvalidOperationException("Reviewed target Mongo database does not exist; no provisioning.");
        }
        IOASISStorageProvider source=sourceType switch {
            ProviderType.MongoDBOASIS=>new MongoDBOASIS(settings.MongoDBOASIS.ConnectionString,settings.MongoDBOASIS.DBName),
            ProviderType.Neo4jOASIS=>new NeoProvider(settings.Neo4jOASIS.ConnectionString,settings.Neo4jOASIS.Username,settings.Neo4jOASIS.Password),
            ProviderType.SQLLiteDBOASIS=>new SQLLiteDBOASIS(ExistingSqlite(settings.SQLLiteDBOASIS.ConnectionString).ToString()),
            _=>throw new InvalidOperationException("Unresolved source profile.")
        };
        IOASISStorageProvider target=targetType switch {
            ProviderType.SQLLiteDBOASIS=>new SQLLiteDBOASIS(ExistingSqlite(settings.SQLLiteDBOASIS.ConnectionString).ToString()),
            ProviderType.MongoDBOASIS=>new MongoDBOASIS(settings.MongoDBOASIS.ConnectionString,settings.MongoDBOASIS.DBName),
            ProviderType.Neo4jOASIS=>new NeoProvider(settings.Neo4jOASIS.ConnectionString,settings.Neo4jOASIS.Username,settings.Neo4jOASIS.Password),
            _=>throw new InvalidOperationException("Unresolved target profile.")
        };
        try
        {
            var activated=await source.ActivateProviderAsync();
            if(activated.IsError || activated.IsWarning)throw new InvalidOperationException("Source activation failed.");
            var queue=((IHolonReplicationOutboxSource)source).GetHolonReplicationOutbox();
            if(queue.IsError || queue.Result is not IHolonReplicationObligationReader outbox)
                throw new InvalidOperationException("Configured source outbox unavailable.");
            Func<HolonReplicaInitializationIntent,HolonReplicaInitializationOutcome,CancellationToken,Task<OASISResult<bool>>> complete=queue.Result switch {
                MongoHolonReplicationOutbox mongo=>mongo.CompleteInitializedBaselineAsync,
                NeoOutbox neo=>neo.CompleteInitializedBaselineAsync,
                SqliteHolonReplicationOutbox sqlite=>sqlite.CompleteInitializedBaselineAsync,
                _=>throw new InvalidOperationException("Configured source has no fenced completion owner.")
            };
            // Shell access authorizes reading this one reviewed source operation.
            // Verify its exact immutable digest before any target activation/write.
            var held=await outbox.ReadObligationAsync(request.Sequence,deadline.Token);
            if(held.IsError || held.IsWarning || held.Exception!=null || held.Result==null
                || held.Result.Inspection.OperationId!=request.SourceOperationId
                || held.Result.PayloadDigest!=request.SourcePayloadDigest)
                throw new InvalidOperationException("Reviewed source operation or payload changed.");
            var receiver=await target.ActivateProviderAsync();
            if(receiver.IsError || receiver.IsWarning)throw new InvalidOperationException("Target activation failed.");
            if(target is not ICommittedHolonReplicaInitializationRecoveryStorageProvider receiverOwner)
                throw new InvalidOperationException("Configured target has no initialization receipt owner.");
            var preparation=Preparation(request,sourceType,outbox,targetIdentity);
            var application=new HolonReplicaInitializationApplication(preparation,receiverOwner,complete);
            return await application.ExecuteAsync(request.OperatorId,request.Sequence,request.InitializationOperationId,protectedJournal,deadline.Token);
        }
        finally
        {
            try
            {
                try { await target.DeActivateProviderAsync(); }
                finally { (target as IDisposable)?.Dispose(); }
            }
            finally
            {
                try { await source.DeActivateProviderAsync(); }
                finally { (source as IDisposable)?.Dispose(); }
            }
        }
    }

    internal static HolonReplicaInitializationPreparation Preparation(Request request,ProviderType sourceType,
        IHolonReplicationObligationReader source,string targetFile)
        =>new("web4-demo-repair",request.CatalogVersion,HolonReplicationRoutes.SourceId(sourceType),request.SourceStoreIdentity,
            sourceType,source,HolonReplicationRoutes.TargetId(TargetProfile(request.TargetProvider)),targetFile,TargetProfile(request.TargetProvider),
            new Policy(request,targetFile,sourceType));

    private sealed class Policy(Request request,string file,ProviderType sourceType) : IHolonReplicaInitializationPolicy
    {
        public string BindingIdentity=>"reviewed-demo-baseline:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicaInitializationAuthorizationContext context,CancellationToken token)
            =>Task.FromResult(new OASISResult<bool>(context.OperatorId==request.OperatorId && context.Sequence==request.Sequence
                && context.InitializationOperationId==request.InitializationOperationId
                && context.SourceId==HolonReplicationRoutes.SourceId(sourceType)
                && context.SourceStoreIdentity==request.SourceStoreIdentity && context.TargetEndpointIdentity==file
                && context.SourceAuthority==sourceType && context.TargetAuthority==TargetProfile(request.TargetProvider)
                && context.TargetId==HolonReplicationRoutes.TargetId(TargetProfile(request.TargetProvider))
                && (context.Mutation==null || context.Mutation.ActorId==request.SourceActorId
                    && context.Mutation.Roots.Count==1 && context.Mutation.Roots[0].Id==request.RootId)));
    }
}
