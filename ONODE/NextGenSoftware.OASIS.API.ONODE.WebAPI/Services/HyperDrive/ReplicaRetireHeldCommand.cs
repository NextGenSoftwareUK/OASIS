using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NeoProvider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using NeoOutbox = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.NeoHolonReplicationOutbox;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;

/// <summary>Trusted shell command: retires one reviewed held Neo4j → SQLite lineage
/// conflict after the owner chose the newer SQLite head. It delivers nothing. It first
/// re-reads the held payload and the SQLite head and refuses unless both still match the
/// reviewed request exactly, then completes the obligation with a fenced retirement
/// acknowledgement. Re-running the same request is idempotent. No HTTP host is started.</summary>
internal static class ReplicaRetireHeldCommand
{
    internal sealed record Request(Guid OperatorId, long Sequence, Guid SourceOperationId, int AttemptCount,
        string SourcePayloadDigest, string SourceStoreIdentity, string TargetFile, Guid RootId,
        Guid PayloadVersionId, int PayloadVersion, Guid SupersedingVersionId, int SupersedingVersion);

    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length != 2 || Environment.GetEnvironmentVariable("OASIS_SANDBOX") != "true")
                throw new InvalidOperationException("Usage in the demo shell: --cosmic-retire-held <reviewed-request.json>.");
            var request = Parse(await System.IO.File.ReadAllBytesAsync(Path.GetFullPath(args[1])));
            var dna = Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>(Environment.GetEnvironmentVariable("OASIS_DNA_JSON")
                ?? throw new InvalidOperationException("Configured DNA required."));
            var changed = await ExecuteAsync(dna, request);
            Console.WriteLine(JsonSerializer.Serialize(new { success = true, retired = changed, sequence = request.Sequence }));
            return 0;
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException or InvalidDataException)
        {
            Console.Error.WriteLine("COSMIC_REPLICA_RETIRE_REFUSED: " + error.Message + " The held obligation is unchanged.");
            return 1;
        }
        catch (Exception)
        {
            // Native exception text can contain configured credentials or payload.
            Console.Error.WriteLine("COSMIC_REPLICA_RETIRE_FAILED: native call failed; re-inspect before any retry.");
            return 1;
        }
    }

    internal static Request Parse(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length > 4096) throw new InvalidDataException("Reviewed retirement exceeds its byte budget.");
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 2 });
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("A reviewed retirement object is required.");
        var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
            if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate reviewed retirement field.");
        var request = document.RootElement.Deserialize<Request>(new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })
            ?? throw new InvalidDataException("A reviewed retirement object is required.");
        if (request.OperatorId == Guid.Empty || request.Sequence <= 0 || request.SourceOperationId == Guid.Empty || request.AttemptCount <= 0
            || request.SourcePayloadDigest?.Length != 64 || string.IsNullOrWhiteSpace(request.SourceStoreIdentity)
            || string.IsNullOrWhiteSpace(request.TargetFile) || request.RootId == Guid.Empty
            || request.PayloadVersionId == Guid.Empty || request.SupersedingVersionId == Guid.Empty
            || request.SupersedingVersion <= request.PayloadVersion)
            throw new InvalidDataException("A complete reviewed retirement with a newer superseding head is required.");
        return request;
    }

    internal static async Task<bool> ExecuteAsync(OASISDNA dna, Request request)
    {
        var settings = dna?.OASIS?.StorageProviders ?? throw new InvalidOperationException("Configured providers required.");
        if (ReplicaBaselineRepairCommand.NeoStoreIdentity(settings.Neo4jOASIS.ConnectionString, settings.Neo4jOASIS.Username) != request.SourceStoreIdentity)
            throw new InvalidOperationException("Configured Neo4j store differs from the reviewed request.");
        if (Path.GetFullPath(new SqliteConnectionStringBuilder(settings.SQLLiteDBOASIS.ConnectionString).DataSource) != request.TargetFile)
            throw new InvalidOperationException("Configured SQLite file differs from the reviewed request.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var source = new NeoProvider(settings.Neo4jOASIS.ConnectionString, settings.Neo4jOASIS.Username, settings.Neo4jOASIS.Password);
        try
        {
            var activated = await source.ActivateProviderAsync();
            if (activated.IsError || activated.IsWarning) throw new InvalidOperationException("Source activation failed.");
            var outbox = ((IHolonReplicationOutboxSource)source).GetHolonReplicationOutbox();
            if (outbox.IsError || outbox.Result is not NeoOutbox neo) throw new InvalidOperationException("Configured Neo4j outbox unavailable.");
            var held = await neo.ReadObligationAsync(request.Sequence, deadline.Token);
            if (held.IsError || held.Result == null) throw new InvalidOperationException("The obligation could not be read.");
            var payload = CommittedHolonReplicaPayload.Read(held.Result.PayloadJson);
            if (held.Result.Inspection.TargetId != "hyperdrive-target:SQLLiteDBOASIS" || payload.Roots.Count != 1
                || payload.Roots[0].Id != request.RootId || payload.Roots[0].VersionId != request.PayloadVersionId
                || payload.Roots[0].Version != request.PayloadVersion)
                throw new InvalidOperationException("Held payload differs from the reviewed request.");
            await VerifySupersedingHeadAsync(settings.SQLLiteDBOASIS.ConnectionString, request);
            var evidence = JsonSerializer.Serialize(new
            {
                target = "SQLLiteDBOASIS", targetFile = request.TargetFile, rootId = request.RootId.ToString("D"),
                retiredVersion = request.PayloadVersion, retiredVersionId = request.PayloadVersionId.ToString("D"),
                keptVersion = request.SupersedingVersion, keptVersionId = request.SupersedingVersionId.ToString("D")
            });
            var result = await neo.RetireSupersededHeldHeadAsync(request.OperatorId, request.Sequence, request.SourceOperationId,
                request.AttemptCount, request.SourcePayloadDigest, evidence, deadline.Token);
            if (result.IsError) throw new InvalidOperationException("Retirement refused: " + result.ErrorCode);
            return result.Result;
        }
        finally
        {
            try { await source.DeActivateProviderAsync(); }
            finally { (source as IDisposable)?.Dispose(); }
        }
    }

    private static async Task VerifySupersedingHeadAsync(string connectionString, Request request)
    {
        var sqlite = new SQLLiteDBOASIS(connectionString);
        var opened = await sqlite.ActivateProviderAsync();
        if (opened.IsError) throw new InvalidOperationException("SQLite activation failed.");
        try
        {
            var current = await sqlite.LoadHolonAsync(request.RootId, loadChildren: false, recursive: false);
            if (current.IsError || current.Result == null || current.Result.VersionId != request.SupersedingVersionId
                || current.Result.Version != request.SupersedingVersion)
                throw new InvalidOperationException("The SQLite head is no longer the reviewed superseding version.");
        }
        finally { await sqlite.DeActivateProviderAsync(); (sqlite as IDisposable)?.Dispose(); }
    }
}
