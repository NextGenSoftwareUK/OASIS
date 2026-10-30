using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests;

[TestClass]
public class ChromaSaveOwnershipTests
{
    private sealed class Coffee : Holon { public string Category { get; set; } = "coffee"; }

    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage, Task<HttpResponseMessage>> Send = _ => throw new InvalidOperationException();
        public int Writes;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/collections"))
                return Task.FromResult(Json(new { id = "3bbf531e-0f7d-4e8a-9306-c901ae2c7880" }));
            Writes++;
            return Send(request);
        }
    }

    private static HttpResponseMessage Json(object value, HttpStatusCode code = HttpStatusCode.OK)
        => new(code) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("http")]
    [DataRow("disconnect")]
    public async Task WireIdentityAndDerivedDataAreStagedUntilAcknowledgement(string mode)
    {
        var holon = new Coffee { Name = "Espresso", MetaData = new() { ["precision"] = 1234567890.123456789012345678m } };
        var transport = new Transport { Send = async request =>
        {
            Assert.AreEqual(Guid.Empty, holon.Id);
            Assert.IsFalse(holon.ProviderUniqueStorageKey?.ContainsKey(ProviderType.ChromaOASIS) == true);
            Assert.IsTrue(request.RequestUri!.AbsolutePath.EndsWith("/upsert"));
            using var wire = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var id = wire.RootElement.GetProperty("ids")[0].GetGuid();
            using var payload = JsonDocument.Parse(wire.RootElement.GetProperty("documents")[0].GetString()!);
            Assert.AreEqual(id, payload.RootElement.GetProperty("Id").GetGuid());
            Assert.AreEqual("coffee", payload.RootElement.GetProperty("Category").GetString());
            Assert.AreEqual(1234567890.123456789012345678m, payload.RootElement.GetProperty("MetaData").GetProperty("precision").GetDecimal());
            Assert.AreEqual(id.ToString("D"), payload.RootElement.GetProperty("ProviderUniqueStorageKey").GetProperty("ChromaOASIS").GetString());
            if (mode == "disconnect") throw new HttpRequestException("Unknown write outcome");
            return Json(new { }, mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        } };
        using var http = new HttpClient(transport);
        var result = await new ChromaOASIS(http).SaveHolonAsync(holon, saveChildren: false);
        Assert.AreEqual(mode == "success", !result.IsError, result.Message);
        Assert.AreEqual(mode == "success", result.IsSaved);
        Assert.AreEqual(mode == "success" ? 1 : 0, result.SavedCount);
        Assert.AreEqual(1, transport.Writes, "Unknown outcomes must never cause replay.");
        if (mode == "success") Assert.AreEqual(holon.Id.ToString("D"), holon.ProviderUniqueStorageKey[ProviderType.ChromaOASIS]);
        else { Assert.AreEqual(Guid.Empty, holon.Id); Assert.IsNull(result.Result); Assert.IsNotNull(result.Exception); }
    }

    [TestMethod]
    public async Task ExistingCallerKeysRemainUntouchedOnFailure()
    {
        var keys = new Dictionary<ProviderType, string> { [ProviderType.ChromaOASIS] = "original", [ProviderType.MongoDBOASIS] = "other" };
        var id = Guid.NewGuid();
        var holon = new Holon { Id = id, ProviderUniqueStorageKey = keys };
        var transport = new Transport { Send = _ => Task.FromResult(Json(new { }, HttpStatusCode.BadRequest)) };
        using var http = new HttpClient(transport);
        var result = await new ChromaOASIS(http).SaveHolonAsync(holon, saveChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.AreSame(keys, holon.ProviderUniqueStorageKey);
        Assert.AreEqual(id, holon.Id);
        Assert.AreEqual("original", keys[ProviderType.ChromaOASIS]);
        Assert.AreEqual("other", keys[ProviderType.MongoDBOASIS]);
    }

    [TestMethod]
    public async Task UnimplementedHistoryRejectsBeforeIdentityAllocationOrIO()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var holon = new Holon();
        var result = await new ChromaOASIS(http) { IsVersionControlEnabled = true }.SaveHolonAsync(holon, saveChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(Guid.Empty, holon.Id);
        Assert.AreEqual(0, transport.Writes);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task BatchReportsPartialWritesAndHonoursStop(bool keepGoing)
    {
        var count = 0;
        var transport = new Transport { Send = _ => Task.FromResult(Json(new { }, ++count == 1 ? HttpStatusCode.BadRequest : HttpStatusCode.OK)) };
        using var http = new HttpClient(transport);
        var rows = new[] { new Holon(), new Holon() };
        var result = await new ChromaOASIS(http).SaveHolonsAsync(rows, saveChildren: false, continueOnError: keepGoing);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(keepGoing ? 2 : 1, count);
        Assert.AreEqual(keepGoing ? 1 : 0, result.SavedCount);
        Assert.AreEqual(keepGoing, result.IsSaved);
        Assert.AreEqual(Guid.Empty, rows[0].Id);
    }
}
