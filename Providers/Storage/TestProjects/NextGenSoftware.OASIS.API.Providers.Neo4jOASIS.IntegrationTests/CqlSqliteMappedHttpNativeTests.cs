using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifySqliteMappedNativeHttp(SqliteProvider provider, Guid actor, string rootPath, List<object> checks)
    {
        var folder = Path.GetFullPath(Path.Combine(rootPath, "LocalState/COSMIC/SqliteMappedHttpCurrent"));
        Assert.IsTrue(folder.StartsWith(Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "native.db");
        foreach (var candidate in new[] { path, path + "-wal", path + "-shm" })
            Assert.IsFalse(File.Exists(candidate), "Preserve any unreconciled owned native fixture.");
        var previous = provider.NativeImportConnectionString;
        try
        {
            await using (var source = new SqliteConnection("Data Source=" + path + ";Pooling=False"))
            {
                await source.OpenAsync();
                await using var command = source.CreateCommand();
                command.CommandText = "CREATE TABLE items(id INTEGER PRIMARY KEY, text TEXT, rate, low, high, tiny, zero, badInf, badNegativeInf, optionalDouble); "
                    + "INSERT INTO items VALUES(1,$text,$rate,$low,$high,$tiny,$zero,$inf,$negativeInf,NULL),"
                    + "(2,$nul,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL)";
                command.Parameters.AddWithValue("$rate", 3.25d);
                command.Parameters.AddWithValue("$low", -double.MaxValue);
                command.Parameters.AddWithValue("$high", double.MaxValue);
                command.Parameters.AddWithValue("$tiny", double.Epsilon);
                command.Parameters.AddWithValue("$zero", -0d);
                command.Parameters.AddWithValue("$inf", double.PositiveInfinity);
                command.Parameters.AddWithValue("$negativeInf", double.NegativeInfinity);
                command.Parameters.AddWithValue("$text", "😀");
                command.Parameters.AddWithValue("$nul", "abc\u0000def");
                await command.ExecuteNonQueryAsync();
            }
            var before = await File.ReadAllBytesAsync(path);
            provider.NativeImportConnectionString = "Data Source=" + path + ";Pooling=False";
            await VerifyNativeDoubleProjection(provider, "items",
                new NativeRecordFilter("id", CosmicQueryComparisonOperator.Equal, 1L), "id", false, checks);
            var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, provider.NativeDataProviderType,
                "items", VirtualHolonMapping.SandboxConnectionName, provider.DescribeVirtualQueryConnection(null),
                new Dictionary<string, string> { ["textAlias"] = "text" });
            var original = await mapping.QueryAsync(actor, provider, null,
                new[] { new NativeRecordFilter("textAlias", CosmicQueryComparisonOperator.Equal, 2L, NativeRecordFunction.Utf16Length) },
                new[] { "textAlias" }, 1);
            Assert.IsFalse(original.IsError, original.Message + original.Exception);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await CqlV1Web4NativeTests.VerifyMappedNativeHttpAsync(provider, mapping, original.Result.Single());
            CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(path));
            checks.Add(new { provider = "SQLLiteDBOASIS", nativeHttp = "mixed raw/computed rows and private definition from real storage",
                jwt = true, discovery = "controlled owned fixture", successAttempts = 3, foreignActorAttempts = 2, globalOrderingVerified = true, orderedAttempts = 3, identityHttpVerified = true, mixedCountHttpVerified = true, mixedNumericHttpVerified = true,
                unchangedSource = true, elapsedMilliseconds = watch.ElapsedMilliseconds });
        }
        finally
        {
            provider.NativeImportConnectionString = previous;
            SqliteConnection.ClearAllPools();
            foreach (var candidate in new[] { path, path + "-wal", path + "-shm" })
                if (File.Exists(candidate)) File.Delete(candidate);
        }
    }
}
