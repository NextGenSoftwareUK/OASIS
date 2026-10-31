using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.UnitTests;

[TestClass]
public class FirebaseOwnershipTests
{
    private sealed class Reply(Func<HttpRequestMessage, string?, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Sends;
        public string? Mutation;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Sends++;
            var text = request.Content == null ? null : await request.Content.ReadAsStringAsync(token);
            if (request.Method == HttpMethod.Put) Mutation = text;
            return reply(request, text);
        }
    }
    private static HttpResponseMessage Response(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [DataTestMethod][DataRow("http", false)][DataRow("malformed", false)][DataRow("null", false)]
    [DataRow("huge", false)][DataRow("identity", false)][DataRow("payload", false)][DataRow("http", true)]
    public async Task FailedWriteKeepsCallerIdentityAndKeyUntilNativeAcknowledgement(string fault, bool existing)
    {
        using var reply = new Reply((_, text) => {
            if (fault == "http") return Response("{}", HttpStatusCode.ServiceUnavailable);
            if (fault == "huge") return Response(new string(' ', 4 * 1024 * 1024 + 1));
            if (fault == "malformed") return Response("{");
            if (fault == "null") return Response("null");
            var node = JsonNode.Parse(text!)!;
            if (fault == "identity") node["id"] = Guid.NewGuid();
            else node["name"] = "not acknowledged";
            return Response(node.ToJsonString());
        });
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        var id = existing ? Guid.NewGuid() : Guid.Empty;
        var row = new Holon { Id = id, Name = "caller owned" };
        if (existing) row.ProviderUniqueStorageKey[ProviderType.FirebaseOASIS] = "original-key";
        var result = await provider.SaveHolonAsync(row, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.AreEqual(0, result.SavedCount);
        Assert.AreEqual(id, row.Id); Assert.AreEqual("caller owned", row.Name);
        if (existing) Assert.AreEqual("original-key", row.ProviderUniqueStorageKey[ProviderType.FirebaseOASIS]);
        else Assert.IsFalse(row.ProviderUniqueStorageKey?.ContainsKey(ProviderType.FirebaseOASIS) ?? false);
        Assert.IsInstanceOfType(result.Exception, typeof(INativeMutationReconciliationRequired));
        using var dispatched = JsonDocument.Parse(reply.Mutation!);
        Assert.AreEqual(dispatched.RootElement.GetProperty("id").GetGuid(), result.Exception.GetType().GetProperty("ProposedId")!.GetValue(result.Exception));
        if (fault == "http") Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ((HttpRequestException)result.Exception.InnerException!).StatusCode);
    }

    [TestMethod]
    public async Task ExplicitNamespaceIsEncodedOnceOnBothReadAndWriteRoutes()
    {
        using var reply = new Reply((request, text) => {
            var parts = request.RequestUri!.Query.TrimStart('?').Split('&').Select(value => value.Split('=', 2))
                .ToDictionary(value => value[0], value => Uri.UnescapeDataString(value[1]));
            Assert.AreEqual("cosmic demo&one", parts["ns"]);
            Assert.AreEqual("synthetic+token", parts["auth"]);
            return Response(request.Method == HttpMethod.Put ? text! : "null");
        });
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", "synthetic+token", http, databaseNamespace: "cosmic demo&one");
        Assert.IsFalse((await provider.SaveHolonAsync(new Holon { Id = Guid.Empty }, saveChildren: false)).IsError);
        Assert.IsFalse((await provider.LoadAllHolonsAsync(loadChildren: false)).IsError);
        Assert.AreEqual(2, reply.Sends);
    }

    private sealed class TypedHolon : Holon
    {
        public decimal Amount { get; set; }
        public long Counter { get; set; }
        public string? Optional { get; set; }
    }
    [TestMethod]
    public async Task SuccessfulSaveUsesDeclaredNativeProjectionAndPublishesAcknowledgedId()
    {
        using var reply = new Reply((_, text) => Response(text!));
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        var row = new TypedHolon { Id = Guid.Empty, Name = "typed", Amount = 123.45m, Counter = 42 };
        var result = await provider.SaveHolonAsync(row, saveChildren: false);
        Assert.IsFalse(result.IsError, result.Exception?.ToString()); Assert.IsTrue(result.IsSaved); Assert.AreEqual(1, result.SavedCount);
        Assert.AreNotEqual(Guid.Empty, row.Id); Assert.AreEqual(row.Id.ToString("D"), row.ProviderUniqueStorageKey[ProviderType.FirebaseOASIS]);
        using var json = JsonDocument.Parse(reply.Mutation!);
        Assert.AreEqual(123.45m, json.RootElement.GetProperty("amount").GetDecimal());
        Assert.AreEqual(42L, json.RootElement.GetProperty("counter").GetInt64());
        Assert.IsFalse(json.RootElement.TryGetProperty("optional", out _));
        Assert.IsFalse(json.RootElement.TryGetProperty("children", out _));
        Assert.IsNull(row.Optional);
    }

    [DataTestMethod][DataRow(true)][DataRow(false)]
    public async Task UnsupportedNativeNumberFailsBeforeDispatchAndCallerMutation(bool integer)
    {
        using var reply = new Reply((_, _) => throw new AssertFailedException("Unsafe representation must not dispatch."));
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        var row = new TypedHolon { Id = Guid.Empty, Amount = integer ? 1m : 1234567890123456789012345678.1m,
            Counter = integer ? 9007199254740993L : 1L };
        var result = await provider.SaveHolonAsync(row, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.AreEqual("FIREBASE_REPRESENTATION_UNSUPPORTED", result.ErrorCode);
        Assert.AreEqual(0, reply.Sends); Assert.AreEqual(Guid.Empty, row.Id);
        Assert.IsFalse(result.Exception is INativeMutationReconciliationRequired);
    }

    [DataTestMethod][DataRow("point")][DataRow("all")][DataRow("parent")]
    public async Task EveryHolonReadRejectsNativeKeyPayloadDisagreement(string operation)
    {
        var requested = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new { id = foreign, name = "foreign" });
        using var reply = new Reply((_, _) => Response(operation == "point" ? json : "{\"" + requested.ToString("D") + "\":" + json + "}"));
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        if (operation == "point") Assert.IsTrue((await provider.LoadHolonAsync(requested, loadChildren: false)).IsError);
        else if (operation == "all") Assert.IsTrue((await provider.LoadAllHolonsAsync(loadChildren: false)).IsError);
        else Assert.IsTrue((await provider.LoadHolonsForParentAsync(requested, loadChildren: false)).IsError);
    }

    [DataTestMethod][DataRow(true)][DataRow(false)]
    public async Task SoftDeletePersistsCanonicalDateAndChecksTheSameSaveAcknowledgement(bool reject)
    {
        var id = Guid.NewGuid();
        using var reply = new Reply((request, text) => request.Method == HttpMethod.Get
            ? Response(JsonSerializer.Serialize(new { id, name = "owned" }))
            : reject ? Response("{}", HttpStatusCode.ServiceUnavailable) : Response(text!));
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        var result = await provider.DeleteHolonAsync(id);
        Assert.AreEqual(reject, result.IsError);
        if (reject) Assert.IsInstanceOfType(result.Exception, typeof(INativeMutationReconciliationRequired));
        else { Assert.IsTrue(result.IsSaved); Assert.IsTrue(result.Result.IsDeleted); }
        using var json = JsonDocument.Parse(reply.Mutation!);
        Assert.AreNotEqual(DateTime.MinValue, json.RootElement.GetProperty("deletedDate").GetDateTime());
    }
}
