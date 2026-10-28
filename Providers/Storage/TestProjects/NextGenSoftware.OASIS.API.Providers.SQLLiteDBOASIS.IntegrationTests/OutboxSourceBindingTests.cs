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
    private sealed class SourceIdentityRaceClock(string connectionString)
        : NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.IHyperDriveClock
    {
        public int Attempts;
        public DateTime UtcNow
        {
            get
            {
                // The production clock boundary follows identity admission and
                // precedes each lease query/update. Use another real connection
                // to attempt an identity replacement at that exact boundary.
                using var connection = new SqliteConnection(connectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandTimeout = 1;
                command.CommandText = "UPDATE CosmicReplicationStoreIdentity SET Identity=$replacement WHERE Id=1";
                command.Parameters.AddWithValue("$replacement", Guid.NewGuid().ToString("D"));
                Action replace = () => command.ExecuteNonQuery();
                replace.Should().Throw<SqliteException>().Which.SqliteErrorCode.Should().Be(5);
                Attempts++;
                return DateTime.UtcNow;
            }
        }
    }

    [Fact]
    public async Task BoundOutboxIdentityCannotChangeBetweenAdmissionAndLeaseMutation()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var identity = await provider.CaptureStorageIdentityAsync();
        identity.IsError.Should().BeFalse(identity.Message);
        var request = OutboxCreate(new Holon { Id=Guid.NewGuid(), Name="outbox identity race" });
        var enrollment = new HolonReplicationEnrollment("race-source", new[] { "target" }, identity.Result);
        var saved = await provider.ExecuteReplicatingHolonWriteAsync(request, enrollment);
        saved.IsError.Should().BeFalse(saved.Message);
        var clock = new SourceIdentityRaceClock(TestConnectionString);
        var bound = new SqliteHolonReplicationOutbox(TestConnectionString, clock, identity.Result);
        var claim = await bound.ClaimNextAsync("target", "original-owner", TimeSpan.FromMinutes(5));
        claim.IsError.Should().BeFalse(claim.Message + claim.Exception);
        (await bound.RenewAsync(claim.Result, TimeSpan.FromMinutes(1))).Result.Should().BeTrue();
        (await bound.FinishAsync(claim.Result, HolonDeliveryOutcome.Completed)).Result.Should().BeTrue();
        clock.Attempts.Should().Be(3);
        var retained = await provider.CaptureStorageIdentityAsync();
        retained.Result.BindingFingerprint.Should().Be(identity.Result.BindingFingerprint);
        (await bound.InspectAsync(claim.Result.Sequence)).Result.State.Should().Be(HolonDeliveryState.Completed);
    }

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
