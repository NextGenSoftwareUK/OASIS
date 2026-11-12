using CosmicCqlDemo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public sealed class CqlHostedReadSettingsTests
{
    private static Guid[] Ids() => new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

    [TestMethod]
    public void AdmissionFreezesOnlyNonSecretOwnedCatalogue()
    {
        var ids = Ids();
        var expected = ids.ToArray();
        var settings = new HostedCosmosReadSettings("https://owned-demo.documents.azure.com", "owned", Guid.NewGuid(), ids, true);
        ids[0] = Guid.NewGuid();
        CollectionAssert.AreEqual(expected, settings.ReadableIds.ToArray());
        Assert.AreEqual("owned-demo.documents.azure.com", settings.Endpoint.Host);
        Assert.ThrowsException<ArgumentException>(() => new HostedCosmosReadSettings(
            settings.Endpoint.ToString(), "owned", settings.Actor, expected, false));
    }

    [TestMethod]
    public void JsonParserRejectsSecretFieldsAndMissingConfirmation()
    {
        var ids = Ids();
        var actor = Guid.NewGuid();
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            Endpoint = "https://owned.documents.azure.com/", DatabaseName = "owned", Actor = actor,
            ReadableIds = ids, FreeAccountSpendingProtectionConfirmed = true
        });
        var settings = HostedCosmosReadSettings.FromJson(json);
        CollectionAssert.AreEqual(ids, settings.ReadableIds.ToArray());
        Assert.ThrowsException<System.Text.Json.JsonException>(() =>
            HostedCosmosReadSettings.FromJson(json[..^1] + ",\"AuthKey\":\"unused\"}"));
        Assert.ThrowsException<ArgumentException>(() => HostedCosmosReadSettings.FromJson(json.Replace(":true", ":false")));
        Assert.ThrowsException<ArgumentException>(() => HostedCosmosReadSettings.FromJson("null"));
    }

    [DataTestMethod]
    [DataRow("https://127.0.0.1:8081/")]
    [DataRow("https://localhost/")]
    [DataRow("http://owned.documents.azure.com/")]
    [DataRow("https://owned.documents.azure.com:8081/")]
    [DataRow("https://user:secret@owned.documents.azure.com/")]
    [DataRow("https://owned.documents.azure.com/?key=secret")]
    [DataRow("https://owned.documents.azure.com/#fragment")]
    [DataRow("https://owned.documents.azure.com/path")]
    [DataRow("https://owned.documents.azure.com.foreign.example/")]
    [DataRow("https://foreign.example/")]
    [DataRow("https://nested.owned.documents.azure.com/")]
    public void RejectsEmulatorAndUnenrolledEndpointsBeforeAnyClient(string endpoint)
        => Assert.ThrowsException<ArgumentException>(() => new HostedCosmosReadSettings(endpoint, "owned", Guid.NewGuid(), Ids(), true));

    [TestMethod]
    public void RejectsUnboundedUnownedOrAmbiguousConfiguration()
    {
        const string endpoint = "https://owned.documents.azure.com/";
        var actor = Guid.NewGuid();
        foreach (var name in new[] { "", " owned", "foreign/path", "secret?value" })
            Assert.ThrowsException<ArgumentException>(() => new HostedCosmosReadSettings(endpoint, name, actor, Ids(), true));
        Assert.ThrowsException<ArgumentException>(() => new HostedCosmosReadSettings(endpoint, "owned", Guid.Empty, Ids(), true));
        foreach (var ids in new[] { Array.Empty<Guid>(), Ids().Take(2).ToArray(), Enumerable.Range(0, 9).Select(_ => Guid.NewGuid()).ToArray(), new[] { actor, actor, actor }, new[] { actor, Guid.Empty, Guid.NewGuid() } })
            Assert.ThrowsException<ArgumentException>(() => new HostedCosmosReadSettings(endpoint, "owned", actor, ids, true));
    }
}
