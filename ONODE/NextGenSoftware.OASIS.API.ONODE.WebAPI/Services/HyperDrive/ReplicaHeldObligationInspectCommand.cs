using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NeoProvider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;

/// <summary>Trusted, read-only shell inspection of one held replication obligation, used to prepare a
/// reviewed --cosmic-repair-baseline request. It prints only identities, state and digests: never the
/// private payload. It reads through the same trusted obligation reader the repair uses and never
/// claims, renews, finishes or replays a delivery. No HTTP host is started.</summary>
internal static class ReplicaHeldObligationInspectCommand
{
    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length != 3 || Environment.GetEnvironmentVariable("OASIS_SANDBOX") != "true"
                || !long.TryParse(args[2], out var sequence) || sequence <= 0)
                throw new InvalidOperationException("Usage in the demo shell: --cosmic-inspect-held <MongoDBOASIS|Neo4jOASIS|SQLLiteDBOASIS> <sequence>.");
            var sourceType = ReplicaBaselineRepairCommand.SourceProfile(args[1]);
            var dna = Newtonsoft.Json.JsonConvert.DeserializeObject<OASISDNA>(Environment.GetEnvironmentVariable("OASIS_DNA_JSON")
                ?? throw new InvalidOperationException("Configured DNA required."));
            var settings = dna.OASIS.StorageProviders;
            var sourceStoreIdentity = sourceType switch
            {
                ProviderType.MongoDBOASIS => ReplicaBaselineRepairCommand.StoreIdentity(settings.MongoDBOASIS.ConnectionString, settings.MongoDBOASIS.DBName),
                ProviderType.Neo4jOASIS => ReplicaBaselineRepairCommand.NeoStoreIdentity(settings.Neo4jOASIS.ConnectionString, settings.Neo4jOASIS.Username),
                ProviderType.SQLLiteDBOASIS => ReplicaBaselineRepairCommand.SqliteStoreIdentity(settings.SQLLiteDBOASIS.ConnectionString),
                _ => throw new InvalidOperationException("Unresolved source profile.")
            };
            var sqliteFile = Path.GetFullPath(new SqliteConnectionStringBuilder(settings.SQLLiteDBOASIS.ConnectionString).DataSource);
            IOASISStorageProvider source = sourceType switch
            {
                ProviderType.MongoDBOASIS => new MongoDBOASIS(settings.MongoDBOASIS.ConnectionString, settings.MongoDBOASIS.DBName),
                ProviderType.Neo4jOASIS => new NeoProvider(settings.Neo4jOASIS.ConnectionString, settings.Neo4jOASIS.Username, settings.Neo4jOASIS.Password),
                ProviderType.SQLLiteDBOASIS => new SQLLiteDBOASIS(settings.SQLLiteDBOASIS.ConnectionString),
                _ => throw new InvalidOperationException("Unresolved source profile.")
            };
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try
            {
                var activated = await source.ActivateProviderAsync();
                if (activated.IsError || activated.IsWarning) throw new InvalidOperationException("Source activation failed.");
                var outbox = ((IHolonReplicationOutboxSource)source).GetHolonReplicationOutbox();
                if (outbox.IsError || outbox.Result is not IHolonReplicationObligationReader reader)
                    throw new InvalidOperationException("Configured source outbox unavailable.");
                var held = await reader.ReadObligationAsync(sequence, deadline.Token);
                if (held.IsError || held.Result == null)
                    throw new InvalidOperationException("The obligation could not be read.");
                var inspection = held.Result.Inspection;
                var payload = CommittedHolonReplicaPayload.Read(held.Result.PayloadJson);
                var route = inspection.NativeRoute;
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    sourceProvider = sourceType.ToString(),
                    sequence = inspection.Sequence,
                    operationId = inspection.OperationId,
                    targetId = inspection.TargetId,
                    state = inspection.State.ToString(),
                    attemptCount = inspection.AttemptCount,
                    errorCode = inspection.ErrorCode,
                    payloadDigest = held.Result.PayloadDigest,
                    actorId = payload.ActorId,
                    operation = payload.Operation.ToString(),
                    rootIds = payload.Roots.Select(root => root.Id).ToArray(),
                    sourceStoreIdentity,
                    sqliteTargetFile = sqliteFile,
                    nativeRoute = route == null ? null : new
                    {
                        sourceEndpointFingerprint = route.SourceStorage.EndpointFingerprint,
                        sourceNativeStoreId = route.SourceStorage.StoreId,
                        targetEndpointFingerprint = route.TargetStorage.EndpointFingerprint,
                        targetNativeStoreId = route.TargetStorage.StoreId
                    }
                }, new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }
            finally
            {
                try { await source.DeActivateProviderAsync(); }
                finally { (source as IDisposable)?.Dispose(); }
            }
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine("COSMIC_REPLICA_INSPECT_FAILED: " + error.Message);
            return 1;
        }
        catch (Exception)
        {
            // Native exception text can contain configured credentials or payload.
            Console.Error.WriteLine("COSMIC_REPLICA_INSPECT_FAILED: native read failed; inspect sanitized evidence.");
            return 1;
        }
    }
}
