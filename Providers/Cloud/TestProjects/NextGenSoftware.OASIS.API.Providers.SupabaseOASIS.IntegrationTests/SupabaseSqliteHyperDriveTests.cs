using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
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
            var conditionalHolon = new Holon { Id = Guid.NewGuid(), Name = "Supabase conditional source", IsActive = true };
            attempted.Add(conditionalHolon.Id);
            var conditionalIntent = new ConditionalHolonWriteRequest(
                new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SupabaseOASIS,
                    new[] { new HolonWriteCondition(conditionalHolon.Id, null) }),
                ConditionalHolonOperation.Save, new[] { conditionalHolon }, saveChildren: false);
            var conditionalSave = sync ? supabase.ExecuteConditionalHolonWrite(conditionalIntent)
                : await supabase.ExecuteConditionalHolonWriteAsync(conditionalIntent);
            Assert.IsTrue(!conditionalSave.IsError && conditionalSave.IsSaved,
                $"{conditionalSave.Message} {conditionalSave.Exception}");
            var conditionalReplay = await supabase.ExecuteConditionalHolonWriteAsync(conditionalIntent);
            Assert.IsFalse(conditionalReplay.IsError, conditionalReplay.Message);
            Assert.AreEqual(conditionalSave.Result.Single().VersionId, conditionalReplay.Result.Single().VersionId);
            Assert.AreEqual(conditionalSave.Result.Single().VersionId,
                (await supabase.LoadHolonAsync(conditionalHolon.Id, loadChildren: false)).Result?.VersionId);
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

            var sourceIdentity = await sqlite.CaptureStorageIdentityAsync();
            Assert.IsFalse(sourceIdentity.IsError, sourceIdentity.Message);
            var actor = Guid.NewGuid();
            var grant = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
            var deliveryRoute = new HolonReplicationRouteBinding(sourceIdentity.Result, binding.Result, grant.BindingIdentity);
            var enrollment = new HolonReplicationEnrollment(deliveryRoute.SourceId, new[] { deliveryRoute.TargetId },
                sourceIdentity.Result, new[] { deliveryRoute });
            var routedHolon = new Holon { Id = Guid.NewGuid(), Name = "SQLite-to-Supabase delivery", IsActive = true };
            attempted.Add(routedHolon.Id);
            var routedIntent = new ConditionalHolonWriteRequest(
                new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
                    new[] { new HolonWriteCondition(routedHolon.Id, null) }),
                ConditionalHolonOperation.Save, new[] { routedHolon }, saveChildren: false);
            var sourceWrite = await sqlite.ExecuteReplicatingHolonWriteAsync(routedIntent, enrollment);
            Assert.IsTrue(!sourceWrite.IsError && sourceWrite.IsSaved, sourceWrite.Message);
            var outbox = sqlite.GetBoundHolonReplicationOutbox(sourceIdentity.Result);
            Assert.IsFalse(outbox.IsError, outbox.Message);
            var target = new CommittedHolonReplicaTarget(deliveryRoute.TargetId, supabase, binding.Result.Authority,
                deliveryRoute.SourceId, sourceIdentity.Result.Authority, grant, "supabase-fixture", Guid.NewGuid(),
                grant.BindingIdentity, binding.Result);
            var dispatcher = new HolonOutboxDispatcher(outbox.Result, new[] { target }, "supabase-fixture");
            var delivered = await dispatcher.DispatchOnceAsync(deliveryRoute.TargetId, maximumCount: 1);
            Assert.IsFalse(delivered.IsError, delivered.Message);
            Assert.AreEqual(1, delivered.Result.CompletedCount);
            Assert.AreEqual(sourceWrite.Result.Single().VersionId,
                (await supabase.LoadHolonAsync(routedHolon.Id, loadChildren: false)).Result?.VersionId);
            var restarted = new HolonOutboxDispatcher(outbox.Result, new[] { target }, "supabase-fixture-restarted");
            var duplicate = await restarted.DispatchOnceAsync(deliveryRoute.TargetId, maximumCount: 1);
            Assert.IsFalse(duplicate.IsError, duplicate.Message);
            Assert.AreEqual(0, duplicate.Result.CompletedCount);

            var reverseRoute = new HolonReplicationRouteBinding(binding.Result, sourceIdentity.Result, grant.BindingIdentity);
            var reverseEnrollment = new HolonReplicationEnrollment(reverseRoute.SourceId, new[] { reverseRoute.TargetId },
                binding.Result, new[] { reverseRoute });
            var reverseHolon = new Holon { Id = Guid.NewGuid(), Name = "Supabase-to-SQLite delivery", IsActive = true };
            attempted.Add(reverseHolon.Id);
            var reverseIntent = new ConditionalHolonWriteRequest(
                new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SupabaseOASIS,
                    new[] { new HolonWriteCondition(reverseHolon.Id, null) }),
                ConditionalHolonOperation.Save, new[] { reverseHolon }, saveChildren: false);
            var reverseSave = sync ? supabase.ExecuteReplicatingHolonWrite(reverseIntent, reverseEnrollment)
                : await supabase.ExecuteReplicatingHolonWriteAsync(reverseIntent, reverseEnrollment);
            Assert.IsTrue(!reverseSave.IsError && reverseSave.IsSaved, $"{reverseSave.Message} {reverseSave.Exception}");
            var sourceOutbox = supabase.GetBoundHolonReplicationOutbox(binding.Result);
            Assert.IsFalse(sourceOutbox.IsError, sourceOutbox.Message);
            var heads = await sourceOutbox.Result.ReadRouteHeadsAsync(pageSize: 1);
            Assert.IsFalse(heads.IsError, heads.Message);
            var head = heads.Result.Heads.Single();
            Assert.AreEqual(reverseRoute.TargetId, head.TargetId);
            var sqliteTarget = new CommittedHolonReplicaTarget(reverseRoute.TargetId, sqlite, sourceIdentity.Result.Authority,
                reverseRoute.SourceId, binding.Result.Authority, grant, "sqlite-fixture", Guid.NewGuid(),
                grant.BindingIdentity, sourceIdentity.Result);
            var reverseDispatcher = new HolonOutboxDispatcher(sourceOutbox.Result, new[] { sqliteTarget }, "supabase-source");
            var originalClaim = await sourceOutbox.Result.ClaimNextAsync(reverseRoute.TargetId,
                "supabase-original-worker", TimeSpan.FromSeconds(3));
            Assert.IsFalse(originalClaim.IsError, originalClaim.Message);
            Assert.IsNotNull(originalClaim.Result);
            var renewedClaim = await sourceOutbox.Result.RenewAsync(originalClaim.Result, TimeSpan.FromSeconds(3));
            Assert.IsTrue(!renewedClaim.IsError && renewedClaim.Result, renewedClaim.Message);
            var heldClaim = await sourceOutbox.Result.PeekNextAsync(reverseRoute.TargetId);
            Assert.IsFalse(heldClaim.IsError, heldClaim.Message);
            Assert.AreEqual(HolonDeliveryState.InFlight, heldClaim.Result.State);
            var competingClaim = await sourceOutbox.Result.ClaimNextAsync(reverseRoute.TargetId,
                "supabase-competing-worker", TimeSpan.FromSeconds(3));
            Assert.IsFalse(competingClaim.IsError, competingClaim.Message);
            Assert.IsNull(competingClaim.Result);

            // Wait past the renewed lease using elapsed time independent of the runner's wall clock.
            await Task.Delay(TimeSpan.FromMilliseconds(3100));
            var expiredFinish = await sourceOutbox.Result.FinishAsync(originalClaim.Result, HolonDeliveryOutcome.Completed);
            Assert.IsFalse(expiredFinish.IsError, expiredFinish.Message);
            Assert.IsFalse(expiredFinish.Result);
            var reverseDelivery = await reverseDispatcher.DispatchOnceAsync(reverseRoute.TargetId, maximumCount: 2);
            Assert.IsFalse(reverseDelivery.IsError, $"{reverseDelivery.Message} {reverseDelivery.Exception}");
            Assert.AreEqual(1, reverseDelivery.Result.CompletedCount);
            var reverseReplica = await sqlite.LoadHolonAsync(reverseHolon.Id, loadChildren: false);
            Assert.IsFalse(reverseReplica.IsError, reverseReplica.Message);
            Assert.AreEqual(reverseSave.Result.Single().VersionId, reverseReplica.Result?.VersionId);
            var finished = await sourceOutbox.Result.InspectAsync(head.Sequence);
            Assert.IsFalse(finished.IsError, finished.Message);
            Assert.AreEqual(HolonDeliveryState.Completed, finished.Result.State);
            Assert.AreEqual(2, finished.Result.AttemptCount);
            var expiredRenewal = await sourceOutbox.Result.RenewAsync(originalClaim.Result, TimeSpan.FromSeconds(3));
            Assert.IsFalse(expiredRenewal.IsError, expiredRenewal.Message);
            Assert.IsFalse(expiredRenewal.Result);

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
