using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS.UnitTests;

[TestClass]
public sealed class BlueSkyPublicProfileMetadataTests
{
    private static readonly string[] Actors = {
        "did:plc:z72i7hdynmk6r22z27h6tvur", "did:plc:ewvi7nxzyoun6zhxrhs64oiz", "did:plc:oky5czdrnfjpqslsw2a5iclo"
    };

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<string[]> Calls = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("/xrpc/app.bsky.actor.getProfiles", request.RequestUri!.AbsolutePath);
            Assert.IsNull(request.Headers.Authorization);
            Assert.IsNull(request.Content); // Never creates PDS sessions, posts or repository writes.
            var keys = request.RequestUri.Query.TrimStart('?').Split('&').Select(part => {
                Assert.IsTrue(part.StartsWith("actors=", StringComparison.Ordinal));
                return Uri.UnescapeDataString(part[7..]);
            }).ToArray();
            Assert.IsTrue(keys.All(Actors.Contains));
            Calls.Add(keys);
            var rows = new JsonArray();
            foreach (string key in keys.Reverse())
            {
                var profile = new JsonObject {
                    ["did"] = key, ["handle"] = "observed.example", ["displayName"] = "Public profile ☕",
                    ["description"] = "Public biography", ["followersCount"] = 9007199254740993L,
                    ["createdAt"] = "2023-01-01T12:13:14.123456Z", ["avatar"] = "http://127.0.0.1/private"
                };
                if (Fault == "optional")
                    foreach (string field in new[] { "displayName", "description", "followersCount", "createdAt" }) profile.Remove(field);
                if (Fault == "name-empty") profile["displayName"] = "";
                if (Fault == "identity" || (Fault == "last" && key == Actors[^1])) profile["did"] = "did:plc:" + new string('a', 24);
                if (Fault == "duplicate") profile["did"] = keys[0];
                if (Fault == "did") profile["did"] = "not-a-did";
                if (Fault == "handle") profile["handle"] = 42;
                if (Fault == "description") profile["description"] = new JsonObject();
                if (Fault == "negative") profile["followersCount"] = -1;
                if (Fault == "fraction") profile["followersCount"] = 1.5;
                if (Fault == "overflow") profile["followersCount"] = JsonNode.Parse("18446744073709551616");
                if (Fault == "zero") profile["followersCount"] = 0;
                if (Fault == "null") profile["followersCount"] = null;
                if (Fault == "date") profile["createdAt"] = "yesterday";
                if (Fault == "text-size") profile["displayName"] = new string('x', 4097);
                rows.Add(profile);
            }
            if (Fault == "missing") rows.RemoveAt(rows.Count - 1);
            string text = new JsonObject { ["profiles"] = rows }.ToJsonString();
            if (Fault == "error") text = "{\"error\":\"Unavailable\",\"profiles\":[]}";
            if (Fault == "duplicate-json") text = "{\"profiles\":[],\"profiles\":[]}";
            if (Fault == "oversize") text = new string(' ', 262145);
            return Task.FromResult(new HttpResponseMessage(Fault == "http" ? HttpStatusCode.Forbidden : HttpStatusCode.OK) {
                Content = new StringContent(text, Encoding.UTF8, "application/json")
            });
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("subset")] [DataRow("window")]
    [DataRow("optional")] [DataRow("name-empty")] [DataRow("zero")]
    [DataRow("identity")] [DataRow("last")] [DataRow("duplicate")] [DataRow("did")] [DataRow("handle")]
    [DataRow("description")] [DataRow("negative")] [DataRow("fraction")] [DataRow("overflow")]
    [DataRow("null")] [DataRow("date")] [DataRow("text-size")] [DataRow("missing")]
    [DataRow("error")] [DataRow("duplicate-json")] [DataRow("oversize")] [DataRow("http")]
    [DataRow("cancel")] [DataRow("budget")] [DataRow("unknown")] [DataRow("inactive")] [DataRow("empty-window-error")]
    public async Task PublishesOnlyCompleteValidatedDidSelections(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new BlueSkyPublicProfileMetadataSource(client, new Uri("https://example.invalid/"), Actors, Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear(); native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { "did:plc:" + new string('a', 24) }
            : scenario == "subset" ? Actors.Take(2).ToArray() : Actors;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlueSkyOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, BlueSkyPublicProfileMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string key = Actors[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlueSkyOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, BlueSkyPublicProfileMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.BlueSkyOASIS,
            new[] { new CosmicQueryTarget(ProviderType.BlueSkyOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "subset" or "window" or "optional" or "name-empty" or "zero")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario == "point" || scenario == "window" ? 1 : scenario == "subset" ? 2 : 3, result.Result.Count);
            Assert.AreEqual(scenario == "window" ? Actors[1] : Actors[0], result.Result[0].MetaData[BlueSkyPublicProfileMetadataSource.AddressMetadata]);
            Assert.AreEqual(1, native.Calls.Count, "One bounded native batch only, never per-profile discovery or image fetches.");
            Assert.IsTrue(result.Result.All(row => row.IsPublic && !row.MetaData.ContainsKey("avatar")));
            if (scenario == "optional")
            {
                Assert.AreEqual("observed.example", result.Result[0].Name);
                Assert.IsFalse(result.Result[0].MetaData.ContainsKey("BlueSkyFollowersCount"));
                Assert.IsFalse(result.Result[0].MetaData.ContainsKey("BlueSkyCreatedAt"));
                Assert.IsNull(result.Result[0].Description);
            }
            else
            {
                Assert.AreEqual(scenario == "zero" ? "0" : "9007199254740993", result.Result[0].MetaData["BlueSkyFollowersCount"]);
                Assert.AreEqual("2023-01-01T12:13:14.123456Z", result.Result[0].MetaData["BlueSkyCreatedAt"]);
                Assert.AreEqual(scenario == "name-empty" ? "observed.example" : "Public profile ☕", result.Result[0].Name);
            }
        }
        else
        {
            Assert.IsTrue(result.IsError, "Never publish partial source rows.");
            Assert.IsTrue(result.Result == null || result.Result.Count == 0);
        }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, native.Calls.Count);
    }

    [TestMethod]
    public async Task PublicProfileCannotSaveOrCreateSessions()
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new BlueSkyPublicProfileMetadataSource(client, new Uri("https://example.invalid/"), Actors, Guid.NewGuid());
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear();
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.AreEqual(0, native.Calls.Count);
    }

    [DataTestMethod]
    [DataRow("handle")] [DataRow("owner")] [DataRow("duplicate")] [DataRow("authorization")]
    [DataRow("cookie")] [DataRow("url-credential")] [DataRow("secret-config")]
    public void InvalidOrAuthenticatedConfigurationNeverReads(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        if (scenario == "authorization") client.DefaultRequestHeaders.Authorization = new("Bearer", "inert-test-marker");
        if (scenario == "cookie") client.DefaultRequestHeaders.Add("Cookie", "inert-test-marker");
        if (scenario == "secret-config") Assert.ThrowsException<ArgumentException>(() => BlueSkyPublicProfileMetadataSource.FromJson("{\"password\":\"forbidden\"}"));
        else Assert.ThrowsException<ArgumentException>(() => new BlueSkyPublicProfileMetadataSource(client,
            new Uri(scenario == "url-credential" ? "https://user:secret@example.invalid/" : "https://example.invalid/"),
            scenario == "handle" ? new[] { "bsky.app" } : scenario == "duplicate" ? new[] { Actors[0], Actors[0] } : Actors,
            scenario == "owner" ? Guid.Empty : Guid.NewGuid()));
        Assert.AreEqual(0, native.Calls.Count);
    }
}
