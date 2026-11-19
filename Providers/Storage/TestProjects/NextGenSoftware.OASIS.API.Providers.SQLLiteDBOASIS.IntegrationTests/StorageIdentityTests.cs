using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task StorageIdentitySurvivesIndependentClientsAndRejectsEndpointCopyEquivalence()
    {
        var journal = Path.Combine(Path.GetDirectoryName(_databasePath)!, "scoped-visibility.pending.json");
        var copy = Path.Combine(Path.GetDirectoryName(_databasePath)!, "binding-copy.db");
        File.Exists(copy).Should().BeFalse("an unfinished copy must not be overwritten");
        await File.WriteAllTextAsync(journal, JsonSerializer.Serialize(new { test = "native-binding", source = _databasePath, copy }));
        string capturedFingerprint = null;
        using (var first = new SQLLiteDBOASIS(TestConnectionString))
        using (var second = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await first.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await second.ActivateProviderAsync()).IsError.Should().BeFalse();
            using var barrier = new Barrier(2);
            var bindings = await Task.WhenAll(new[] { first, second }.Select(provider => Task.Run(async () =>
            {
                barrier.SignalAndWait(TimeSpan.FromSeconds(10)).Should().BeTrue();
                return await provider.CaptureStorageIdentityAsync();
            })));
            bindings.Should().OnlyContain(result => !result.IsError);
            bindings.Select(result => result.Result.StoreId).Distinct().Should().HaveCount(1);
            bindings.Select(result => result.Result.BindingFingerprint).Distinct().Should().HaveCount(1);
            var original = bindings[0].Result;
            capturedFingerprint = original.BindingFingerprint;
            original.EndpointFingerprint.Should().HaveLength(64);
            original.StoreId.Should().NotBeEmpty();

            // No provider data write or metadata rewrite on a later capture.
            using var observer = new SqliteConnection(TestConnectionString);
            await observer.OpenAsync();
            using var version = observer.CreateCommand();
            version.CommandText = "PRAGMA data_version";
            var before = Convert.ToInt64(await version.ExecuteScalarAsync());
            var again = await second.CaptureStorageIdentityAsync();
            again.IsError.Should().BeFalse(again.Message);
            again.Result.BindingFingerprint.Should().Be(original.BindingFingerprint);
            Convert.ToInt64(await version.ExecuteScalarAsync()).Should().Be(before);

            // Native online backup preserves logical store identity. Its different
            // endpoint must nevertheless produce a different routing binding.
            using (var from = new SqliteConnection(TestConnectionString))
            using (var to = new SqliteConnection($"Data Source={copy};Pooling=False"))
            {
                await from.OpenAsync();
                await to.OpenAsync();
                from.BackupDatabase(to);
            }
            using var copied = new SQLLiteDBOASIS($"Data Source={copy};Pooling=False");
            (await copied.ActivateProviderAsync()).IsError.Should().BeFalse();
            var alternate = await copied.CaptureStorageIdentityAsync();
            alternate.IsError.Should().BeFalse(alternate.Message);
            alternate.Result.StoreId.Should().Be(original.StoreId);
            alternate.Result.EndpointFingerprint.Should().NotBe(original.EndpointFingerprint);
            alternate.Result.BindingFingerprint.Should().NotBe(original.BindingFingerprint);
        }
        using (var reopened = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
            var persisted = await reopened.CaptureStorageIdentityAsync();
            persisted.IsError.Should().BeFalse(persisted.Message);
            persisted.Result.BindingFingerprint.Should().Be(capturedFingerprint);
        }
        foreach (var path in new[] { copy, copy + "-wal", copy + "-shm", copy + "-journal" })
            if (File.Exists(path)) File.Delete(path);
        File.Delete(journal);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("AAAAAAAA-AAAA-4AAA-AAAA-AAAAAAAAAAAA")]
    public async Task CorruptStorageIdentityIsRejectedWithoutReplacement(string stored)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var connection = new SqliteConnection(TestConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE CosmicReplicationStoreIdentity(Id INTEGER PRIMARY KEY,Identity TEXT NOT NULL); INSERT INTO CosmicReplicationStoreIdentity VALUES(1,$id)";
        command.Parameters.AddWithValue("$id", stored);
        await command.ExecuteNonQueryAsync();
        command.Parameters.Clear();
        command.CommandText = "PRAGMA data_version";
        var before = Convert.ToInt64(await command.ExecuteScalarAsync());
        var capture = await provider.CaptureStorageIdentityAsync();
        capture.IsError.Should().BeTrue();
        capture.ErrorCode.Should().Be("COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED");
        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(before);
        command.CommandText = "SELECT Identity FROM CosmicReplicationStoreIdentity WHERE Id=1";
        (await command.ExecuteScalarAsync()).Should().Be(stored);
        command.CommandText = "SELECT COUNT(*) FROM CosmicReplicationStoreIdentity";
        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(1);
    }

    [Fact]
    public async Task EmptyExistingStorageIdentityIsNotSilentlyReinitialized()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var connection = new SqliteConnection(TestConnectionString);
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE CosmicReplicationStoreIdentity(Id INTEGER PRIMARY KEY,Identity TEXT NOT NULL)";
        await command.ExecuteNonQueryAsync();
        command.CommandText = "PRAGMA data_version";
        var before = Convert.ToInt64(await command.ExecuteScalarAsync());
        var capture = await provider.CaptureStorageIdentityAsync();
        capture.ErrorCode.Should().Be("COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED");
        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(before);
        command.CommandText = "SELECT COUNT(*) FROM CosmicReplicationStoreIdentity";
        Convert.ToInt64(await command.ExecuteScalarAsync()).Should().Be(0);
    }

    [Fact]
    public async Task InactiveAndPreCancelledStorageIdentityCaptureCreateNoDatabase()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var inactive = await provider.CaptureStorageIdentityAsync();
        inactive.ErrorCode.Should().Be("COSMIC_STORAGE_IDENTITY_NOT_READY");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Func<Task> capture = () => provider.CaptureStorageIdentityAsync(cancelled.Token);
        await capture.Should().ThrowAsync<OperationCanceledException>();
        File.Exists(_databasePath).Should().BeFalse();
    }

    [Fact]
    public async Task StorageIdentityNeverCreatesMissingStoreOrUpgradesMemoryOrReadOnlyModes()
    {
        using (var absent = new SQLLiteDBOASIS(TestConnectionString) { IsProviderActivated = true })
        {
            var failed = await absent.CaptureStorageIdentityAsync();
            failed.IsError.Should().BeTrue();
            File.Exists(_databasePath).Should().BeFalse();
        }
        using (var memory = new SQLLiteDBOASIS("Data Source=:memory:") { IsProviderActivated = true })
            (await memory.CaptureStorageIdentityAsync()).ErrorCode.Should().Be("COSMIC_STORAGE_IDENTITY_UNSUPPORTED");
        using (var initialized = new SQLLiteDBOASIS(TestConnectionString))
            (await initialized.ActivateProviderAsync()).IsError.Should().BeFalse();
        var before = await File.ReadAllBytesAsync(_databasePath);
        using (var readOnly = new SQLLiteDBOASIS($"Data Source={_databasePath};Mode=ReadOnly;Pooling=False") { IsProviderActivated = true })
            (await readOnly.CaptureStorageIdentityAsync()).ErrorCode.Should().Be("COSMIC_STORAGE_IDENTITY_READ_ONLY");
        (await File.ReadAllBytesAsync(_databasePath)).Should().Equal(before);
    }
}
