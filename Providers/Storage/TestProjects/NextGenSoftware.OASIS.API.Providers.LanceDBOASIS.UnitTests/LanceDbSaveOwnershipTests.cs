using System.Net;
using System.Text;
using System.Text.Json;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS.UnitTests;

[TestClass]
public class LanceDbSaveOwnershipTests
{
    private sealed class Coffee : Holon { public string Category { get; set; } = "coffee"; }
    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSend = (_, _) => throw new InvalidOperationException();
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return OnSend(request, token); }
    }
    private static HttpResponseMessage Json(object value, HttpStatusCode code = HttpStatusCode.OK)
        => new(code) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    [DataTestMethod]
    [DataRow("insert")]
    [DataRow("update")]
    [DataRow("version")]
    [DataRow("count")]
    [DataRow("deleted")]
    [DataRow("missing")]
    [DataRow("http")]
    [DataRow("oversized")]
    [DataRow("disconnect")]
    public async Task RealArrowWireAndNativeCommitReceiptOwnPublication(string mode)
    {
        var holon = new Coffee { Name = "Espresso", MetaData = new() { ["precision"] = 1234567890.123456789012345678m } };
        var transport = new Transport { OnSend = async (request, token) =>
        {
            Assert.AreEqual(Guid.Empty, holon.Id);
            Assert.IsFalse(holon.ProviderUniqueStorageKey?.ContainsKey(ProviderType.LanceDBOASIS) == true);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/v1/table/oasis_holons/merge_insert/", request.RequestUri!.AbsolutePath);
            Assert.AreEqual("?on=id&when_matched_update_all=true&when_not_matched_insert_all=true&when_not_matched_by_source_delete=false", request.RequestUri.Query);
            Assert.AreEqual("application/vnd.apache.arrow.stream", request.Content!.Headers.ContentType!.MediaType);
            using var reader = new ArrowStreamReader(await request.Content.ReadAsStreamAsync(token));
            using var batch = await reader.ReadNextRecordBatchAsync(token);
            Assert.IsNotNull(batch);
            Assert.AreEqual(1, batch.Length);
            CollectionAssert.AreEqual(new[] { "id", "doc", "username", "email", "parent_holon_id", "vector" }, batch.Schema.FieldsList.Select(f => f.Name).ToArray());
            Assert.IsTrue(batch.Schema.FieldsList.All(f => !f.IsNullable));
            var id = Guid.Parse(((StringArray)batch.Column(0)).GetString(0));
            using var payload = JsonDocument.Parse(((StringArray)batch.Column(1)).GetString(0));
            Assert.AreEqual(id, payload.RootElement.GetProperty("Id").GetGuid());
            Assert.AreEqual("coffee", payload.RootElement.GetProperty("Category").GetString());
            Assert.AreEqual(1234567890.123456789012345678m, payload.RootElement.GetProperty("MetaData").GetProperty("precision").GetDecimal());
            Assert.AreEqual(id.ToString("D"), payload.RootElement.GetProperty("ProviderUniqueStorageKey").GetProperty("LanceDBOASIS").GetString());
            var vector = (FixedSizeListArray)batch.Column(5);
            Assert.AreEqual(1, ((FixedSizeListType)vector.Data.DataType).ListSize);
            Assert.AreEqual("item", ((FixedSizeListType)vector.Data.DataType).ValueField.Name);
            Assert.IsTrue(((FixedSizeListType)vector.Data.DataType).ValueField.IsNullable);
            Assert.AreEqual(1.0f, ((FloatArray)vector.Values).GetValue(0));
            Assert.IsNull(await reader.ReadNextRecordBatchAsync(token));
            if (mode == "disconnect") throw new HttpRequestException("Unknown write outcome");
            if (mode == "oversized") return Json(new { padding = new string('x', 65537) });
            if (mode == "missing") return Json(new { });
            return Json(new { version = mode == "version" ? 0UL : ulong.MaxValue, num_inserted_rows = mode == "update" ? 0 : mode == "count" ? 2 : 1,
                num_updated_rows = mode == "update" ? 1 : 0, num_deleted_rows = mode == "deleted" ? 1 : 0 },
                mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        } };
        using var http = new HttpClient(transport);
        var result = await new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project").SaveHolonAsync(holon, saveChildren: false);
        var success = mode is "insert" or "update";
        Assert.AreEqual(success, !result.IsError, result.Message);
        Assert.AreEqual(success, result.IsSaved);
        Assert.AreEqual(success ? 1 : 0, result.SavedCount);
        Assert.AreEqual(1, transport.Calls);
        if (success) Assert.AreEqual(holon.Id.ToString("D"), holon.ProviderUniqueStorageKey[ProviderType.LanceDBOASIS]);
        else { Assert.AreEqual(Guid.Empty, holon.Id); Assert.IsNull(result.Result); Assert.IsNotNull(result.Exception); }
    }

    [TestMethod]
    public async Task FailedExistingWritePreservesCallerKeyDictionary()
    {
        var id = Guid.NewGuid();
        var keys = new Dictionary<ProviderType, string> { [ProviderType.LanceDBOASIS] = "original", [ProviderType.MongoDBOASIS] = "other" };
        var holon = new Holon { Id = id, ProviderUniqueStorageKey = keys };
        var transport = new Transport { OnSend = (_, _) => Task.FromResult(Json(new { }, HttpStatusCode.BadRequest)) };
        using var http = new HttpClient(transport);
        var result = await new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project").SaveHolonAsync(holon, saveChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(id, holon.Id);
        Assert.AreSame(keys, holon.ProviderUniqueStorageKey);
        Assert.AreEqual("original", keys[ProviderType.LanceDBOASIS]);
        Assert.AreEqual("other", keys[ProviderType.MongoDBOASIS]);
    }

    [DataTestMethod]
    [DataRow("history")]
    [DataRow("size")]
    public async Task UnsupportedWritesRejectBeforeMutationOrNativeIO(string mode)
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project") { IsVersionControlEnabled = mode == "history" };
        var holon = new Holon { Name = mode == "size" ? new string('x', 1024 * 1024 + 1) : "coffee" };
        var result = await provider.SaveHolonAsync(holon, saveChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(Guid.Empty, holon.Id);
        Assert.AreEqual(0, transport.Calls);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task BatchReportsAcknowledgedWritesAndHonoursStop(bool keepGoing)
    {
        var count = 0;
        var transport = new Transport { OnSend = (_, _) => Task.FromResult(Json(new { version = 2, num_inserted_rows = 1, num_updated_rows = 0, num_deleted_rows = 0 },
            ++count == 1 ? HttpStatusCode.BadRequest : HttpStatusCode.OK)) };
        using var http = new HttpClient(transport);
        var rows = new[] { new Holon(), new Holon() };
        var result = await new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project").SaveHolonsAsync(rows, saveChildren: false, continueOnError: keepGoing);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(keepGoing ? 2 : 1, count);
        Assert.AreEqual(keepGoing ? 1 : 0, result.SavedCount);
        Assert.AreEqual(Guid.Empty, rows[0].Id);
    }
}
