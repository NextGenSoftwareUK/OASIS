using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.UnitTests;

[TestClass]
public class LitestreamQuerySourceTests
{
    [TestMethod]
    public async Task StandalonePublicSnapshotActivatesAndRejectsLaterChanges()
    {
        var directory = Directory.CreateTempSubdirectory("oasis-litestream-query-");
        var path = Path.Combine(directory.FullName, "snapshot.db");
        var id = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ConnectionString;
        try
        {
            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE oasis_holons (id TEXT PRIMARY KEY, data_json TEXT, is_deleted INTEGER); " +
                    "INSERT INTO oasis_holons VALUES ($id,$json,0)";
                command.Parameters.AddWithValue("$id", id.ToString("D"));
                command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(
                    new Holon { Id = id, CreatedByAvatarId = actor, IsPublic = true },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                await command.ExecuteNonQueryAsync();
            }

            var digest = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path)));
            var source = new LitestreamQuerySource(path, digest, id);
            var ready = await source.ActivateQuerySourceAsync(default);
            Assert.IsFalse(ready.IsError, ready.Exception?.ToString());
            const ProviderType authority = ProviderType.LitestreamOASIS;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, authority, new[] { id }).Result).Result;
            var plan = CosmicQueryPlanner.Plan(query, authority, new[] { new CosmicQueryTarget(authority, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var rows = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(rows.IsError, rows.Exception?.ToString());
            Assert.AreEqual(1, rows.Result.Count);
            Assert.AreEqual(id, rows.Result[0].Id);
            Assert.AreEqual(digest, Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(path))));

            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "UPDATE oasis_holons SET is_deleted=1";
                await command.ExecuteNonQueryAsync();
            }
            var changed = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.AreEqual("CQL_QUERY_FAILED", changed.ErrorCode);
            Assert.IsNull(changed.Result);
        }
        finally
        {
            File.Delete(path);
            directory.Delete();
        }
    }

    [TestMethod]
    public async Task CancelledActivationCannotCreateMissingSnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"oasis-litestream-{Guid.NewGuid():N}.db");
        var source = new LitestreamQuerySource(path, new string('a', 64), Guid.NewGuid());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var result = await source.ActivateQuerySourceAsync(cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.IsFalse(File.Exists(path));
    }
}
