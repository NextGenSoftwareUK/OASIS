using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;

/// <summary>Trusted shell command: returns one reviewed held MongoDB route head
/// to Pending after the host fix for its failure code is deployed. Credentials
/// come only from host DNA. No HTTP or background host is started.</summary>
internal static class HeldHeadRearmCommand
{
    internal sealed record Request(Guid OperatorId, long Sequence, Guid SourceOperationId,
        string SourcePayloadDigest, string TargetId, string ErrorCode, string SourceStoreIdentity);

    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length != 2 || Environment.GetEnvironmentVariable("OASIS_SANDBOX") != "true")
                throw new InvalidOperationException("Explicit demo shell context required.");
            var bytes = await File.ReadAllBytesAsync(Path.GetFullPath(args[1]));
            var request = Parse(bytes);
            var dna = Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>(Environment.GetEnvironmentVariable("OASIS_DNA_JSON")
                ?? throw new InvalidOperationException("Configured DNA required."));
            var result = await ExecuteAsync(dna, request);
            Console.WriteLine(JsonSerializer.Serialize(new { success = !result.IsError && result.Result, code = result.ErrorCode, sequence = request.Sequence }));
            return !result.IsError && result.Result ? 0 : 1;
        }
        catch (Exception)
        {
            // Native exception text can contain configured credentials or payload.
            Console.Error.WriteLine("COSMIC_REPLICA_REARM_COMMAND_FAILED: the held entry is unchanged unless success was printed.");
            return 1;
        }
    }

    internal static Request Parse(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length > 4096) throw new InvalidDataException("Reviewed re-arm request exceeds its byte budget.");
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 2 });
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("A reviewed re-arm object is required.");
        var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
            if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate reviewed re-arm field.");
        var request = document.RootElement.Deserialize<Request>(new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
            ?? throw new InvalidDataException("A reviewed re-arm object is required.");
        if (request.OperatorId == Guid.Empty || request.Sequence <= 0 || request.SourceOperationId == Guid.Empty
            || request.SourcePayloadDigest?.Length != 64 || string.IsNullOrWhiteSpace(request.TargetId)
            || !MongoHolonReplicationOutbox.IsRearmableHeldCode(request.ErrorCode) || string.IsNullOrWhiteSpace(request.SourceStoreIdentity))
            throw new InvalidDataException("A complete reviewed re-arm request is required.");
        return request;
    }

    internal static async Task<OASISResult<bool>> ExecuteAsync(OASISDNA dna, Request request)
    {
        var settings = dna?.OASIS?.StorageProviders?.MongoDBOASIS ?? throw new InvalidOperationException("Configured MongoDB required.");
        if (ReplicaBaselineRepairCommand.StoreIdentity(settings.ConnectionString, settings.DBName) != request.SourceStoreIdentity)
            throw new InvalidOperationException("Configured endpoints differ from the reviewed request.");
        var source = new MongoDBOASIS(settings.ConnectionString, settings.DBName);
        try
        {
            var activated = await source.ActivateProviderAsync();
            if (activated.IsError || activated.IsWarning) throw new InvalidOperationException("Source activation failed.");
            var outbox = source.GetHolonReplicationOutbox();
            if (outbox.IsError || outbox.Result is not MongoHolonReplicationOutbox mongo)
                throw new InvalidOperationException("Configured source outbox unavailable.");
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            return await mongo.RearmHeldHeadAsync(request.OperatorId, request.Sequence, request.SourceOperationId,
                request.SourcePayloadDigest, request.TargetId, request.ErrorCode, deadline.Token);
        }
        finally
        {
            try { await source.DeActivateProviderAsync(); }
            finally { (source as IDisposable)?.Dispose(); }
        }
    }
}
