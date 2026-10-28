using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task CopiedOutboxCannotReadClaimRenewOrAcknowledgeTheOriginalSourceLease()
    {
        var copy = Path.Combine(Path.GetDirectoryName(_databasePath)!, "outbox-source-binding-copy.db");
        File.Exists(copy).Should().BeFalse("a retained outcome must be reconciled before reuse");
        var journal = Path.Combine(Path.GetDirectoryName(_databasePath)!, "scoped-visibility.pending.json");
        using (var original = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await original.ActivateProviderAsync()).IsError.Should().BeFalse();
            var identity = await original.CaptureStorageIdentityAsync();
            identity.IsError.Should().BeFalse(identity.Message);
            var request = OutboxCreate(new Holon { Id = Guid.NewGuid(), Name = "source-owned outbox" });
            var enrollment = new HolonReplicationEnrollment("original-source", new[] { "target" }, identity.Result);
            await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new
            {
                test = nameof(CopiedOutboxCannotReadClaimRenewOrAcknowledgeTheOriginalSourceLease),
                copy, request.Write.OperationId, identity = identity.Result
            }));
            var saved = await original.ExecuteReplicatingHolonWriteAsync(request, enrollment);
            saved.IsError.Should().BeFalse(saved.Message + saved.Exception);

            var bound = new SqliteHolonReplicationOutbox(TestConnectionString, null, identity.Result);
            var claimed = await bound.ClaimNextAsync("target", "owner", TimeSpan.FromMinutes(5));
            claimed.IsError.Should().BeFalse(claimed.Message);
            claimed.Result.Should().NotBeNull();
            var claim = claimed.Result;
            (await bound.InspectAsync(claim.Sequence)).Result.State.Should().Be(HolonDeliveryState.InFlight);

            // Native backup copies the exact lease token, payload and store UUID.
            // The actual file endpoint still makes this a different source.
            using (var source = new SqliteConnection(TestConnectionString))
            using (var destination = new SqliteConnection($"Data Source={copy};Pooling=False"))
            {
                await source.OpenAsync();
                await destination.OpenAsync();
                source.BackupDatabase(destination);
            }
            var copied = new SqliteHolonReplicationOutbox($"Data Source={copy};Pooling=False", null, identity.Result);
            using (var observer = new SqliteConnection($"Data Source={copy};Pooling=False"))
            {
                await observer.OpenAsync();
                using var version = observer.CreateCommand();
                version.CommandText = "PRAGMA data_version";
                var before = Convert.ToInt64(await version.ExecuteScalarAsync());
                const string mismatch = "COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_MISMATCH";
                void Rejected<T>(OASISResult<T> result, string code = mismatch)
                {
                    result.IsError.Should().BeTrue();
                    result.ErrorCode.Should().Be(code);
                    result.Result.Should().Be(default(T));
                }
                Rejected(await copied.InspectAsync(claim.Sequence));
                Rejected(await copied.PeekNextAsync("target"));
                Rejected(await copied.ReadRouteHeadsAsync());
                Rejected(await copied.ClaimNextAsync("target", "intruder", TimeSpan.FromMinutes(1)));
                Rejected(await copied.RenewAsync(claim, TimeSpan.FromMinutes(1)));
                Rejected(await copied.FinishAsync(claim, HolonDeliveryOutcome.Completed));
                Rejected(await copied.FinishWithRecoveryAsync(claim, HolonDeliveryOutcome.ReconciliationRequired,
                    new HolonDeliveryRecoveryEvidence(true, new[] { "test-native-receipt" }), "UNKNOWN"));
                Convert.ToInt64(await version.ExecuteScalarAsync()).Should().Be(before,
                    "source mismatch must not mutate copied delivery state, including attempts or leases");

                // Corrupt identity must fail explicitly; delivery never initializes
                // or silently replaces an identity while recovering a queue.
                using (var corrupt = observer.CreateCommand())
                {
                    corrupt.CommandText = "UPDATE CosmicReplicationStoreIdentity SET Identity='invalid'";
                    (await corrupt.ExecuteNonQueryAsync()).Should().Be(1);
                }
                before = Convert.ToInt64(await version.ExecuteScalarAsync());
                Rejected(await copied.ClaimNextAsync("target", "intruder", TimeSpan.FromMinutes(1)),
                    "COSMIC_OUTBOX_SOURCE_STORAGE_BINDING_INVALID");
                Convert.ToInt64(await version.ExecuteScalarAsync()).Should().Be(before);
            }

            // The real source's unchanged native lease remains usable.
            (await bound.RenewAsync(claim, TimeSpan.FromMinutes(1))).Result.Should().BeTrue();
            (await bound.FinishAsync(claim, HolonDeliveryOutcome.Completed)).Result.Should().BeTrue();
            (await bound.InspectAsync(claim.Sequence)).Result.State.Should().Be(HolonDeliveryState.Completed);
            (await bound.ReadRouteHeadsAsync()).Result.Heads.Should().BeEmpty();
        }
        foreach (var path in new[] { copy, copy + "-wal", copy + "-shm", copy + "-journal" })
            if (File.Exists(path)) File.Delete(path);
        File.Delete(journal);
    }
}
