using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS.UnitTests;

[TestClass]
public class LanceDbNativeQueryTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSend = (_, _) => throw new InvalidOperationException();
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return OnSend(request, token); }
    }
    private static CosmicQueryPlan<Holon> Plan(LanceDBOASIS provider, Guid actor, Guid[] ids, bool paging = false)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.LanceDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.LanceDBOASIS, ids).Result).Result;
        if (paging) query = query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query, ProviderType.LanceDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.LanceDBOASIS, provider.QueryCapabilities) }).Result;
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("paging")]
    [DataRow("foreign")]
    [DataRow("payload")]
    [DataRow("duplicate")]
    [DataRow("columns")]
    [DataRow("type")]
    [DataRow("null")]
    [DataRow("http")]
    [DataRow("media")]
    [DataRow("envelope")]
    [DataRow("docsize")]
    [DataRow("empty")]
    [DataRow("budget")]
    [DataRow("deleted")]
    [DataRow("truncated")]
    [DataRow("cancel")]
    [DataRow("overflow")]
    public async Task RealArrowFilesValidateAllBatchesAndAuthorityBeforePublication(string mode)
    {
        var actor = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        using var cancellation = new CancellationTokenSource();
        var transport = new Transport { OnSend = async (request, token) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/v1/table/oasis_holons/query/", request.RequestUri!.AbsolutePath);
            Assert.AreEqual("application/vnd.apache.arrow.file", request.Headers.Accept.Single().MediaType);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("id IN (" + string.Join(",", ids.Select(id => "'" + id.ToString("D") + "'")) + ")", body.RootElement.GetProperty("filter").GetString());
            Assert.AreEqual(3, body.RootElement.GetProperty("k").GetInt32());
            Assert.AreEqual(0, body.RootElement.GetProperty("offset").GetInt32());
            Assert.IsFalse(body.RootElement.TryGetProperty("vector", out _), "Exact key retrieval is not similarity search.");
            CollectionAssert.AreEqual(new[] { "id", "doc" }, body.RootElement.GetProperty("columns").EnumerateArray().Select(v => v.GetString()).ToArray());
            var schema = new Schema.Builder().Field(new Field("id", StringType.Default, false))
                .Field(new Field(mode == "columns" ? "other" : "doc", mode == "type" ? (IArrowType)Int32Type.Default : StringType.Default, mode == "null")).Build();
            using var encoded = new MemoryStream();
            using (var writer = new ArrowFileWriter(encoded, schema, leaveOpen: true))
            {
                foreach (var id in mode == "empty" ? new[] { Guid.Empty } : mode == "overflow" ? new[] { ids[0], ids[1], Guid.NewGuid() } : new[] { ids[1], mode == "duplicate" ? ids[1] : mode == "foreign" ? Guid.NewGuid() : ids[0] })
                {
                    var keys = new StringArray.Builder();
                    var docs = new StringArray.Builder();
                    if (mode != "empty")
                    {
                        keys.Append(id.ToString("D"));
                        if (mode == "null") docs.AppendNull();
                        else docs.Append(mode == "docsize" ? new string('x', 1024 * 1024 + 1) : JsonSerializer.Serialize(new Holon
                        { Id = mode == "payload" ? Guid.NewGuid() : id, CreatedByAvatarId = actor, DeletedDate = mode == "deleted" ? DateTime.UtcNow : default }));
                    }
                    using var batch = new RecordBatch(schema, new IArrowArray[] { keys.Build(), mode == "type" ? new Int32Array.Builder().Append(1).Build() : docs.Build() }, mode == "empty" ? 0 : 1);
                    await writer.WriteRecordBatchAsync(batch, token);
                }
                await writer.WriteEndAsync(token);
            }
            var bytes = encoded.ToArray();
            if (mode == "truncated") bytes = bytes[..^8];
            if (mode == "envelope") bytes = new byte[4 * 1024 * 1024 + 1];
            if (mode == "cancel") { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
            var response = new HttpResponseMessage(mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mode == "media" ? "application/json" : "application/vnd.apache.arrow.file");
            return response;
        } };
        using var http = new HttpClient(transport);
        var provider = new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project") { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, ids, mode == "paging"),
            maximumResults: mode == "budget" ? 1 : 100, cancellationToken: cancellation.Token);
        var success = mode is "success" or "paging" or "empty" or "deleted";
        Assert.AreEqual(success, !result.IsError, result.Message + result.Exception);
        Assert.AreEqual(1, transport.Calls);
        if (mode == "success") CollectionAssert.AreEqual(ids, result.Result.Select(h => h.Id).ToArray());
        else if (mode == "paging") Assert.AreEqual(ids[1], result.Result.Single().Id);
        else if (success) Assert.AreEqual(0, result.Result.Count);
        else { Assert.IsNull(result.Result); Assert.IsFalse(result.IsLoaded); }
        if (mode == "cancel") Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("payload")]
    [DataRow("truncated")]
    public async Task ExistingHolonLookupUsesArrowAndChecksPayloadIdentity(string mode)
    {
        var id = Guid.NewGuid();
        var schema = new Schema.Builder().Field(new Field("id", StringType.Default, false)).Field(new Field("doc", StringType.Default, false)).Build();
        using var bytes = new MemoryStream();
        using (var writer = new ArrowFileWriter(bytes, schema, leaveOpen: true))
        {
            using var batch = new RecordBatch(schema, new IArrowArray[]
            {
                new StringArray.Builder().Append(id.ToString("D")).Build(),
                new StringArray.Builder().Append(JsonSerializer.Serialize(new Holon { Id = mode == "payload" ? Guid.NewGuid() : id, Name = "native coffee" })).Build()
            }, 1);
            await writer.WriteRecordBatchAsync(batch);
            await writer.WriteEndAsync();
        }
        var transport = new Transport { OnSend = async (request, token) =>
        {
            using var requestBody = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("id = '" + id.ToString("D") + "'", requestBody.RootElement.GetProperty("filter").GetString());
            Assert.AreEqual(2, requestBody.RootElement.GetProperty("k").GetInt32());
            var encoded = bytes.ToArray();
            if (mode == "truncated") encoded = encoded[..^8];
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(encoded) };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.apache.arrow.file");
            return response;
        } };
        using var http = new HttpClient(transport);
        var result = await new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project").LoadHolonAsync(id, loadChildren: false);
        Assert.AreEqual(mode == "success", !result.IsError, result.Message);
        if (mode == "success") Assert.AreEqual("native coffee", result.Result.Name);
        else Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task MetadataLookupEscapesSqlLiteralsAndSurfacesNativeErrors()
    {
        var transport = new Transport { OnSend = async (request, token) =>
        {
            Assert.AreEqual("/v1/table/oasis_avatars/query/", request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("email = 'o''hara@example.test'", body.RootElement.GetProperty("filter").GetString());
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("offline") };
        } };
        using var http = new HttpClient(transport);
        var result = await new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project").LoadAvatarByEmailAsync("o'hara@example.test");
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.AreEqual(1, transport.Calls);
    }

    [TestMethod]
    public async Task EmptyCatalogueDoesNotQueryNativeStorage()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new LanceDBOASIS(http, "http://native.invalid", "test-key", "test-project") { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Guid.NewGuid(), System.Array.Empty<Guid>()));
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(0, transport.Calls);
    }
}
