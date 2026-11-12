using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class OpenSearchNativeDocumentLifecycleTests
{
    [TestMethod]
    public async Task HardDeleteRequiresTheNativeDocumentAcknowledgement()
    {
        const string origin = "http://127.0.0.1:19210";
        Assert.AreEqual("owned-ci-19210", Environment.GetEnvironmentVariable("OASIS_OPENSEARCH_DOCUMENT_FIXTURE"));
        using var client = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(15) };
        using var identity = JsonDocument.Parse(await client.GetStringAsync("/"));
        Assert.AreEqual("opensearch", identity.RootElement.GetProperty("version").GetProperty("distribution").GetString());
        Assert.AreEqual("3.9.0", identity.RootElement.GetProperty("version").GetProperty("number").GetString());
        using var empty = JsonDocument.Parse(await client.GetStringAsync("/_cat/indices/oasis_*?format=json"));
        Assert.AreEqual(0, empty.RootElement.GetArrayLength(), "The fixture must have no pre-existing OASIS indices.");

        var provider = new OpenSearchOASIS(client, origin);
        var activation = await provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.IsTrue(activation.Result);

        var holon = new Holon { Id = Guid.NewGuid(), Name = "owned OpenSearch fixture" };
        var saved = await provider.SaveHolonAsync(holon, saveChildren: false);
        Assert.IsFalse(saved.IsError, saved.Message);
        Assert.IsTrue(saved.IsSaved);
        var loaded = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(holon.Id, loaded.Result.Id);

        async Task SetWriteBlock(bool blocked)
        {
            using var response = await client.PutAsJsonAsync("/oasis_holons/_settings",
                new Dictionary<string, bool> { ["index.blocks.write"] = blocked });
            response.EnsureSuccessStatusCode();
        }

        await SetWriteBlock(true);
        try
        {
            var rejected = await provider.DeleteHolonSoftAsync(holon.Id, softDelete: false);
            Assert.IsTrue(rejected.IsError);
            Assert.IsFalse(rejected.Result);
            Assert.AreEqual(HttpStatusCode.Forbidden, (rejected.Exception as HttpRequestException)?.StatusCode);
            using var retained = await client.GetAsync($"/oasis_holons/_doc/{holon.Id:D}");
            Assert.AreEqual(HttpStatusCode.OK, retained.StatusCode);
        }
        finally
        {
            await SetWriteBlock(false);
        }

        var deleted = await provider.DeleteHolonSoftAsync(holon.Id, softDelete: false);
        Assert.IsFalse(deleted.IsError, deleted.Message);
        Assert.IsTrue(deleted.Result);
        using var absent = await client.GetAsync($"/oasis_holons/_doc/{holon.Id:D}");
        Assert.AreEqual(HttpStatusCode.NotFound, absent.StatusCode);
        var missing = await provider.LoadHolonAsync(holon.Id, loadChildren: false);
        Assert.IsNull(missing.Result);
        Assert.IsNull(missing.Exception, "An acknowledged missing document is distinct from a transport failure.");
        Assert.IsTrue((await provider.DeActivateProviderAsync()).Result);
    }
}
