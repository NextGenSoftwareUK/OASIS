using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class SurrealDbNativeCqlTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    private static void Durable(string path, object value)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(file, value);
        file.Flush(true);
    }

    [TestMethod]
    public async Task OriginalNativeRecordsFilterAndReloadWithoutNewWrites()
    {
        var state = Environment.GetEnvironmentVariable("COSMIC_SURREALDB_ROOT")
            ?? throw new InvalidOperationException("Use the retained native runner.");
        var identity = Path.Combine(state, "identity.json");
        var pending = Path.Combine(state, "seed.pending.json");
        var baseline = Path.Combine(state, "native-baseline.json");
        Assert.IsFalse(File.Exists(pending), "Unknown original write outcome; reconcile, never replay.");
        var created = !File.Exists(identity);
        if (!created) Assert.IsTrue(File.Exists(baseline), "Original baseline missing; never reseed.");
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(state, "credentials.json")));
        var username = config.RootElement.GetProperty("username").GetString()!;
        var password = config.RootElement.GetProperty("password").GetString()!;
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:18001") };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password)));
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        http.DefaultRequestHeaders.Add("surreal-ns", "oasis");
        http.DefaultRequestHeaders.Add("surreal-db", "oasis");

        async Task<JsonElement> Sql(string sql)
        {
            using var response = await http.PostAsync("/sql", new StringContent(sql, Encoding.UTF8, "text/plain"));
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            foreach (var statement in body.RootElement.EnumerateArray())
                Assert.AreEqual("OK", statement.GetProperty("status").GetString(), statement.GetRawText());
            return body.RootElement[body.RootElement.GetArrayLength() - 1].GetProperty("result").Clone();
        }

        var seed = created ? new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray())
            : JsonSerializer.Deserialize<Seed>(File.ReadAllText(identity))!;
        if (created)
        {
            // This exclusively owned first-use store must have no business records.
            await Sql("DEFINE NAMESPACE IF NOT EXISTS oasis; USE NS oasis; DEFINE DATABASE IF NOT EXISTS oasis;");

        }
        var provider = new SurrealDBOASIS("http://127.0.0.1:18001", username, password);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString() ?? activation.Message);
        Assert.IsTrue(provider.IsProviderActivated);
        try
        {
            if (created)
            {
                Assert.AreEqual(0, (await Sql("SELECT * FROM oasis_holons;")).GetArrayLength());
                Durable(pending, seed);
                var names = new[] { "Espresso", "Latte", "Tea" };
                for (var i = 0; i < 3; i++)
                {
                    var row = new Holon { Id = seed.Ids[i], Name = names[i], CreatedByAvatarId = seed.Actor, IsNewHolon = true };
                    var saved = await provider.SaveHolonAsync(row, saveChildren: false);
                    Assert.IsFalse(saved.IsError, saved.Exception?.ToString() ?? saved.Message);
                    Assert.IsTrue(saved.IsSaved);
                    Assert.AreEqual(1, saved.SavedCount);
                    Assert.IsTrue(row.IsNewHolon, "Only the manager may publish clean state.");
                    Assert.AreEqual(row.Id.ToString("D"), row.ProviderUniqueStorageKey[ProviderType.SurrealDBOASIS]);
                    Durable(Path.Combine(state, $"write-{i}.ack.json"), new { id = row.Id });
                }
            }

            async Task<string> NativeHash()
            {
                // Fixture verification only; CQL never uses this table enumeration.
                var rows = await Sql("SELECT * FROM oasis_holons ORDER BY id;");
                Assert.AreEqual(3, rows.GetArrayLength());
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rows.GetRawText())));
            }
            var before = await NativeHash();
            if (created)
            {
                Durable(baseline, new { hash = before });
                Durable(identity, seed);
                File.Delete(pending);
            }
            else
            {
                using var original = JsonDocument.Parse(File.ReadAllText(baseline));
                Assert.AreEqual(original.RootElement.GetProperty("hash").GetString(), before);
            }
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SurrealDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.SurrealDBOASIS, seed.Ids).Result).Result;

            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, ProviderType.SurrealDBOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.SurrealDBOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                return result.Result;
            }
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte", "Tea" }, (await Read(query)).Select(r => r.Name).ToArray());
            var coffee = seed.Ids.Take(2).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte" },
                (await Read(query.Where(r => coffee.Contains(r.Id)).Result)).Select(r => r.Name).ToArray());
            Assert.AreEqual(1, (await Read(query.Skip(1).Take(1).Result)).Count);
            CollectionAssert.AreEquivalent(seed.Ids, (await Read(query)).Select(r => r.Id).ToArray());
            Assert.AreEqual(before, await NativeHash(), "CQL must not alter native records.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
