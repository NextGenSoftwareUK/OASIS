using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class OracleCqlContentNativeTests
{
    private sealed record Seed(string Owner, Guid Actor, Guid OtherActor, Guid[] Ids);
    private const string Owner = "cosmic-cql-oracle-content-v1";

    [TestMethod]
    public async Task SavedHolonContentQueriesPreserveCanonicalRecordsAcrossRestart()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var state = Path.Combine(root.FullName, "LocalState", "COSMIC", "OracleContentCql");
        Directory.CreateDirectory(state);
        using var sourceLease = new FileStream(Path.Combine(state, "source.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "seed.json");
        var pending = Path.Combine(state, "seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve uncertain fixture writes; never replay.");
        var uri = Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        Assert.AreEqual("127.0.0.1:11521/FREEPDB1", new OracleConnectionStringBuilder(uri).DataSource);
        var provider = new OracleDBOASIS(uri);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        await using var native = new OracleConnection(uri);
        await native.OpenAsync();
        async Task<string[]> Snapshot(Guid[] ids)
        {
            var names = ids.Select((_, index) => ":id" + index).ToArray();
            await using var command = new OracleCommand("SELECT ID,DATA_JSON,TO_CHAR(MODIFIED_DATE,'YYYY-MM-DD HH24:MI:SS.FF9') FROM OASIS_HOLONS WHERE ID IN (" + string.Join(",", names) + ") ORDER BY ID", native) { BindByName = true };
            for (var index = 0; index < ids.Length; index++) command.Parameters.Add("id" + index, OracleDbType.Raw, 16).Value = ids[index].ToByteArray();
            await using var reader = await command.ExecuteReaderAsync();
            var observed = new List<string>();
            while (await reader.ReadAsync())
            {
                var id = new Guid((byte[])reader.GetValue(0));
                var json = reader.GetString(1);
                Assert.AreEqual(Owner, JObject.Parse(json)["MetaData"]?["demoOwner"]?.Value<string>(), "Never adopt a foreign fixture.");
                observed.Add(id.ToString("D") + "|" + reader.GetString(2) + "|" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            }
            return observed.ToArray();
        }
        Seed seed;
        if (!File.Exists(manifest))
        {
            seed = new Seed(Owner, Guid.NewGuid(), Guid.NewGuid(), Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray());
            Assert.AreEqual(0, (await Snapshot(seed.Ids)).Length, "Never overwrite existing identities.");
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var names = new[] { "Espresso", "Latte", "Tea", "Hidden coffee" };
            var descriptions = new[] { "Strong coffee 100%_ café'☕", "Milky coffee", "Black tea", "coffee" };
            for (var index = 0; index < 4; index++)
            {
                var metadata = new Dictionary<string, object> {
                    ["demoOwner"] = Owner, ["category"] = index == 2 ? "tea" : "coffee",
                    ["location"] = new Dictionary<string, object> { ["city"] = index == 1 ? "Paris" : "London" },
                    ["numericKey"] = new Dictionary<string, object> { ["0"] = index == 0 ? "coffee" : "tea" },
                    ["array"] = new JArray("coffee"), ["unicode"] = index == 0 ? "100%_café'☕" : "other",
                    ["quoted'\"key"] = index == 0 ? "coffee" : "tea"
                };
                if (index == 1) metadata["optional"] = null!;
                if (index == 2) metadata["optional"] = 123;
                if (index <= 1) metadata["long"] = new string('x', 4000) + (index == 0 ? "" : " ");
                var saved = await provider.SaveHolonAsync(new Holon { Id = seed.Ids[index], Name = names[index],
                    Description = descriptions[index], MetaData = metadata,
                    CreatedByAvatarId = index == 3 ? seed.OtherActor : seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved, "Require acknowledged source writes.");
                using var acknowledgement = new FileStream(Path.Combine(state, $"source-write-{index}.ack.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize(acknowledgement, new { id = seed.Ids[index], acknowledged = true }); acknowledgement.Flush(true);
            }
            File.Move(pending, manifest);
        }
        else seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
        Assert.AreEqual(Owner, seed.Owner);
        Assert.AreEqual(4, seed.Ids.Distinct().Count());
        var before = await Snapshot(seed.Ids);
        Assert.AreEqual(4, before.Length, "Missing source is not recreated.");
        var snapshotPath = Path.Combine(state, "source-snapshot.json");
        if (File.Exists(snapshotPath)) CollectionAssert.AreEqual(JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(snapshotPath))!, before);
        else
        {
            using var snapshot = new FileStream(snapshotPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(snapshot, before); snapshot.Flush(true);
        }
        CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.OracleDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.OracleDBOASIS, seed.Ids.Take(3).ToArray()).Result).Result;
        var queries = 0;
        async Task Check(CosmicQuery<Holon> query, params string[] expected)
        {
            var planned = CosmicQueryPlanner.Plan(query, ProviderType.OracleDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.OracleDBOASIS, provider.QueryCapabilities) });
            Assert.IsFalse(planned.IsError, planned.Message);
            var result = await provider.ExecuteCosmicQueryAsync(planned.Result);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Name).ToArray());
            foreach (var row in result.Result) Assert.AreEqual(row.Id.ToString("D"), row.ProviderUniqueStorageKey[ProviderType.OracleDBOASIS]);
            queries++;
        }
        await Check(Query().Where(h => h.Name == "Espresso").Result, "Espresso");
        await Check(Query().Where(h => h.Name.Contains("ss")).Result, "Espresso");
        await Check(Query().Where(h => h.Description.Contains("coffee")).Result, "Espresso", "Latte");
        await Check(Query().Where(h => h.Description.Contains("%")).Result, "Espresso");
        await Check(Query().Where(h => h.Description.Contains("_")).Result, "Espresso");
        await Check(Query().Where(h => h.Description.Contains("é'☕")).Result, "Espresso");
        await Check(Query().Where(h => h.Name == "espresso").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result, "Espresso", "Latte");
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "location", "city") == "London").Result, "Espresso", "Tea");
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "numericKey", "0") == "coffee").Result, "Espresso");
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "array", "0") == "coffee").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "optional") == "123").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "missing") == "coffee").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "unicode") == "100%_café'☕").Result, "Espresso");
        await Check(Query().Where(h => (h.Name == "Espresso" || h.Name == "Tea") &&
            CosmicQueryMetadata.Value<string>(h, "location", "city") == "London").Result, "Espresso", "Tea");
        await Check(Query().Where(h => h.Name == "Hidden coffee" || CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result, "Espresso", "Latte");
        var rawOrder = Comparer<Guid>.Create((left, right) => left.ToByteArray().AsSpan().SequenceCompareTo(right.ToByteArray()));
        var second = seed.Ids.Take(2).OrderBy(id => id, rawOrder).Skip(1).First();
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result.Skip(1).Result.Take(1).Result, second == seed.Ids[0] ? "Espresso" : "Latte");
        await Check(Query().Where(h => h.Name.Contains("")).Result, "Espresso", "Latte", "Tea");
        await Check(Query().Where(h => h.Name == "").Result);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "quoted'\"key") == "coffee").Result, "Espresso");
        var longValue = new string('x', 4000);
        await Check(Query().Where(h => CosmicQueryMetadata.Value<string>(h, "long") == longValue).Result, "Espresso");
        Assert.AreEqual(21, queries);
        CollectionAssert.AreEqual(before, await Snapshot(seed.Ids), "Queries changed canonical JSON or native timestamps.");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
