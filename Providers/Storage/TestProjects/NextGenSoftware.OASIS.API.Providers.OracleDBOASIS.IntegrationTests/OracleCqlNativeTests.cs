using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed partial class OracleCqlNativeTests
{
    [TestMethod]
    public async Task NativeMembershipAuthorizationPagingAndExactCleanup()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts")))
            root = root.Parent;
        Assert.IsNotNull(root);

        var work = Path.Combine(root.FullName, "LocalState", "COSMIC", "OracleCqlCurrent");
        Directory.CreateDirectory(work);
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(work, "fixture.pending.json");
        Assert.IsFalse(File.Exists(journal), "Reconcile the original fixture before another write.");

        var connectionString = Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING");
        var settings = new OracleConnectionStringBuilder(connectionString);
        Assert.AreEqual("127.0.0.1:11521/FREEPDB1", settings.DataSource);
        var provider = new OracleDBOASIS(connectionString!);
        var rows = Enumerable.Range(0, 4).Select(index => new Holon
        {
            Id = Guid.NewGuid(), Name = "COSMIC Oracle owned coffee " + index
        }).ToArray();

        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await using var verifier = new OracleConnection(connectionString);
            await verifier.OpenAsync();

            async Task<string?> Read(Guid id)
            {
                await using var command = new OracleCommand("SELECT DATA_JSON FROM OASIS_HOLONS WHERE ID=:id", verifier)
                { BindByName = true };
                command.Parameters.Add("id", OracleDbType.Raw).Value = id.ToByteArray();
                var result = await command.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? null : Convert.ToString(result);
            }

            foreach (var row in rows) Assert.IsNull(await Read(row.Id));

            // Persist intent before the first native write. Failure retains this journal.
            using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write,
                       FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(file, new { ids = rows.Select(row => row.Id), names = rows.Select(row => row.Name) });
                file.Flush(true);
            }

            foreach (var row in rows)
            {
                var saved = await provider.SaveHolonAsync(row, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved);
            }

            // The fourth native record exists but is excluded by trusted authorization.
            var allowed = rows.Take(3).Select(row => row.Id).ToArray();
            var requested = rows.Select(row => row.Id).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.OracleDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.OracleDBOASIS, allowed).Result).Result
                .Where(row => requested.Contains(row.Id)).Result;

            async Task<NextGenSoftware.OASIS.Common.OASISResult<IReadOnlyList<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>>> Execute(CosmicQuery<Holon> source, int budget = 100)
            {
                var plan = CosmicQueryPlanner.Plan(source, ProviderType.OracleDBOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.OracleDBOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                return await provider.ExecuteCosmicQueryAsync(plan.Result, budget);
            }

            var all = await Execute(query);
            Assert.IsFalse(all.IsError, all.Exception?.ToString() ?? all.Message);
            CollectionAssert.AreEquivalent(allowed, all.Result.Select(row => row.Id).ToArray());
            foreach (var row in all.Result)
            {
                Assert.AreEqual(rows.Single(seed => seed.Id == row.Id).Name, row.Name);
                Assert.AreEqual(row.Id.ToString("D"), row.ProviderUniqueStorageKey[ProviderType.OracleDBOASIS]);
            }

            var subsetIds = allowed.Take(2).ToArray();
            var subset = await Execute(query.Where(row => subsetIds.Contains(row.Id)).Result);
            Assert.IsFalse(subset.IsError, subset.Message);
            CollectionAssert.AreEquivalent(subsetIds, subset.Result.Select(row => row.Id).ToArray());

            var page = await Execute(query.Skip(1).Result.Take(1).Result);
            Assert.IsFalse(page.IsError, page.Message);
            Assert.AreEqual(all.Result[1].Id, page.Result.Single().Id);
            var empty = await Execute(query.Take(0).Result);
            Assert.IsFalse(empty.IsError, empty.Message);
            Assert.AreEqual(0, empty.Result.Count);
            var limited = await Execute(query, 2);
            Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode);
            Assert.IsNull(limited.Result);

            if (Environment.GetEnvironmentVariable("OASIS_ORACLE_MONGO_DEMO") == "1")
                await VerifyMongoAsync(root.FullName, provider, allowed);

            // Compare exact acknowledged envelopes before deleting only this fixture.
            foreach (var row in rows)
            {
                var json = await Read(row.Id);
                Assert.IsNotNull(json);
                using var document = JsonDocument.Parse(json);
                Assert.AreEqual(row.Name, document.RootElement.GetProperty("Name").GetString());
                await using var cleanup = new OracleCommand(
                    "DELETE FROM OASIS_HOLONS WHERE ID=:id AND DBMS_LOB.COMPARE(DATA_JSON,:expected)=0", verifier)
                { BindByName = true };
                cleanup.Parameters.Add("id", OracleDbType.Raw).Value = row.Id.ToByteArray();
                cleanup.Parameters.Add("expected", OracleDbType.Clob).Value = json;
                Assert.AreEqual(1, await cleanup.ExecuteNonQueryAsync());
                Assert.IsNull(await Read(row.Id));
            }
            File.Delete(journal);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }
}
