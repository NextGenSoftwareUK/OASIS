using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class SupabaseSqliteHyperDriveTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task RoutesAcrossNativeSupabaseAndSqlite(bool sync)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_SUPABASE_ROUTING_DIRECTORY");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(fixture));
        fixture = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(fixture));
        StringAssert.StartsWith(Path.GetFileName(fixture), "oasis-postgrest.");
        const string endpoint = "http://127.0.0.1:18091";
        Assert.AreEqual(endpoint, Environment.GetEnvironmentVariable("OASIS_SUPABASE_FIRST_POSTGREST"));
        var key = Environment.GetEnvironmentVariable("OASIS_SUPABASE_FIRST_JWT");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));

        var sqlitePath = Path.Combine(fixture, sync ? "routing-sync.db" : "routing-async.db");
        Assert.IsFalse(File.Exists(sqlitePath));
        using var sqlite = new SqliteProvider($"Data Source={sqlitePath};Pooling=False");
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(3) };
        var supabase = new SupabaseOASIS(endpoint, key!, client, "");
        var attempted = new List<Guid>();
        try
        {
            var supabaseActivation = await supabase.ActivateProviderAsync();
            var sqliteActivation = await sqlite.ActivateProviderAsync();
            Assert.IsTrue(!supabaseActivation.IsError && supabaseActivation.Result, supabaseActivation.Message);
            Assert.IsTrue(!sqliteActivation.IsError && sqliteActivation.Result, sqliteActivation.Message);
            var binding = await supabase.CaptureStorageIdentityAsync();
            Assert.IsFalse(binding.IsError, binding.Message);
            Assert.AreEqual(ProviderType.SupabaseOASIS, binding.Result.Authority);
            var replica = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid() };
            CosmicConcurrency.SetProviderRevisionId(replica, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            var receiptIntent = new CommittedHolonReplicaRequest(Guid.NewGuid(), "owned-sqlite",
                ProviderType.SQLLiteDBOASIS, Guid.NewGuid(), ConditionalHolonOperation.Save,
                new[] { replica }, saveChildren: false);
            var absentReceipt = await supabase.ReadCommittedHolonReplicaReceiptAsync(
                new CommittedHolonReplicaReceiptReadRequest(receiptIntent, binding.Result));
            Assert.IsFalse(absentReceipt.IsError, $"{absentReceipt.Message} {absentReceipt.Exception}");
            Assert.AreEqual(ConditionalHolonReceiptState.Absent, absentReceipt.Result.ReceiptState);
            var wrongBinding = new CosmicStorageIdentity(binding.Result.Authority, binding.Result.EndpointFingerprint, Guid.NewGuid());
            var wrongStore = await supabase.ReadCommittedHolonReplicaReceiptAsync(
                new CommittedHolonReplicaReceiptReadRequest(receiptIntent, wrongBinding));
            Assert.AreEqual("COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH", wrongStore.ErrorCode);
            using (var stateRequest = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/rpc/oasis_read_replica_state"))
            {
                stateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                stateRequest.Content = JsonContent.Create(new
                {
                    expected_store = Guid.Parse(binding.Result.StoreId),
                    requested_ids = new[] { replica.Id }
                });
                using var stateResponse = await client.SendAsync(stateRequest);
                var stateBody = await stateResponse.Content.ReadAsStringAsync();
                Assert.IsTrue(stateResponse.IsSuccessStatusCode, stateBody);
                using var stateDocument = JsonDocument.Parse(stateBody);
                var state = stateDocument.RootElement;
                Assert.AreEqual(1, state.GetArrayLength());
                Assert.IsTrue(state[0].GetProperty("store_matches").GetBoolean());
                Assert.AreEqual(replica.Id, state[0].GetProperty("id").GetGuid());
                Assert.AreEqual(JsonValueKind.Null, state[0].GetProperty("current_row").ValueKind);
                Assert.AreEqual(JsonValueKind.Null, state[0].GetProperty("replica_head").ValueKind);
            }
            attempted.Add(replica.Id);
            var applied = sync ? supabase.ApplyBoundCommittedHolonReplica(receiptIntent, binding.Result)
                : await supabase.ApplyBoundCommittedHolonReplicaAsync(receiptIntent, binding.Result);
            Assert.IsFalse(applied.IsError, $"{applied.Message} {applied.Exception}");
            var replay = await supabase.ApplyBoundCommittedHolonReplicaAsync(receiptIntent, binding.Result);
            Assert.IsFalse(replay.IsError, $"{replay.Message} {replay.Exception}");
            Assert.AreEqual(applied.Result.Single().VersionId, replay.Result.Single().VersionId);
            Assert.AreEqual(replica.VersionId, (await supabase.LoadHolonAsync(replica.Id, loadChildren: false)).Result?.VersionId);
            var committed = await supabase.ReadCommittedHolonReplicaReceiptAsync(
                new CommittedHolonReplicaReceiptReadRequest(receiptIntent, binding.Result));
            Assert.IsFalse(committed.IsError, $"{committed.Message} {committed.Exception}");
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, committed.Result.ReceiptState);

            var successor = new Holon { Id = replica.Id, Version = 2, VersionId = Guid.NewGuid(),
                PreviousVersionId = Guid.NewGuid(), Name = "Supabase replica successor", IsActive = true };
            CosmicConcurrency.SetProviderRevisionId(successor, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            CommittedHolonReplicaRequest NextReplica(ConditionalHolonOperation operation, Holon snapshot)
                => new(Guid.NewGuid(), receiptIntent.SourceId, receiptIntent.SourceAuthority, receiptIntent.ActorId,
                    operation, new[] { snapshot }, saveChildren: false);
            var divergent = NextReplica(ConditionalHolonOperation.Save, successor);
            var rejectedReplica = await supabase.ApplyBoundCommittedHolonReplicaAsync(divergent, binding.Result);
            Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT", rejectedReplica.ErrorCode,
                $"{rejectedReplica.Message} {rejectedReplica.Exception}");
            var rejectedReceipt = await supabase.ReadCommittedHolonReplicaReceiptAsync(
                new CommittedHolonReplicaReceiptReadRequest(divergent, binding.Result));
            Assert.IsFalse(rejectedReceipt.IsError, rejectedReceipt.Message);
            Assert.AreEqual(ConditionalHolonReceiptState.Absent, rejectedReceipt.Result.ReceiptState);
            Assert.AreEqual(replica.VersionId, (await supabase.LoadHolonAsync(replica.Id, loadChildren: false)).Result?.VersionId);

            successor.PreviousVersionId = replica.VersionId;
            var advancedReplica = await supabase.ApplyBoundCommittedHolonReplicaAsync(
                NextReplica(ConditionalHolonOperation.Save, successor), binding.Result);
            Assert.IsFalse(advancedReplica.IsError, $"{advancedReplica.Message} {advancedReplica.Exception}");
            Assert.AreEqual(successor.VersionId, (await supabase.LoadHolonAsync(replica.Id, loadChildren: false)).Result?.VersionId);
            var tombstone = new Holon { Id = replica.Id, Version = 3, VersionId = Guid.NewGuid(),
                PreviousVersionId = successor.VersionId, Name = successor.Name, IsActive = false,
                DeletedDate = DateTime.UtcNow, DeletedByAvatarId = receiptIntent.ActorId };
            CosmicConcurrency.SetProviderRevisionId(tombstone, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            var deletion = NextReplica(ConditionalHolonOperation.HardDelete, tombstone);
            var deletedReplica = sync ? supabase.ApplyBoundCommittedHolonReplica(deletion, binding.Result)
                : await supabase.ApplyBoundCommittedHolonReplicaAsync(deletion, binding.Result);
            Assert.IsTrue(!deletedReplica.IsError && deletedReplica.IsDeleted,
                $"{deletedReplica.Message} {deletedReplica.Exception}");
            var deletionReceipt = await supabase.ReadCommittedHolonReplicaReceiptAsync(
                new CommittedHolonReplicaReceiptReadRequest(deletion, binding.Result));
            Assert.IsFalse(deletionReceipt.IsError, deletionReceipt.Message);
            Assert.AreEqual(ConditionalHolonReceiptState.Committed, deletionReceipt.Result.ReceiptState);
            var deletedRead = await supabase.LoadHolonAsync(replica.Id, loadChildren: false);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, deletedRead.ErrorCode);

            var reopened = new SupabaseOASIS(endpoint, key!, client, "");
            try
            {
                var activation = await reopened.ActivateProviderAsync();
                Assert.IsFalse(activation.IsError, activation.Message);
                var reopenedBinding = await reopened.CaptureStorageIdentityAsync();
                Assert.IsFalse(reopenedBinding.IsError, reopenedBinding.Message);
                Assert.AreEqual(binding.Result.BindingFingerprint, reopenedBinding.Result.BindingFingerprint);
            }
            finally
            {
                await reopened.DeActivateProviderAsync();
            }

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var providerTypes = new[] { ProviderType.SupabaseOASIS, ProviderType.SQLLiteDBOASIS };
            var candidates = providerTypes.Select(type => new EnumValue<ProviderType>(type)).ToArray();
            var replication = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true };
            replication.RegisterProvider(supabase);
            replication.RegisterProvider(sqlite);
            Assert.IsFalse(replication.SetAndReplaceAutoReplicationListForProviders(candidates).IsError);
            var replicationRouter = new OASISHyperDrive(replication);
            var records = new List<Holon>();
            foreach (var primary in providerTypes)
            {
                var holon = new Holon { Id = Guid.NewGuid(), Name = $"priority routing {primary}", CreatedByAvatarId = Guid.NewGuid() };
                attempted.Add(holon.Id);
                var request = new StorageOperationRequest { Operation = "SaveHolon", PreferredProvider = primary, Payload = holon, SaveChildren = false };
                var saved = sync ? replicationRouter.RouteRequest<IHolon>(request) : await replicationRouter.RouteRequestAsync<IHolon>(request);
                Assert.IsFalse(saved.IsError || saved.IsWarning, saved.Message);
                foreach (var provider in new IOASISStorageProvider[] { supabase, sqlite })
                {
                    var direct = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
                    Assert.IsFalse(direct.IsError, direct.Message);
                    AssertRecord(holon, direct.Result);
                }
                records.Add(holon);
            }

            var balancing = new ProviderManager(null, dna) { IsAutoLoadBalanceEnabled = true };
            balancing.RegisterProvider(supabase);
            balancing.RegisterProvider(sqlite);
            Assert.IsFalse(balancing.SetAndReplaceAutoLoadBalanceListForProviders(candidates).IsError);
            var balancedRouter = new OASISHyperDrive(balancing);
            var selected = records[0];
            var served = new List<string>();
            for (var index = 0; index < 6; index++)
            {
                var read = new StorageOperationRequest { Operation = "LoadHolon", HolonId = selected.Id, LoadChildren = false };
                var result = sync ? balancedRouter.RouteRequest<IHolon>(read, LoadBalancingStrategy.RoundRobin)
                    : await balancedRouter.RouteRequestAsync<IHolon>(read, LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(result.IsError, result.Message);
                AssertRecord(selected, result.Result);
                using var route = JsonDocument.Parse(result.MetaData["hyperDriveRoute"]);
                var initial = route.RootElement.GetProperty("InitialProvider").GetString();
                Assert.AreEqual(initial, route.RootElement.GetProperty("ServingProvider").GetString());
                Assert.AreEqual("RoundRobin", route.RootElement.GetProperty("RequestedStrategy").GetString());
                served.Add(initial!);
            }
            foreach (var provider in providerTypes)
                Assert.AreEqual(3, served.Count(value => value == provider.ToString()));

            var sqliteOnly = new Holon { Id = Guid.NewGuid(), Name = "SQLite-only holon", CreatedByAvatarId = Guid.NewGuid() };
            var sqliteSave = await sqlite.SaveHolonAsync(sqliteOnly, saveChildren: false);
            Assert.IsTrue(!sqliteSave.IsError && sqliteSave.IsSaved, sqliteSave.Message);
            AssertRecord(sqliteOnly, (await sqlite.LoadHolonAsync(sqliteOnly.Id, loadChildren: false)).Result);

            var healthyFailover = new ProviderManager(null, dna) { IsAutoFailOverEnabled = true };
            healthyFailover.RegisterProvider(supabase);
            healthyFailover.RegisterProvider(sqlite);
            Assert.IsFalse(healthyFailover.SetAndReplaceAutoFailOverListForProviders(candidates).IsError);
            var healthyRouter = new OASISHyperDrive(healthyFailover);
            var missing = new StorageOperationRequest
            {
                Operation = "LoadHolonByProviderKey", ProviderKey = sqliteOnly.Id.ToString("D"),
                PreferredProvider = ProviderType.SupabaseOASIS, LoadChildren = false
            };
            var absent = sync ? healthyRouter.RouteRequest<IHolon>(missing)
                : await healthyRouter.RouteRequestAsync<IHolon>(missing);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, absent.ErrorCode, absent.Message);
            Assert.IsNull(absent.Result);
            Assert.AreEqual(0L, healthyFailover.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);

            using var unavailableEndpoint = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            unavailableEndpoint.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            var unavailableUrl = $"http://127.0.0.1:{((IPEndPoint)unavailableEndpoint.LocalEndPoint!).Port}";
            var unavailableSupabase = new SupabaseOASIS(unavailableUrl, key!, client, "");
            await VerifyFailoverAsync(dna, candidates, selected, sync, unavailableSupabase, sqlite);

            var unavailablePath = Path.Combine(fixture, "missing.db");
            using var unavailableSqlite = new SqliteProvider($"Data Source={unavailablePath};Mode=ReadWrite;Pooling=False;Default Timeout=1");
            await VerifyFailoverAsync(dna, candidates, selected, sync, unavailableSqlite, supabase);
            Assert.IsFalse(File.Exists(unavailablePath));
        }
        finally
        {
            try
            {
                foreach (var id in attempted)
                {
                    var url = $"{endpoint}/oasis_holons?id=eq.{id:D}";
                    using var delete = new HttpRequestMessage(HttpMethod.Delete, url);
                    delete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    using var deleted = await client.SendAsync(delete);
                    deleted.EnsureSuccessStatusCode();
                    using var read = new HttpRequestMessage(HttpMethod.Get, url);
                    read.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    using var response = await client.SendAsync(read);
                    response.EnsureSuccessStatusCode();
                    using var rows = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    Assert.AreEqual(0, rows.RootElement.GetArrayLength());
                }
            }
            finally
            {
                await supabase.DeActivateProviderAsync();
            }
            await sqlite.DeActivateProviderAsync();
        }
    }

    private static async Task VerifyFailoverAsync(OASISDNA dna, EnumValue<ProviderType>[] candidates,
        Holon expected, bool sync, IOASISStorageProvider unavailable, IOASISStorageProvider replica)
    {
        var failover = new ProviderManager(null, dna) { IsAutoFailOverEnabled = false };
        failover.RegisterProvider(unavailable);
        failover.RegisterProvider(replica);
        Assert.IsFalse(failover.SetAndReplaceAutoFailOverListForProviders(candidates).IsError);
        var router = new OASISHyperDrive(failover);
        var source = unavailable.ProviderType.Value;
        StorageOperationRequest ReadSource() => new() { Operation = "LoadHolon", HolonId = expected.Id,
            LoadChildren = false, PreferredProvider = source };

        var rejected = sync ? router.RouteRequest<IHolon>(ReadSource()) : await router.RouteRequestAsync<IHolon>(ReadSource());
        Assert.IsTrue(rejected.IsError, rejected.Message);
        failover.IsAutoFailOverEnabled = true;
        var recovered = sync ? router.RouteRequest<IHolon>(ReadSource()) : await router.RouteRequestAsync<IHolon>(ReadSource());
        Assert.IsFalse(recovered.IsError, recovered.Message);
        AssertRecord(expected, recovered.Result);
        using var route = JsonDocument.Parse(recovered.MetaData["hyperDriveRoute"]);
        Assert.AreEqual(source.ToString(), route.RootElement.GetProperty("RequestedProvider").GetString());
        Assert.AreEqual(replica.ProviderType.Value.ToString(), route.RootElement.GetProperty("ServingProvider").GetString());
    }

    private static void AssertRecord(Holon expected, IHolon? actual)
    {
        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Id, actual.Id);
        Assert.AreEqual(expected.Name, actual.Name);
        Assert.AreEqual(expected.CreatedByAvatarId, actual.CreatedByAvatarId);
    }
}
