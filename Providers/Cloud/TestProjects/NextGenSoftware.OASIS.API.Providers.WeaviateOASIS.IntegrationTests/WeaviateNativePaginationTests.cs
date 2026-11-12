using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class WeaviateNativePaginationTests
{
    [TestMethod]
    public async Task LoadsExactHolonIdentitiesAcrossNativeCursorPages()
    {
        if (Environment.GetEnvironmentVariable("OASIS_WEAVIATE_SHARED_FIXTURE") != "owned-ci-18080")
            Assert.Inconclusive("Requires the disposable Weaviate CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var mode = Environment.GetEnvironmentVariable("OASIS_WEAVIATE_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        const string origin = "http://127.0.0.1:18080";
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(20) };
        using var identity = JsonDocument.Parse(await client.GetStringAsync("/v1/meta"));
        Assert.AreEqual("1.39.8", identity.RootElement.GetProperty("version").GetString());
        using var schema = JsonDocument.Parse(await client.GetStringAsync("/v1/schema"));
        var classes = schema.RootElement.GetProperty("classes");
        Assert.IsTrue(classes.ValueKind == JsonValueKind.Null || classes.GetArrayLength() == 0);

        var provider = new WeaviateOASIS(client, origin);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsTrue(!activation.IsError && activation.Result, activation.Message);
        var owner = Guid.NewGuid();
        var records = Enumerable.Range(0, 101).Select(index => new Holon
        {
            Id = Guid.NewGuid(),
            Name = $"pagination-{index}",
            CreatedByAvatarId = owner
        }).ToArray();
        var attempted = new List<Guid>(records.Length);
        try
        {
            foreach (var record in records)
            {
                attempted.Add(record.Id);
                var saved = await provider.SaveHolonAsync(record, saveChildren: false);
                Assert.IsTrue(!saved.IsError && saved.IsSaved, saved.Message);
                Assert.AreEqual(1, saved.SavedCount);
            }

            Assert.AreEqual(records.Length, await NativeCountAsync(client));
            var loaded = mode == "sync"
                ? provider.LoadAllHolons(loadChildren: false)
                : await provider.LoadAllHolonsAsync(loadChildren: false);
            Assert.IsTrue(!loaded.IsError && loaded.IsLoaded, loaded.Message);
            var result = loaded.Result.ToArray();
            Assert.AreEqual(records.Length, loaded.LoadedCount);
            CollectionAssert.AreEquivalent(records.Select(record => record.Id).ToArray(),
                result.Select(record => record.Id).ToArray());
            Assert.IsTrue(result.All(record => record.CreatedByAvatarId == owner));
        }
        finally
        {
            var cleanupFailures = new List<Guid>();
            foreach (var id in attempted)
            {
                using var deleted = await client.DeleteAsync($"/v1/objects/OasisHolon/{id:D}");
                using var absent = await client.GetAsync($"/v1/objects/OasisHolon/{id:D}");
                if (deleted.StatusCode is not (HttpStatusCode.NoContent or HttpStatusCode.NotFound) ||
                    absent.StatusCode != HttpStatusCode.NotFound)
                    cleanupFailures.Add(id);
            }

            Assert.AreEqual(0, cleanupFailures.Count, $"Native cleanup failed for {string.Join(", ", cleanupFailures)}.");
            Assert.AreEqual(0, await NativeCountAsync(client));
            var deactivation = await provider.DeActivateProviderAsync();
            Assert.IsTrue(!deactivation.IsError && deactivation.Result, deactivation.Message);
            Console.WriteLine($"Verified exact cleanup for {attempted.Count} Weaviate pagination fixtures.");
        }
    }

    private static async Task<int> NativeCountAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/v1/graphql",
            new { query = "{ Aggregate { OasisHolon { meta { count } } } }" });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsFalse(document.RootElement.TryGetProperty("errors", out _), document.RootElement.GetRawText());
        return document.RootElement.GetProperty("data").GetProperty("Aggregate")
            .GetProperty("OasisHolon")[0].GetProperty("meta").GetProperty("count").GetInt32();
    }
}
