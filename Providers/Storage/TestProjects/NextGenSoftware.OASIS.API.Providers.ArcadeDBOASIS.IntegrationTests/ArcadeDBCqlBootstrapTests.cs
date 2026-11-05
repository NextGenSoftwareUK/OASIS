using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class ArcadeDBCqlBootstrapTests
{
    [TestMethod]
    [TestCategory("ArcadeDBCqlBootstrap")]
    public async Task ExplicitOwnedEmptyDatabaseGetsOneJournalledProviderBootstrap()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_ARCADEDB_CQL_SOURCE")!;
        var password = Environment.GetEnvironmentVariable("COSMIC_ARCADEDB_CQL_PASSWORD")!;
        Assert.IsFalse(string.IsNullOrWhiteSpace(path)); Assert.IsFalse(string.IsNullOrWhiteSpace(password));
        var raw = await File.ReadAllBytesAsync(path);
        var source = JsonSerializer.Deserialize<ArcadeDBCqlNativeTests.Source>(raw)!;
        Assert.AreEqual("http://127.0.0.1:24800", source.Endpoint);
        Assert.AreEqual(3,source.LiveIds.Length);
        Assert.AreEqual(5,source.LiveIds.Append(source.DeletedId).Append(source.MissingId).Distinct().Count());
        Assert.AreNotEqual(Guid.Empty,source.Actor);
        Assert.IsFalse(source.LiveIds.Append(source.DeletedId).Append(source.MissingId).Contains(Guid.Empty));
        var state = Path.GetDirectoryName(path)!;
        using var lease = new FileStream(Path.Combine(state,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        var provider = new ArcadeDBOASIS(source.Endpoint,"root",password,http);
        using (var exists = await http.GetAsync(source.Endpoint + "/api/v1/exists/oasis"))
        {
            exists.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await exists.Content.ReadAsStringAsync());
            Assert.IsFalse(body.RootElement.GetProperty("result").GetBoolean(),"Existing database requires reconciliation, never reseeding.");
        }
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(raw));
        async Task WriteOnce(string name, object value)
        {
            using var file = new FileStream(Path.Combine(state,name), FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(file,value); file.Flush(true);
        }
        await WriteOnce("bootstrap.intent.json",new { sourceSha256 = hash, database = "oasis", maximumSaves = 4 });
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError,active.Message);
        try
        {
            foreach (var id in source.LiveIds.Append(source.DeletedId))
            {
                var row = new Holon { Id = id, Name = "ArcadeDB CQL teaching " + id.ToString("N"),
                    CreatedByAvatarId = source.Actor, ParentHolonId = source.Actor, IsPublic = false, IsActive = true,
                    DeletedDate = id == source.DeletedId ? new DateTime(2026,11,5,0,0,0,DateTimeKind.Utc) : DateTime.MinValue };
                await WriteOnce("bootstrap-" + id.ToString("D") + ".intent.json",new { id, sourceSha256 = hash });
                var saved = await provider.SaveHolonAsync(row,saveChildren:false);
                Assert.IsFalse(saved.IsError,saved.Message);
                Assert.AreEqual(id,saved.Result.Id);
                await WriteOnce("bootstrap-" + id.ToString("D") + ".ack.json",new { id, sourceSha256 = hash });
            }
            await WriteOnce("bootstrap.ack.json",new { sourceSha256 = hash, savedRecords = 4 });
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
