using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests;

[TestClass]
public class PouchDbCqlBootstrapTests
{
    private static void Durable(string path, object value)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, value); stream.Flush(true);
    }

    [TestMethod]
    [TestCategory("PouchDbCqlBootstrap")]
    public async Task ExplicitEmptyOwnedSourceCreatesFourCanonicalRecordsOnce()
    {
        var root = Environment.GetEnvironmentVariable("COSMIC_POUCHDB_CQL_ROOT") ?? throw new InvalidOperationException("Explicit native runner required.");
        var path = Environment.GetEnvironmentVariable("COSMIC_POUCHDB_CQL_SOURCE")!;
        var bytes = await File.ReadAllBytesAsync(path);
        var source = JsonSerializer.Deserialize<PouchDBCqlNativeTests.Source>(bytes)!;
        Assert.AreEqual("http://127.0.0.1:19600", source.Endpoint);
        Assert.IsFalse(File.Exists(Path.Combine(root, "bootstrap.intent.json")), "Unknown original outcome cannot be replayed.");
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        using var absent = await http.GetAsync(source.Endpoint + "/oasis_holons");
        Assert.AreEqual(HttpStatusCode.NotFound, absent.StatusCode);
        Durable(Path.Combine(root, "bootstrap.intent.json"), new { sourceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)), records = 4 });
        using var database = await http.PutAsync(source.Endpoint + "/oasis_holons", null);
        Assert.AreEqual(HttpStatusCode.Created, database.StatusCode);
        using var dbAck = JsonDocument.Parse(await database.Content.ReadAsStringAsync()); Assert.IsTrue(dbAck.RootElement.GetProperty("ok").GetBoolean());
        foreach (var id in source.LiveIds.Append(source.DeletedId))
        {
            var row = new Holon { Id = id, Name = "PouchDB teaching " + id.ToString("N"), CreatedByAvatarId = source.Actor,
                DeletedDate = id == source.DeletedId ? DateTime.UtcNow : DateTime.MinValue };
            if (id == source.LiveIds[0]) row.Children = new[] { new Holon { Id = Guid.NewGuid(), Name = "Embedded native child" } };
            var payload = JsonSerializer.SerializeToNode(row)!.AsObject(); payload["_id"] = id.ToString("D");
            Durable(Path.Combine(root, id.ToString("N") + ".intent.json"), new { id, payloadSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload))) });
            using var response = await http.PutAsJsonAsync(source.Endpoint + "/oasis_holons/" + id.ToString("D"), payload);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            using var ack = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.IsTrue(ack.RootElement.GetProperty("ok").GetBoolean()); Assert.AreEqual(id.ToString("D"), ack.RootElement.GetProperty("id").GetString());
            var revision = ack.RootElement.GetProperty("rev").GetString(); Assert.IsTrue(revision!.StartsWith("1-", StringComparison.Ordinal));
            Durable(Path.Combine(root, id.ToString("N") + ".ack.json"), new { id, revision });
        }
        Durable(Path.Combine(root, "bootstrap.ack.json"), new { sourceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)), createdRecords = 4 });
    }
}
