using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitReplicaBaselinePreservesVersionFourAndSequentialDeliveryAndExactReplay(bool synchronous)
    {
        await WithVersionFourSource(async (source, committed, actor) =>
        {
            using var target = new SQLLiteDBOASIS(TestConnectionString);
            Assert.False((await target.ActivateProviderAsync()).IsError);
            var baseline = ReplicaSnapshot(committed, actor);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await target.ApplyCommittedHolonReplicaAsync(baseline)).ErrorCode);

            var installed = synchronous ? target.InitializeCommittedHolonReplica(baseline)
                : await target.InitializeCommittedHolonReplicaAsync(baseline);
            Assert.False(installed.IsError, installed.Message + installed.Exception);
            var original = installed.Result.Single();
            Assert.Equal(4, original.Version);
            Assert.Equal(committed.VersionId, original.VersionId);
            Assert.Equal(committed.PreviousVersionId, original.PreviousVersionId);
            Assert.Equal(committed.CreatedByAvatarId, original.CreatedByAvatarId);
            Assert.Single(original.Children);
            Assert.NotEqual(CosmicConcurrency.GetProviderRevisionId(committed, ProviderType.SQLLiteDBOASIS),
                CosmicConcurrency.GetProviderRevisionId(original, ProviderType.SQLLiteDBOASIS));

            var nextInput = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(committed);
            nextInput.Children = committed.Children.Select(child => (IHolon)NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(child)).ToList();
            nextInput.Name = "source version five";
            var next = await CommitReplicaSource(source, nextInput, actor);
            var delivered = await target.ApplyCommittedHolonReplicaAsync(ReplicaSnapshot(next, actor));
            Assert.False(delivered.IsError, delivered.Message + delivered.Exception);
            Assert.Equal(5, delivered.Result.Single().Version);

            var replay = await target.InitializeCommittedHolonReplicaAsync(baseline);
            Assert.False(replay.IsError, replay.Message + replay.Exception);
            Assert.Equal(original.VersionId, replay.Result.Single().VersionId);
            Assert.Equal(4, replay.Result.Single().Version);
            Assert.Equal(5, (await target.LoadHolonAsync(committed.Id, false)).Result.Version);
            Assert.Equal("COSMIC_OPERATION_ID_REUSED", (await target.ApplyCommittedHolonReplicaAsync(baseline)).ErrorCode);
        });
    }

    [Fact]
    public async Task ExplicitReplicaBaselineReceiptAndHeadSurviveProviderRestart()
    {
        await WithVersionFourSource(async (_, committed, actor) =>
        {
            var baseline = ReplicaSnapshot(committed, actor);
            Guid? acknowledgedRevision;
            using (var writer = new SQLLiteDBOASIS(TestConnectionString))
            {
                Assert.False((await writer.ActivateProviderAsync()).IsError);
                var result = await writer.InitializeCommittedHolonReplicaAsync(baseline);
                Assert.False(result.IsError, result.Message + result.Exception);
                acknowledgedRevision = CosmicConcurrency.GetProviderRevisionId(result.Result.Single(), ProviderType.SQLLiteDBOASIS);
            }
            using var reopened = new SQLLiteDBOASIS(TestConnectionString);
            Assert.False((await reopened.ActivateProviderAsync()).IsError);
            var replay = await reopened.InitializeCommittedHolonReplicaAsync(baseline);
            Assert.False(replay.IsError, replay.Message + replay.Exception);
            Assert.Equal(acknowledgedRevision, CosmicConcurrency.GetProviderRevisionId(replay.Result.Single(), ProviderType.SQLLiteDBOASIS));
            Assert.Equal(committed.VersionId, (await reopened.LoadHolonAsync(committed.Id, false)).Result.VersionId);
        });
    }

    [Fact]
    public async Task ConcurrentInitializationsOfTheSameTargetGraphHaveOneWinner()
    {
        await WithVersionFourSource(async (_, committed, actor) =>
        {
            using var first = new SQLLiteDBOASIS(TestConnectionString);
            using var second = new SQLLiteDBOASIS(TestConnectionString);
            Assert.False((await first.ActivateProviderAsync()).IsError);
            Assert.False((await second.ActivateProviderAsync()).IsError);
            var a = ReplicaSnapshot(committed, actor);
            var b = ReplicaSnapshot(committed, actor);
            using var start = new ManualResetEventSlim();
            var one = Task.Run(() => { start.Wait(); return first.InitializeCommittedHolonReplica(a); });
            var two = Task.Run(() => { start.Wait(); return second.InitializeCommittedHolonReplica(b); });
            start.Set();
            var outcomes = await Task.WhenAll(one, two);
            Assert.Single(outcomes.Where(result => !result.IsError));
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", Assert.Single(outcomes.Where(result => result.IsError)).ErrorCode);
            Assert.Equal(1, ReplicaCount("CosmicReplicaHead", "HolonId", committed.Id));
            Assert.Equal(1, ReplicaCount("CosmicWriteReceipt", "OperationId", a.OperationId)
                + ReplicaCount("CosmicWriteReceipt", "OperationId", b.OperationId));
        });
    }

    [Fact]
    public async Task BaselineCannotReplaceIndependentChildAndRejectsTheWholeGraphAtomically()
    {
        await WithVersionFourSource(async (_, committed, actor) =>
        {
            using var target = new SQLLiteDBOASIS(TestConnectionString);
            Assert.False((await target.ActivateProviderAsync()).IsError);
            var childId = committed.Children.Single().Id;
            var independent = await CommitReplicaSource(target, new Holon { Id = childId, Name = "independent target" }, actor);
            var baseline = ReplicaSnapshot(committed, actor);

            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await target.InitializeCommittedHolonReplicaAsync(baseline)).ErrorCode);
            Assert.Equal(independent.VersionId, (await target.LoadHolonAsync(childId, false)).Result.VersionId);
            Assert.Equal(0, ReplicaCount("Holon", "Id", committed.Id));
            Assert.Equal(0, ReplicaCount("CosmicWriteReceipt", "OperationId", baseline.OperationId));
            Assert.Equal(0, ReplicaCount("CosmicReplicaHead", "HolonId", committed.Id));
        });
    }

    [Fact]
    public async Task BaselineRefusesAnUnrecognizedReceiptInsteadOfAssumingTargetAbsence()
    {
        await WithVersionFourSource(async (_, committed, actor) =>
        {
            using var target = new SQLLiteDBOASIS(TestConnectionString);
            Assert.False((await target.ActivateProviderAsync()).IsError);
            using (var connection = new SqliteConnection(TestConnectionString))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO CosmicWriteReceipt(OperationId,RequestHash,ResultJson,CommittedAtUtc) VALUES($id,'unknown','{}','2026-10-30T00:00:00Z')";
                command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
                command.ExecuteNonQuery();
            }
            var baseline = ReplicaSnapshot(committed, actor);
            var result = await target.InitializeCommittedHolonReplicaAsync(baseline);
            Assert.True(result.IsError);
            Assert.Equal(0, ReplicaCount("Holon", "Id", committed.Id));
            Assert.Equal(0, ReplicaCount("CosmicWriteReceipt", "OperationId", baseline.OperationId));
        });
    }

    [Fact]
    public async Task BaselineCannotResurrectAnIndependentHardDeleteEvenWithHistoryDisabled()
    {
        await WithVersionFourSource(async (_, committed, actor) =>
        {
            using var target = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = false };
            Assert.False((await target.ActivateProviderAsync()).IsError);
            var independent = await CommitReplicaSource(target, new Holon { Id = committed.Id, Name = "independent target" }, actor);
            var deletion = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
                new[] { new HolonWriteCondition(independent.Id, CosmicConcurrency.GetProviderRevisionId(independent, ProviderType.SQLLiteDBOASIS)) }),
                ConditionalHolonOperation.HardDelete, new[] { independent }, saveChildren: false);
            Assert.False((await target.ExecuteConditionalHolonWriteAsync(deletion)).IsError);
            Assert.Equal(0, ReplicaCount("Holon", "Id", committed.Id));
            Assert.Equal(0, ReplicaCount("CosmicHolonRevision", "HolonId", committed.Id));

            var baseline = ReplicaSnapshot(committed, actor);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT", (await target.InitializeCommittedHolonReplicaAsync(baseline)).ErrorCode);
            Assert.Equal(0, ReplicaCount("Holon", "Id", committed.Id));
            Assert.Equal(0, ReplicaCount("CosmicWriteReceipt", "OperationId", baseline.OperationId));
        });
    }

    private static CommittedHolonReplicaRequest ReplicaSnapshot(IHolon root, Guid actor)
        => new(Guid.NewGuid(), "owned-initial-baseline-source", ProviderType.SQLLiteDBOASIS, actor,
            ConditionalHolonOperation.Save, new[] { root });

    private static async Task<IHolon> CommitReplicaSource(SQLLiteDBOASIS provider, IHolon root, Guid actor)
    {
        var selected = new[] { root }.Concat(root.Children).ToArray();
        var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), actor, ProviderType.SQLLiteDBOASIS,
            selected.Select(record => new HolonWriteCondition(record.Id, record.Version == 0 ? null
                : CosmicConcurrency.GetProviderRevisionId(record, ProviderType.SQLLiteDBOASIS)))), ConditionalHolonOperation.Save, new[] { root });
        var result = await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.False(result.IsError, result.Message + result.Exception);
        return result.Result.Single();
    }

    private async Task WithVersionFourSource(Func<SQLLiteDBOASIS, IHolon, Guid, Task> verify)
    {
        var path = Path.Combine(Path.GetDirectoryName(_databasePath)!, "initial-baseline-source-current.db");
        Assert.False(File.Exists(path), "A retained source fixture must be inspected before reuse.");
        var source = new SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var completed = false;
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);
            var actor = Guid.NewGuid();
            IHolon root = new Holon { Id = Guid.NewGuid(), Name = "committed baseline", Children = new List<IHolon>
                { new Holon { Id = Guid.NewGuid(), Name = "committed child" } } };
            for (var version = 1; version <= 4; version++) root = await CommitReplicaSource(source, root, actor);
            await verify(source, root, actor);
            completed = true;
        }
        finally
        {
            source.Dispose();
            if (completed)
                foreach (var owned in new[] { path, path + "-wal", path + "-shm" })
                    if (File.Exists(owned)) File.Delete(owned);
        }
    }

    private long ReplicaCount(string table, string field, Guid id)
    {
        using var connection = new SqliteConnection(TestConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        // Table/column names are private test constants, never request input.
        command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE {field}=$id";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        return (long)command.ExecuteScalar()!;
    }
}
