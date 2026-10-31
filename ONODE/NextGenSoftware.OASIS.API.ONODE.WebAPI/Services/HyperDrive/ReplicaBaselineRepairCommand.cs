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

/// <summary>Trusted shell command for explicitly reviewed Mongo/Neo4j-to-SQLite
/// routes. The request may name only these source profiles; native endpoints and
/// credentials come exclusively from host DNA. No HTTP/background host is started.</summary>
internal static class ReplicaBaselineRepairCommand
{
    internal sealed record Request(Guid OperatorId, Guid InitializationOperationId, Guid CatalogVersion,
        long Sequence, Guid SourceOperationId, Guid SourceActorId, Guid RootId,
        string SourcePayloadDigest, string SourceStoreIdentity, string TargetFile,
        [property:JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] string SourceProvider=null);

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
        _=>throw new ArgumentException("Only explicit MongoDB or Neo4j source profiles are supported.")
    };

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
        var settings=dna.OASIS.StorageProviders;
        var configuredSource=sourceType==ProviderType.MongoDBOASIS
            ? StoreIdentity(settings.MongoDBOASIS.ConnectionString,settings.MongoDBOASIS.DBName)
            : NeoStoreIdentity(settings.Neo4jOASIS.ConnectionString,settings.Neo4jOASIS.Username);
        var connection=new SqliteConnectionStringBuilder(settings.SQLLiteDBOASIS.ConnectionString);
        var targetFile=Path.GetFullPath(connection.DataSource);
        if(configuredSource!=request.SourceStoreIdentity || targetFile!=Path.GetFullPath(request.TargetFile)
            || !System.IO.File.Exists(targetFile))throw new InvalidOperationException("Configured endpoints differ from the reviewed request.");
        connection.Mode=SqliteOpenMode.ReadWrite; // This command must never create a replacement target database.
        IOASISStorageProvider source=sourceType==ProviderType.MongoDBOASIS
            ? new MongoDBOASIS(settings.MongoDBOASIS.ConnectionString,settings.MongoDBOASIS.DBName)
            : new NeoProvider(settings.Neo4jOASIS.ConnectionString,settings.Neo4jOASIS.Username,settings.Neo4jOASIS.Password);
        using var target=new SQLLiteDBOASIS(connection.ToString());
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(60));
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
            var preparation=Preparation(request,sourceType,outbox,targetFile);
            var application=new HolonReplicaInitializationApplication(preparation,target,complete);
            return await application.ExecuteAsync(request.OperatorId,request.Sequence,request.InitializationOperationId,protectedJournal,deadline.Token);
        }
        finally { await source.DeActivateProviderAsync(); }
    }

    internal static HolonReplicaInitializationPreparation Preparation(Request request,ProviderType sourceType,
        IHolonReplicationObligationReader source,string targetFile)
        =>new("web4-demo-repair",request.CatalogVersion,HolonReplicationRoutes.SourceId(sourceType),request.SourceStoreIdentity,
            sourceType,source,HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS),targetFile,ProviderType.SQLLiteDBOASIS,
            new Policy(request,targetFile,sourceType));

    private sealed class Policy(Request request,string file,ProviderType sourceType) : IHolonReplicaInitializationPolicy
    {
        public string BindingIdentity=>"reviewed-demo-baseline:"+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))));
        public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicaInitializationAuthorizationContext context,CancellationToken token)
            =>Task.FromResult(new OASISResult<bool>(context.OperatorId==request.OperatorId && context.Sequence==request.Sequence
                && context.InitializationOperationId==request.InitializationOperationId
                && context.SourceId==HolonReplicationRoutes.SourceId(sourceType)
                && context.SourceStoreIdentity==request.SourceStoreIdentity && context.TargetEndpointIdentity==file
                && context.SourceAuthority==sourceType && context.TargetAuthority==ProviderType.SQLLiteDBOASIS
                && context.TargetId==HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS)
                && (context.Mutation==null || context.Mutation.ActorId==request.SourceActorId
                    && context.Mutation.Roots.Count==1 && context.Mutation.Roots[0].Id==request.RootId)));
    }
}
