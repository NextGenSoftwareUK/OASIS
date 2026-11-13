using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.IntegrationTests;

public sealed class IPFSRestartTests
{
    private static readonly Guid RecordId = new("90644865-2be6-4044-bdfb-3e6a394f5b22");

    [Fact]
    public async Task PersistsCurrentAndHistoricalHolonAcrossDaemonRestart()
    {
        var endpoint = Environment.GetEnvironmentVariable("IPFSOASIS_TEST_API");
        var dnaPath = Environment.GetEnvironmentVariable("IPFSOASIS_RESTART_DNA");
        var phase = Environment.GetEnvironmentVariable("IPFSOASIS_RESTART_PHASE");
        Assert.False(string.IsNullOrWhiteSpace(endpoint));
        Assert.False(string.IsNullOrWhiteSpace(dnaPath));
        Assert.True(phase is "write" or "read", "Select the disposable Kubo restart fixture phase.");

        var identityPath = dnaPath + ".identity";
        OASISDNA dna;
        if (phase == "write")
        {
            Assert.False(File.Exists(dnaPath));
            dna = new OASISDNA();
            dna.OASIS.StorageProviders = new StorageProviderSettings
            {
                IPFSOASIS = new IPFSOASISSettings { ConnectionString = endpoint }
            };
        }
        else
        {
            Assert.True(File.Exists(dnaPath));
            var loaded = OASISDNAManager.LoadDNA(dnaPath);
            Assert.False(loaded.IsError, loaded.Message);
            dna = Assert.IsType<OASISDNA>(loaded.Result);
            Assert.Equal(endpoint, dna.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
            Assert.False(string.IsNullOrWhiteSpace(dna.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress));
        }

        var provider = new IPFSOASIS(dna, dnaPath);
        try
        {
            var active = await provider.ActivateProviderAsync();
            Assert.True(active.Result && !active.IsError, active.Message);
            if (phase == "write")
            {
                var unbound = await provider.CaptureStorageIdentityAsync();
                Assert.True(unbound.IsError);
                Assert.Null(unbound.Result);
                Assert.False(File.Exists(dnaPath));

                var holon = new Holon { Id = RecordId, Name = "before update", Version = 1, HolonType = HolonType.Holon };
                var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
                var identity = await provider.CaptureStorageIdentityAsync();
                Assert.False(identity.IsError, identity.Message);
                await File.WriteAllTextAsync(identityPath, identity.Result.BindingFingerprint);
                await VerifyReplicaAsync(provider, identity.Result, dnaPath, replay: false);
                holon.Name = "after update";
                holon.Version = 2;
                saved = await provider.SaveHolonAsync(holon, saveChildren: false);
                Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
                Assert.True(File.Exists(dnaPath));
            }
            else
            {
                var identity = await provider.CaptureStorageIdentityAsync();
                Assert.False(identity.IsError, identity.Message);
                Assert.Equal(await File.ReadAllTextAsync(identityPath), identity.Result.BindingFingerprint);
                await VerifyReplicaAsync(provider, identity.Result, dnaPath, replay: true);
                var current = await provider.LoadHolonAsync(RecordId, loadChildren: false);
                Assert.True(current.IsLoaded && !current.IsError, current.Message);
                Assert.Equal("after update", current.Result.Name);
                Assert.Equal(2, current.Result.Version);
                var history = await provider.LoadHolonAsync(RecordId, loadChildren: false, version: 1);
                Assert.True(history.IsLoaded && !history.IsError, history.Message);
                Assert.Equal("before update", history.Result.Name);
                Assert.Equal(1, history.Result.Version);
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    private static async Task VerifyReplicaAsync(IPFSOASIS provider, CosmicStorageIdentity identity, string dnaPath, bool replay)
    {
        var replica = new Holon
        {
            Id = new Guid("90644865-2be6-4044-bdfb-3e6a394f5b23"),
            VersionId = new Guid("90644865-2be6-4044-bdfb-3e6a394f5b24"),
            Name = "replicated from SQLite",
            Version = 1,
            HolonType = HolonType.Holon
        };
        CosmicConcurrency.SetProviderRevisionId(replica, ProviderType.SQLLiteDBOASIS,
            new Guid("90644865-2be6-4044-bdfb-3e6a394f5b25"));
        var request = new CommittedHolonReplicaRequest(
            new Guid("90644865-2be6-4044-bdfb-3e6a394f5b26"), "restart-sqlite-source",
            ProviderType.SQLLiteDBOASIS, RecordId, ConditionalHolonOperation.Save, new[] { replica }, saveChildren: false);
        var persistedBefore = await File.ReadAllTextAsync(dnaPath);
        var wrongStore = new CosmicStorageIdentity(identity.Authority, identity.EndpointFingerprint, Guid.NewGuid());
        var rejected = await provider.ApplyBoundCommittedHolonReplicaAsync(request, wrongStore);
        Assert.True(rejected.IsError);
        Assert.Equal("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", rejected.ErrorCode);
        Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));

        var observation = new CommittedHolonReplicaReceiptReadRequest(request, identity);
        var receipt = await provider.ReadCommittedHolonReplicaReceiptAsync(observation);
        Assert.True(receipt.IsLoaded && !receipt.IsError, receipt.Message);
        Assert.Equal(replay ? ConditionalHolonReceiptState.Committed : ConditionalHolonReceiptState.Absent,
            receipt.Result.ReceiptState);
        Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));

        var before = await provider.LoadHolonAsync(replica.Id, loadChildren: false);
        if (replay)
            Assert.True(before.IsLoaded && !before.IsError, before.Message);
        var applied = await provider.ApplyBoundCommittedHolonReplicaAsync(request, identity);
        Assert.True(applied.IsSaved && !applied.IsError, applied.Message);
        var committed = await provider.ReadCommittedHolonReplicaReceiptAsync(observation);
        Assert.True(committed.IsLoaded && !committed.IsError, committed.Message);
        Assert.Equal(ConditionalHolonReceiptState.Committed, committed.Result.ReceiptState);
        Assert.NotNull(committed.Result.AcknowledgementDigest);
        var acknowledgement = Assert.Single(applied.Result);
        var current = await provider.LoadHolonAsync(replica.Id, loadChildren: false);
        Assert.True(current.IsLoaded && !current.IsError, current.Message);
        Assert.Equal(replica.VersionId, current.Result.VersionId);
        var revision = CosmicConcurrency.GetProviderRevisionId(acknowledgement, ProviderType.IPFSOASIS);
        Assert.NotNull(revision);
        Assert.Equal(revision, CosmicConcurrency.GetProviderRevisionId(current.Result, ProviderType.IPFSOASIS));
        if (replay)
        {
            Assert.Equal(receipt.Result.AcknowledgementDigest, committed.Result.AcknowledgementDigest);
            Assert.Equal(revision, CosmicConcurrency.GetProviderRevisionId(before.Result, ProviderType.IPFSOASIS));
            Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));
            await VerifyReplicaSuccessorAsync(provider, identity, replica, dnaPath);
            await VerifyHostedReplicaTargetAsync(provider, identity);
        }
    }

    private static async Task VerifyReplicaSuccessorAsync(IPFSOASIS provider, CosmicStorageIdentity identity,
        Holon original, string dnaPath)
    {
        var successor = new Holon
        {
            Id = original.Id,
            Version = 2,
            VersionId = Guid.NewGuid(),
            PreviousVersionId = Guid.NewGuid(),
            Name = "updated replica",
            HolonType = HolonType.Holon
        };
        CosmicConcurrency.SetProviderRevisionId(successor, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        var operationId = Guid.NewGuid();
        CommittedHolonReplicaRequest Request() => new(operationId, "restart-sqlite-source",
            ProviderType.SQLLiteDBOASIS, RecordId, ConditionalHolonOperation.Save, new[] { successor }, saveChildren: false);

        var persistedBefore = await File.ReadAllTextAsync(dnaPath);
        var rejected = await provider.ApplyBoundCommittedHolonReplicaAsync(Request(), identity);
        Assert.True(rejected.IsError);
        Assert.Contains("lineage", rejected.Message);
        Assert.Equal(persistedBefore, await File.ReadAllTextAsync(dnaPath));

        successor.PreviousVersionId = original.VersionId;
        var accepted = await provider.ApplyBoundCommittedHolonReplicaAsync(Request(), identity);
        Assert.True(accepted.IsSaved && !accepted.IsError, accepted.Message);
        var current = await provider.LoadHolonAsync(original.Id, loadChildren: false);
        Assert.True(current.IsLoaded && !current.IsError, current.Message);
        Assert.Equal(successor.VersionId, current.Result.VersionId);
        var prior = await provider.LoadHolonAsync(original.Id, loadChildren: false, version: 1);
        Assert.True(prior.IsLoaded && !prior.IsError, prior.Message);
        Assert.Equal(original.VersionId, prior.Result.VersionId);
    }

    private static async Task VerifyHostedReplicaTargetAsync(IPFSOASIS provider, CosmicStorageIdentity identity)
    {
        var actor = Guid.NewGuid();
        var policy = new HolonReplicationActorGrant(new[] { actor }, DateTime.UtcNow.AddMinutes(10));
        var source = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid());
        var route = new HolonReplicationRouteBinding(source, identity, policy.BindingIdentity);
        var replica = new Holon
        {
            Id = Guid.NewGuid(),
            VersionId = Guid.NewGuid(),
            Version = 1,
            Name = "hosted replica target",
            HolonType = HolonType.Holon
        };
        CosmicConcurrency.SetProviderRevisionId(replica, source.Authority, Guid.NewGuid());
        var request = new CommittedHolonReplicaRequest(Guid.NewGuid(), route.SourceId, source.Authority, actor,
            ConditionalHolonOperation.Save, new[] { replica }, false, false, 0, new[] { route });
        var target = new CommittedHolonReplicaTarget(route.TargetId, provider, identity.Authority, route.SourceId,
            source.Authority, policy, "ipfs-restart-fixture", Guid.NewGuid(), policy.BindingIdentity, identity);
        var result = await target.ApplyReplicatedMutationAsync(CommittedHolonReplicaTarget.CaptureItem(request),
            CancellationToken.None);
        Assert.True(result.Result && !result.IsError, result.Message);
        var receipt = await provider.ReadCommittedHolonReplicaReceiptAsync(new(request, identity));
        Assert.True(receipt.IsLoaded && !receipt.IsError, receipt.Message);
        Assert.Equal(ConditionalHolonReceiptState.Committed, receipt.Result.ReceiptState);
    }
}
