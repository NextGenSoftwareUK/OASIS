using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class RavenDbNativeCqlTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    private static void Durable(string path, object value)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(file, value);
        file.Flush(true);
    }

    [TestMethod]
    public async Task OriginalSecuredNativeRecordsFilterAndReloadWithoutNewWrites()
    {
        var state = Environment.GetEnvironmentVariable("COSMIC_RAVENDB_ROOT")
            ?? throw new InvalidOperationException("Use the retained secured native runner.");
        Assert.AreEqual(Path.Combine(state, "ca.pem"), Environment.GetEnvironmentVariable("SSL_CERT_FILE"));
        Assert.IsTrue(OperatingSystem.IsLinux());
        var identity = Path.Combine(state, "identity.json");
        var pending = Path.Combine(state, "seed.pending.json");
        var baseline = Path.Combine(state, "native-baseline.json");
        Assert.IsFalse(File.Exists(pending), "Original write outcome unresolved; reconcile without replay.");
        var created = !File.Exists(identity);
        if (!created) Assert.IsTrue(File.Exists(baseline), "Missing original baseline; never reseed.");
        var seed = created ? new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray())
            : JsonSerializer.Deserialize<Seed>(File.ReadAllText(identity))!;
        using var certificate = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(state, "client.pfx"), null,
            X509KeyStorageFlags.EphemeralKeySet);
        using var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(certificate);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:18003") };
        var provider = new RavenDBOASIS(new[] { "https://localhost:18003" }, "oasis", certificate);
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString() ?? activated.Message);
        Assert.IsTrue(provider.IsProviderActivated);
        try
        {
            async Task VerifyCatalogue(bool empty = false)
            {
                // Fixture-only enumeration. CQL uses a bounded native multi-load.
                using var listing = JsonDocument.Parse(await http.GetStringAsync("/databases/oasis/docs?start=0&pageSize=10"));
                var ids = listing.RootElement.GetProperty("Results").EnumerateArray()
                    .Select(row => row.GetProperty("@metadata").GetProperty("@id").GetString()!).ToArray();
                CollectionAssert.AreEquivalent(empty ? Array.Empty<string>() : seed.Ids.Select(id => $"OasisHolons/{id:D}").ToArray(), ids);
            }

            if (created)
            {
                await VerifyCatalogue(empty: true);
                Durable(pending, seed);
                var names = new[] { "Espresso", "Latte", "Tea" };
                for (var index = 0; index < seed.Ids.Length; index++)
                {
                    var row = new Holon { Id = seed.Ids[index], Name = names[index], CreatedByAvatarId = seed.Actor, IsNewHolon = true };
                    var saved = await provider.SaveHolonAsync(row, saveChildren: false);
                    Assert.IsFalse(saved.IsError, saved.Exception?.ToString() ?? saved.Message);
                    Assert.IsTrue(saved.IsSaved);
                    Assert.AreEqual(1, saved.SavedCount);
                    Assert.IsTrue(row.IsNewHolon, "Manager owns clean-state publication.");
                    Assert.AreEqual(row.Id.ToString("D"), row.ProviderUniqueStorageKey[ProviderType.RavenDBOASIS]);
                    Durable(Path.Combine(state, $"write-{index}.ack.json"), new { id = row.Id, changeVector = saved.MetaData["RavenDB.ChangeVector"] });
                }
            }

            async Task<Dictionary<string, string>> Hashes()
            {
                var hashes = new Dictionary<string, string>();
                for (var index = 0; index < seed.Ids.Length; index++)
                {
                    var key = $"OasisHolons/{seed.Ids[index]:D}";
                    var bytes = await http.GetByteArrayAsync("/databases/oasis/docs?id=" + Uri.EscapeDataString(key));
                    using var native = JsonDocument.Parse(bytes);
                    Assert.AreEqual(1, native.RootElement.GetProperty("Results").GetArrayLength());
                    var metadata = native.RootElement.GetProperty("Results")[0].GetProperty("@metadata");
                    Assert.AreEqual(key, metadata.GetProperty("@id").GetString());
                    using var receipt = JsonDocument.Parse(File.ReadAllText(Path.Combine(state, $"write-{index}.ack.json")));
                    Assert.AreEqual(receipt.RootElement.GetProperty("changeVector").GetString(), metadata.GetProperty("@change-vector").GetString());
                    hashes[key] = Convert.ToHexString(SHA256.HashData(bytes));
                }
                return hashes;
            }

            await VerifyCatalogue();
            var before = await Hashes();
            if (created)
            {
                Durable(baseline, before);
                Durable(identity, seed);
                File.Delete(pending);
            }
            else CollectionAssert.AreEquivalent(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(baseline))!.ToArray(), before.ToArray());

            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RavenDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.RavenDBOASIS, seed.Ids).Result).Result;
            async Task<IReadOnlyList<IHolon>> Read(CosmicQuery<Holon> selection)
            {
                var plan = CosmicQueryPlanner.Plan(selection, ProviderType.RavenDBOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.RavenDBOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                return result.Result;
            }
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte", "Tea" }, (await Read(query)).Select(row => row.Name).ToArray());
            var coffee = seed.Ids.Take(2).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Espresso", "Latte" },
                (await Read(query.Where(row => coffee.Contains(row.Id)).Result)).Select(row => row.Name).ToArray());
            Assert.AreEqual(1, (await Read(query.Skip(1).Take(1).Result)).Count);
            CollectionAssert.AreEquivalent(before.ToArray(), (await Hashes()).ToArray());
            await VerifyCatalogue();
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
