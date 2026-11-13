using FluentAssertions;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    // Protocol fixtures carry coherent committed lineage; these tests exercise
    // the actual receiver transaction, not authoritative-source certification.
    private static CommittedHolonReplicaRequest BoundReplicaFixture()
    {
        var row = new Holon { Id = Guid.NewGuid(), Name = "bound replica", IsActive = true,
            Version = 1, VersionId = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(row, ProviderType.MongoDBOASIS, Guid.NewGuid());
        return new(Guid.NewGuid(), "bound-native-fixture", ProviderType.MongoDBOASIS,
            row.CreatedByAvatarId, ConditionalHolonOperation.Save, new[] { row }, saveChildren: false);
    }

    private long BoundCount(string table)
    {
        // Table names are fixed by tests, never caller input.
        using var connection = new SqliteConnection(TestConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM " + table;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private void BoundSql(string sql)
    {
        using var connection = new SqliteConnection(TestConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BoundReplicaChecksNativeIdentityBeforeWriteAndReceiptReplay(bool synchronous)
    {
        var journal = Path.Combine(Path.GetDirectoryName(_databasePath)!, "scoped-visibility.pending.json");
        await File.WriteAllTextAsync(journal, "{\"test\":\"bound-receiver\"}");
        using (var receiver = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await receiver.ActivateProviderAsync()).IsError.Should().BeFalse();
            var captured = await receiver.CaptureStorageIdentityAsync();
            captured.IsError.Should().BeFalse(captured.Message);
            var expected = captured.Result;
            var request = BoundReplicaFixture();
            async Task<OASISResult<IEnumerable<IHolon>>> Apply(CommittedHolonReplicaRequest mutation, CosmicStorageIdentity identity)
                => synchronous ? receiver.ApplyBoundCommittedHolonReplica(mutation, identity)
                    : await receiver.ApplyBoundCommittedHolonReplicaAsync(mutation, identity);

            // Same logical store UUID at a different native address is not this receiver.
            var wrongEndpoint = new CosmicStorageIdentity(expected.Authority,
                expected.EndpointFingerprint == new string('B', 64) ? new string('C', 64) : new string('B', 64), expected.StoreId);
            (await Apply(request, wrongEndpoint)).ErrorCode.Should().Be("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH");
            BoundCount("Holon").Should().Be(0);
            BoundCount("CosmicWriteReceipt").Should().Be(0);
            BoundCount("CosmicReplicaHead").Should().Be(0);

            var first = await Apply(request, expected);
            first.IsError.Should().BeFalse(first.Message + first.Exception);
            var replay = await Apply(request, expected);
            replay.IsError.Should().BeFalse(replay.Message + replay.Exception);
            CosmicConcurrency.GetProviderRevisionId(replay.Result.Single(), expected.Authority).Should()
                .Be(CosmicConcurrency.GetProviderRevisionId(first.Result.Single(), expected.Authority));
            BoundCount("CosmicWriteReceipt").Should().Be(1);

            // Simulate an administrator replacing native store identity after enrollment.
            // An old receipt must not make the wrong receiver acknowledge success.
            BoundSql("UPDATE CosmicReplicationStoreIdentity SET Identity='" + Guid.NewGuid().ToString("D") + "'");
            (await Apply(request, expected)).ErrorCode.Should().Be("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH");
            (await Apply(BoundReplicaFixture(), expected)).ErrorCode.Should().Be("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH");
            BoundCount("Holon").Should().Be(1);
            BoundCount("CosmicWriteReceipt").Should().Be(1);
            BoundCount("CosmicReplicaHead").Should().Be(1);
        }
        File.Delete(journal); // Successful fixture only; failures retain intent/database.
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("corrupt")]
    public async Task BoundReplicaNeverInitializesOrRepairsReceiverIdentity(string damage)
    {
        var journal = Path.Combine(Path.GetDirectoryName(_databasePath)!, "scoped-visibility.pending.json");
        await File.WriteAllTextAsync(journal, "{\"test\":\"bound-metadata-admission\"}");
        using (var receiver = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await receiver.ActivateProviderAsync()).IsError.Should().BeFalse();
            var expected = await receiver.CaptureStorageIdentityAsync();
            expected.IsError.Should().BeFalse();
            BoundSql(damage == "missing" ? "DROP TABLE CosmicReplicationStoreIdentity"
                : damage == "empty" ? "DELETE FROM CosmicReplicationStoreIdentity"
                : "UPDATE CosmicReplicationStoreIdentity SET Identity='corrupt'");
            var rejected = await receiver.ApplyBoundCommittedHolonReplicaAsync(BoundReplicaFixture(), expected.Result);
            rejected.ErrorCode.Should().Be("COSMIC_REPLICA_STORAGE_BINDING_INVALID");
            BoundCount("Holon").Should().Be(0);
            BoundCount("CosmicWriteReceipt").Should().Be(0);
            BoundCount("CosmicReplicaHead").Should().Be(0);
            using var observer = new SqliteConnection(TestConnectionString);
            await observer.OpenAsync();
            using var command = observer.CreateCommand();
            command.CommandText = damage == "missing"
                ? "SELECT COUNT(*) FROM sqlite_master WHERE name='CosmicReplicationStoreIdentity'"
                : damage == "empty" ? "SELECT COUNT(*) FROM CosmicReplicationStoreIdentity"
                : "SELECT COUNT(*) FROM CosmicReplicationStoreIdentity WHERE Identity='corrupt'";
            Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(damage == "corrupt" ? 1 : 0);
        }
        File.Delete(journal);
    }

    [Fact]
    public async Task BoundReplicaHoldsNativeWriterReservationFromIdentityReadThroughCommit()
    {
        var journal = Path.Combine(Path.GetDirectoryName(_databasePath)!, "scoped-visibility.pending.json");
        await File.WriteAllTextAsync(journal, "{\"test\":\"bound-native-lock\"}");
        using (var receiver = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await receiver.ActivateProviderAsync()).IsError.Should().BeFalse();
            var expected = await receiver.CaptureStorageIdentityAsync();
            expected.IsError.Should().BeFalse();
            var observed = false;
            var replacement = Guid.NewGuid().ToString("D");
            using var context = new ReplicaProbeContext(TestConnectionString, new MutateCallerOnRead(() =>
            {
                // EF reads the existing graph after native identity admission.
                // A second connection must be unable to replace that identity
                // until the first connection commits its graph and receipt.
                using var competitor = new SqliteConnection(TestConnectionString);
                competitor.Open();
                using var command = competitor.CreateCommand();
                command.CommandTimeout = 1;
                command.CommandText = "UPDATE CosmicReplicationStoreIdentity SET Identity=$replacement";
                command.Parameters.AddWithValue("$replacement", replacement);
                var blocked = Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
                blocked.SqliteErrorCode.Should().BeOneOf(5, 6);
                observed = true;
            }));
            var repository = new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories.HolonRepository(context);
            var request = BoundReplicaFixture();
            var applied = repository.ApplyBoundCommittedHolonReplica(request, expected.Result, CancellationToken.None);
            applied.IsError.Should().BeFalse(applied.Message + applied.Exception);
            observed.Should().BeTrue("the competing writer must run inside the graph transaction");
            BoundCount("Holon").Should().Be(1);
            BoundCount("CosmicWriteReceipt").Should().Be(1);
            BoundSql("UPDATE CosmicReplicationStoreIdentity SET Identity='" + replacement + "'");
            (await receiver.ApplyBoundCommittedHolonReplicaAsync(request, expected.Result)).ErrorCode.Should()
                .Be("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH");
        }
        File.Delete(journal);
    }

    private sealed class BeforeBoundApply(Action action) : IHolonReplicationAuthorizationPolicy
    {
        public string BindingIdentity => "bound-receiver-native-test-v1";
        public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicationAuthorizationContext context, CancellationToken token)
        { token.ThrowIfCancellationRequested(); action(); return Task.FromResult(new OASISResult<bool>(true)); }
    }

    [Fact]
    public async Task BoundCatalogRejectsStoreReplacementAfterAuthorizationBeforeNativeApply()
    {
        var journal = Path.Combine(Path.GetDirectoryName(_databasePath)!, "scoped-visibility.pending.json");
        await File.WriteAllTextAsync(journal, "{\"test\":\"catalog-bound-receiver\"}");
        using (var receiver = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await receiver.ActivateProviderAsync()).IsError.Should().BeFalse();
            var captured = await receiver.CaptureStorageIdentityAsync();
            captured.IsError.Should().BeFalse();
            var policy = new BeforeBoundApply(() => BoundSql(
                "UPDATE CosmicReplicationStoreIdentity SET Identity='" + Guid.NewGuid().ToString("D") + "'"));
            var request = BoundReplicaFixture();
            var catalog = new HolonReplicationCatalog("bound-native", Guid.NewGuid(), request.SourceId, "fixture-source",
                request.SourceAuthority, receiver.GetHolonReplicationOutbox().Result,
                new[] { new HolonReplicationDestination("sqlite", captured.Result, receiver, policy) });
            var before = CommittedHolonReplicaTarget.CaptureItem(request);
            var frozen = before.PayloadJson;
            var result = await catalog.Targets.Single().ApplyReplicatedMutationAsync(before, CancellationToken.None);
            result.ErrorCode.Should().Be("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH");
            before.PayloadJson.Should().Be(frozen);
            BoundCount("Holon").Should().Be(0);
            BoundCount("CosmicWriteReceipt").Should().Be(0);
            BoundCount("CosmicReplicaHead").Should().Be(0);
        }
        File.Delete(journal);
    }
}
