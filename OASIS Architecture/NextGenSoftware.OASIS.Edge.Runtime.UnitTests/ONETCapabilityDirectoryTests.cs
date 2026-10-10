using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ONET;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.Runtime.UnitTests;

public sealed class ONETCapabilityDirectoryTests
{
    [Fact]
    public async Task PublishedSignedLeasesAreDiscoverableAndSelectedDeterministically()
    {
        DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
        var registryChannel = new MemoryChannel("registry");
        var edgeChannel = new MemoryChannel("edge-client");
        registryChannel.Peer = edgeChannel;
        edgeChannel.Peer = registryChannel;
        using var registryEndpoint = new ONETRequestResponseEndpoint(registryChannel);
        using var edgeEndpoint = new ONETRequestResponseEndpoint(edgeChannel);
        var registry = new ONETCapabilityRegistry(registryEndpoint, () => now);
        var first = await CreateAdvertisementAsync(ONETNodeProfile.Edge, now.AddSeconds(-2), "SQLiteOASIS", "read");
        var newest = await CreateAdvertisementAsync(ONETNodeProfile.Full, now.AddSeconds(-1), "MongoDBOASIS", "sync-host");
        registry.RegisterLocal(first, first.NodeId).IsError.Should().BeFalse();
        registry.RegisterLocal(newest, newest.NodeId).IsError.Should().BeFalse();
        var directory = new ONETCapabilityDirectoryClient(edgeEndpoint, "registry", () => now);

        var selected = await directory.SelectNodeAsync(new ONETCapabilityQuery
        {
            Service = "sync-v3", ProviderType = "MongoDBOASIS", Capability = "sync-host"
        }, default);

        selected.IsError.Should().BeFalse(selected.Message);
        selected.Result.NodeId.Should().Be(newest.NodeId);
        selected.Result.NodeProfile.Should().Be(ONETNodeProfile.Full);
    }

    [Fact]
    public async Task DirectoryRejectsUnsignedOrTamperedRegistryResults()
    {
        DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
        var registryChannel = new MemoryChannel("registry");
        var edgeChannel = new MemoryChannel("edge-client");
        registryChannel.Peer = edgeChannel;
        edgeChannel.Peer = registryChannel;
        using var registryEndpoint = new ONETRequestResponseEndpoint(registryChannel);
        using var edgeEndpoint = new ONETRequestResponseEndpoint(edgeChannel);
        var advertisement = await CreateAdvertisementAsync(ONETNodeProfile.Full, now, "MongoDBOASIS", "sync-host");
        advertisement.Services = new[] { "forged" };
        registryEndpoint.RegisterHandler(ONETCapabilityDirectoryClient.OperationName, (_, _) =>
            Task.FromResult(new OASISResult<string>
            {
                Result = System.Text.Json.JsonSerializer.Serialize(new[] { advertisement })
            }));
        var directory = new ONETCapabilityDirectoryClient(edgeEndpoint, "registry", () => now);

        var result = await directory.QueryAsync(new ONETCapabilityQuery(), default);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("ONET_CAPABILITY_DIRECTORY_UNVERIFIED");
    }

    [Fact]
    public async Task FederatedDirectoryReconcilesNewestLeaseAndReportsSatisfiedPartialQuorum()
    {
        DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var older = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, now.AddSeconds(-2), "MongoDBOASIS", "sync-host");
        var newer = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, now.AddSeconds(-1), "MongoDBOASIS", "sync-host");
        var directory = new ONETFederatedCapabilityDirectory(new IONETCapabilityDirectory[]
        {
            new StubDirectory(new[] { older }), new StubDirectory(new[] { newer }),
            new StubDirectory("REGISTRY_UNAVAILABLE", "Registry three is unreachable.")
        }, 2);

        var result = await directory.QueryAsync(new ONETCapabilityQuery { MaximumResults = 10 }, default);

        result.IsError.Should().BeFalse(result.Message);
        result.IsWarning.Should().BeTrue();
        result.WarningCount.Should().Be(1);
        result.Result.Should().ContainSingle().Which.NodeId.Should().Be(newer.NodeId);
        result.Result.Single().IssuedUtc.Should().Be(newer.IssuedUtc);
    }

    [Fact]
    public async Task FederatedDirectoryFailsWhenRegistryQuorumIsNotMet()
    {
        var directory = new ONETFederatedCapabilityDirectory(new IONETCapabilityDirectory[]
        {
            new StubDirectory("OFFLINE", "one"), new StubDirectory("OFFLINE", "two"),
            new StubDirectory(Array.Empty<ONETCapabilityAdvertisement>())
        }, 2);

        var result = await directory.QueryAsync(new ONETCapabilityQuery(), default);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("ONET_CAPABILITY_REGISTRY_QUORUM_FAILED");
    }

    [Fact]
    public async Task FederatedPublisherRequiresConfiguredAcknowledgementQuorum()
    {
        var advertisement = await CreateAdvertisementAsync(ONETNodeProfile.Edge, DateTime.UtcNow,
            "SQLiteOASIS", "write");
        var accepted = new StubPublisher(new OASISResult<bool>(true));
        var rejected = new StubPublisher(new OASISResult<bool>
            { IsError = true, ErrorCount = 1, ErrorCode = "OFFLINE", Message = "registry unavailable" });

        var satisfied = await new ONETFederatedCapabilityPublisher(
            new IONETCapabilityPublisher[] { accepted, rejected }, 1).PublishAsync(advertisement, default);
        var failed = await new ONETFederatedCapabilityPublisher(
            new IONETCapabilityPublisher[] { accepted, rejected }, 2).PublishAsync(advertisement, default);

        satisfied.IsError.Should().BeFalse();
        satisfied.IsWarning.Should().BeTrue();
        satisfied.WarningCount.Should().Be(1);
        failed.IsError.Should().BeTrue();
        failed.ErrorCode.Should().Be("ONET_CAPABILITY_PUBLISH_QUORUM_FAILED");
    }

    [Fact]
    public async Task RegistryReconciliationImportsPeerLeaseAndIgnoresOlderReplay()
    {
        DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var older = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, now.AddSeconds(-2), "MongoDBOASIS", "sync-host");
        var newer = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, now.AddSeconds(-1), "MongoDBOASIS", "sync-host");
        var localChannel = new MemoryChannel("local-registry");
        using var localEndpoint = new ONETRequestResponseEndpoint(localChannel);
        var local = new ONETCapabilityRegistry(localEndpoint, () => now);
        local.MergeSignedLease(newer).IsError.Should().BeFalse();
        var reconciler = new ONETCapabilityRegistryReconciler(local,
            new[] { new StubDirectory(new[] { older }) }, 1);

        var result = await reconciler.ReconcileAsync(default);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.StaleLeasesIgnored.Should().Be(1);
        local.TryGetCurrent(newer.NodeId, out var retained).Should().BeTrue();
        retained.IssuedUtc.Should().Be(newer.IssuedUtc);
    }

    [Fact]
    public async Task RegistryReconciliationConvertsPeerExceptionIntoQuorumError()
    {
        var channel = new MemoryChannel("local-registry");
        using var endpoint = new ONETRequestResponseEndpoint(channel);
        var registry = new ONETCapabilityRegistry(endpoint);
        var reconciler = new ONETCapabilityRegistryReconciler(registry,
            new[] { new ThrowingDirectory() }, 1);

        var result = await reconciler.ReconcileAsync(default);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("ONET_CAPABILITY_GOSSIP_QUORUM_FAILED");
    }

    [Fact]
    public async Task RegistryReconciliationSurvivesRepeatedPartitionsAndRejectsEquivocationAfterRecovery()
    {
        DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var first = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, now.AddMinutes(-3),
            "MongoDBOASIS", "sync-host");
        var recovered = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, now.AddMinutes(-2),
            "MongoDBOASIS", "sync-host");
        var equivocation = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, recovered.IssuedUtc,
            "MongoDBOASIS", "forged-capability");
        var peerOne = new MutableDirectory(new[] { first });
        var peerTwo = new MutableDirectory(new[] { first });
        var peerThree = new MutableDirectory("OFFLINE", "partitioned");
        var localChannel = new MemoryChannel("local-registry");
        using var localEndpoint = new ONETRequestResponseEndpoint(localChannel);
        var local = new ONETCapabilityRegistry(localEndpoint, () => now);
        var reconciler = new ONETCapabilityRegistryReconciler(local,
            new IONETCapabilityDirectory[] { peerOne, peerTwo, peerThree }, 2);

        for (int cycle = 0; cycle < 4; cycle++)
        {
            peerOne.SetLeases(cycle < 2 ? new[] { first } : new[] { recovered });
            peerTwo.SetLeases(cycle < 2 ? new[] { first } : new[] { recovered });
            peerThree.SetFailure("OFFLINE", $"partition cycle {cycle}");

            var reconciled = await reconciler.ReconcileAsync(default);

            reconciled.IsError.Should().BeFalse(reconciled.Message);
            reconciled.IsWarning.Should().BeTrue();
            reconciled.WarningCount.Should().Be(1);
        }

        local.TryGetCurrent(recovered.NodeId, out var retained).Should().BeTrue();
        retained.IssuedUtc.Should().Be(recovered.IssuedUtc);
        retained.Providers.Single().Capabilities.Should().Contain("sync-host");

        peerThree.SetLeases(new[] { equivocation });
        var conflict = await reconciler.ReconcileAsync(default);

        conflict.IsError.Should().BeTrue();
        conflict.ErrorCode.Should().Be("ONET_CAPABILITY_EQUIVOCATION");
        local.TryGetCurrent(recovered.NodeId, out retained).Should().BeTrue();
        retained.Signature.Should().Be(recovered.Signature);
        retained.Providers.Single().Capabilities.Should().Contain("sync-host");
    }

    [Fact]
    public async Task RegistryReconciliationSurvivesDeterministicFiveHundredCyclePartitionSoak()
    {
        DateTime now = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var current = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, now,
            "MongoDBOASIS", "sync-host");
        var peers = Enumerable.Range(0, 5).Select(_ => new MutableDirectory(new[] { current })).ToArray();
        var localChannel = new MemoryChannel("soak-local-registry");
        using var localEndpoint = new ONETRequestResponseEndpoint(localChannel);
        var local = new ONETCapabilityRegistry(localEndpoint, () => now);
        var reconciler = new ONETCapabilityRegistryReconciler(local, peers, 3);
        int expectedQuorumFailures = 0;

        for (int cycle = 1; cycle <= 500; cycle++)
        {
            now = now.AddSeconds(1);
            if (cycle % 20 == 0)
                current = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, now,
                    "MongoDBOASIS", "sync-host");

            for (int peer = 0; peer < peers.Length; peer++)
            {
                // Rotate two unavailable registries so every peer repeatedly partitions and recovers.
                bool unavailable = peer == cycle % peers.Length || peer == (cycle + 2) % peers.Length;
                if (cycle % 73 == 0 && peer == (cycle + 4) % peers.Length)
                    unavailable = true; // deterministic loss of quorum
                if (unavailable) peers[peer].SetFailure("OFFLINE", $"cycle {cycle}, peer {peer}");
                else peers[peer].SetLeases(new[] { current });
            }

            var result = await reconciler.ReconcileAsync(default);
            if (cycle % 73 == 0)
            {
                expectedQuorumFailures++;
                result.IsError.Should().BeTrue();
                result.ErrorCode.Should().Be("ONET_CAPABILITY_GOSSIP_QUORUM_FAILED");
            }
            else
            {
                result.IsError.Should().BeFalse($"cycle {cycle}: {result.Message}");
                result.IsWarning.Should().BeTrue();
                result.WarningCount.Should().Be(2);
            }

            local.TryGetCurrent(current.NodeId, out var retained).Should().BeTrue($"cycle {cycle}");
            retained.Signature.Should().Be(current.Signature, $"cycle {cycle} must retain the newest valid lease");
        }

        expectedQuorumFailures.Should().Be(6);

        var equivocation = await CreateAdvertisementAsync(key, ONETNodeProfile.Full, current.IssuedUtc,
            "MongoDBOASIS", "forged-capability");
        peers[0].SetLeases(new[] { current });
        peers[1].SetLeases(new[] { current });
        peers[2].SetLeases(new[] { current });
        peers[3].SetLeases(new[] { equivocation });
        peers[4].SetFailure("OFFLINE", "post-soak partition");

        var conflict = await reconciler.ReconcileAsync(default);

        conflict.IsError.Should().BeTrue();
        conflict.ErrorCode.Should().Be("ONET_CAPABILITY_EQUIVOCATION");
        local.TryGetCurrent(current.NodeId, out var final).Should().BeTrue();
        final.Signature.Should().Be(current.Signature);
        final.Providers.Single().Capabilities.Should().Contain("sync-host");
    }

    private static async Task<ONETCapabilityAdvertisement> CreateAdvertisementAsync(ONETNodeProfile profile,
        DateTime issued, string providerType, string capability)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return await CreateAdvertisementAsync(key, profile, issued, providerType, capability);
    }

    private static async Task<ONETCapabilityAdvertisement> CreateAdvertisementAsync(ECDsa key,
        ONETNodeProfile profile, DateTime issued, string providerType, string capability)
    {
        byte[] publicKey = key.ExportSubjectPublicKeyInfo();
        string nodeId;
        using (var sha = SHA256.Create()) nodeId = Convert.ToHexString(sha.ComputeHash(publicKey)).ToLowerInvariant();
        var created = await ONETCapabilityProof.CreateAsync(nodeId, Convert.ToBase64String(publicKey), profile,
            new[] { "sync-v3" }, new[]
            {
                new ONETProviderCapability
                {
                    ProviderType = providerType, ProviderCategory = "Storage", Capabilities = new[] { capability }
                }
            }, issued, TimeSpan.FromMinutes(10), (message, _) => Task.FromResult(new OASISResult<string>
            {
                Result = Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256))
            }), default);
        created.IsError.Should().BeFalse(created.Message);
        return created.Result;
    }

    private sealed class MemoryChannel : IONETApplicationMessageChannel
    {
        public MemoryChannel(string localNodeId) => LocalNodeId = localNodeId;
        public string LocalNodeId { get; }
        public MemoryChannel? Peer { get; set; }
        public event EventHandler<ONETApplicationMessage>? MessageReceived;
        public Task<OASISResult<bool>> SendAsync(ONETApplicationMessage message, CancellationToken cancellationToken)
        {
            Peer?.Receive(message);
            return Task.FromResult(new OASISResult<bool> { Result = Peer != null, IsError = Peer == null,
                ErrorCount = Peer == null ? 1 : 0, Message = Peer == null ? "No peer is connected." : string.Empty });
        }
        private void Receive(ONETApplicationMessage message) => MessageReceived?.Invoke(this, message);
    }

    private sealed class StubDirectory : IONETCapabilityDirectory
    {
        private readonly OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>> _result;
        public StubDirectory(IReadOnlyList<ONETCapabilityAdvertisement> advertisements) =>
            _result = new OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>(advertisements);
        public StubDirectory(string code, string message) => _result =
            new OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>
            { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
        public Task<OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>> QueryAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken) => Task.FromResult(_result);
        public async Task<OASISResult<ONETCapabilityAdvertisement>> SelectNodeAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken)
        {
            var queried = await QueryAsync(query, cancellationToken);
            return queried.IsError || queried.Result.Count == 0
                ? new OASISResult<ONETCapabilityAdvertisement> { IsError = true, ErrorCount = 1,
                    ErrorCode = queried.ErrorCode ?? "EMPTY", Message = queried.Message }
                : new OASISResult<ONETCapabilityAdvertisement>(queried.Result[0]);
        }
    }

    private sealed class StubPublisher : IONETCapabilityPublisher
    {
        private readonly OASISResult<bool> _result;
        public StubPublisher(OASISResult<bool> result) => _result = result;
        public Task<OASISResult<bool>> PublishAsync(ONETCapabilityAdvertisement advertisement,
            CancellationToken cancellationToken) => Task.FromResult(_result);
    }

    private sealed class MutableDirectory : IONETCapabilityDirectory
    {
        private OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>> _result;

        public MutableDirectory(IReadOnlyList<ONETCapabilityAdvertisement> advertisements) =>
            SetLeases(advertisements);

        public MutableDirectory(string code, string message) => SetFailure(code, message);

        public void SetLeases(IReadOnlyList<ONETCapabilityAdvertisement> advertisements) =>
            _result = new OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>(advertisements);

        public void SetFailure(string code, string message) => _result =
            new OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>
            { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };

        public Task<OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>> QueryAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken) => Task.FromResult(_result);

        public async Task<OASISResult<ONETCapabilityAdvertisement>> SelectNodeAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken)
        {
            var queried = await QueryAsync(query, cancellationToken);
            return queried.IsError || queried.Result.Count == 0
                ? new OASISResult<ONETCapabilityAdvertisement> { IsError = true, ErrorCount = 1,
                    ErrorCode = queried.ErrorCode ?? "EMPTY", Message = queried.Message }
                : new OASISResult<ONETCapabilityAdvertisement>(queried.Result[0]);
        }
    }

    private sealed class ThrowingDirectory : IONETCapabilityDirectory
    {
        public Task<OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>> QueryAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("transport failed");
        public Task<OASISResult<ONETCapabilityAdvertisement>> SelectNodeAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("transport failed");
    }
}
