using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MatrixOASIS.UnitTests;

[TestClass]
public sealed class MatrixPublicRoomMetadataTests
{
    private static readonly string[] Aliases = { "#matrix:matrix.org", "#synapse:matrix.org", "#element:matrix.org" };

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<string> Calls = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.IsNull(request.Content);
            Assert.IsNull(request.Headers.Authorization);
            Assert.IsFalse(request.Headers.Contains("Cookie"));
            Assert.AreEqual("", request.RequestUri!.Fragment);
            Assert.AreEqual("", request.RequestUri.Query);
            const string prefix = "/_matrix/client/v3/directory/room/";
            Assert.IsTrue(request.RequestUri.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal));
            string alias = Uri.UnescapeDataString(request.RequestUri.AbsolutePath[prefix.Length..]);
            Assert.IsTrue(Aliases.Contains(alias));
            Calls.Add(alias);
            string room = Fault == "old-id" ? "!opaque:matrix.org" : "!opaque_" + Array.IndexOf(Aliases, alias);
            string[] servers = Fault == "large-list" ? Enumerable.Repeat("matrix.org", 1169).ToArray() : new[] { "matrix.org", "example.org" };
            if (Fault == "empty-servers") servers = Array.Empty<string>();
            if (Fault == "server-control") servers = new[] { "bad\nserver" };
            if (Fault == "server-limit") servers = Enumerable.Repeat("matrix.org", 4097).ToArray();
            if (Fault == "room-id") room = "wrong";
            if (Fault == "room-size") room = "!" + new string('x', 255);
            string text = JsonSerializer.Serialize(new { room_id = room, servers });
            if (Fault == "missing") text = "{\"servers\":[\"matrix.org\"]}";
            if (Fault == "error") text = "{\"errcode\":\"M_NOT_FOUND\"}";
            if (Fault == "duplicate") text = "{\"room_id\":\"!a\",\"room_id\":\"!b\",\"servers\":[\"matrix.org\"]}";
            if (Fault == "oversize") text = new string(' ', 65537);
            bool denied = Fault == "http" || (Fault == "last" && alias == Aliases[^1]);
            return Task.FromResult(new HttpResponseMessage(denied ? HttpStatusCode.NotFound : HttpStatusCode.OK) {
                Content = new StringContent(text, Encoding.UTF8, "application/json")
            });
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("subset")] [DataRow("window")]
    [DataRow("old-id")] [DataRow("large-list")]
    [DataRow("empty-servers")] [DataRow("server-control")] [DataRow("server-limit")]
    [DataRow("room-id")] [DataRow("room-size")] [DataRow("missing")]
    [DataRow("error")] [DataRow("duplicate")] [DataRow("oversize")] [DataRow("http")]
    [DataRow("last")] [DataRow("empty-window-error")] [DataRow("budget")]
    [DataRow("unknown")] [DataRow("cancel")] [DataRow("inactive")]
    public async Task CompleteAliasSelectionBeforePaging(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new MatrixPublicRoomMetadataSource(client, new Uri("https://example.invalid/"), Aliases, Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear();
        native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { "#unknown:matrix.org" }
            : scenario == "subset" ? Aliases.Take(2).ToArray() : Aliases;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MatrixOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, MatrixPublicRoomMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string alias = Aliases[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MatrixOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, MatrixPublicRoomMetadataSource.AddressMetadata) == alias).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.MatrixOASIS,
            new[] { new CosmicQueryTarget(ProviderType.MatrixOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancel") cancellation.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancellation.Token);
        if (scenario is "normal" or "point" or "subset" or "window" or "old-id" or "large-list")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            int expected = scenario is "point" or "window" ? 1 : scenario == "subset" ? 2 : 3;
            Assert.AreEqual(expected, result.Result.Count);
            Assert.AreEqual(scenario == "point" ? 1 : selected.Length, native.Calls.Count);
            Assert.AreEqual(scenario == "window" ? Aliases[1] : Aliases[0], result.Result[0].Name);
            Assert.IsTrue(result.Result.All(row => row.IsPublic && row.MetaData["MatrixObservationPolicy"].ToString() == "server-reported-alias-resolution"));
            Assert.IsTrue(result.Result[0].MetaData["MatrixRoomId"].ToString()!.StartsWith('!'));
            using var servers = JsonDocument.Parse(result.Result[0].MetaData["MatrixServersJson"].ToString()!);
            Assert.AreEqual(scenario == "large-list" ? 1169 : 2, servers.RootElement.GetArrayLength());
        }
        else
        {
            Assert.IsTrue(result.IsError, "No incomplete rows or manufactured empty success.");
            Assert.IsTrue(result.Result == null || result.Result.Count == 0);
        }
        if (scenario is "unknown" or "cancel" or "inactive") Assert.AreEqual(0, native.Calls.Count);
        if (scenario == "http") Assert.AreEqual(1, native.Calls.Count, "No retry or alternate endpoint.");
        if (scenario is "last" or "empty-window-error") Assert.AreEqual(3, native.Calls.Count);
    }

    [TestMethod]
    public async Task UnsupportedPredicateAndNativeWritesNeverReachDirectory()
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new MatrixPublicRoomMetadataSource(client, new Uri("https://example.invalid/"), Aliases, Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MatrixOASIS).Result).Where(row => row.Name == "room").Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.MatrixOASIS,
            new[] { new CosmicQueryTarget(ProviderType.MatrixOASIS, source.QueryCapabilities) });
        Assert.IsTrue(plan.IsError);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.AreEqual(0, native.Calls.Count);
    }

    [DataTestMethod]
    [DataRow("alias")] [DataRow("owner")] [DataRow("duplicate")] [DataRow("authorization")]
    [DataRow("cookie")] [DataRow("url-credential")] [DataRow("secret-config")]
    public void InvalidConfigurationCannotCreateAnAuthenticatedRead(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        if (scenario == "authorization") client.DefaultRequestHeaders.Authorization = new("Bearer", "inert-test-marker");
        if (scenario == "cookie") client.DefaultRequestHeaders.Add("Cookie", "inert-test-marker");
        if (scenario == "secret-config") Assert.ThrowsException<ArgumentException>(() => MatrixPublicRoomMetadataSource.FromJson("{\"password\":\"forbidden\"}"));
        else Assert.ThrowsException<ArgumentException>(() => new MatrixPublicRoomMetadataSource(client,
            new Uri(scenario == "url-credential" ? "https://user:secret@example.invalid/" : "https://example.invalid/"),
            scenario == "alias" ? new[] { "not-an-alias" } : scenario == "duplicate" ? new[] { Aliases[0], Aliases[0] } : Aliases,
            scenario == "owner" ? Guid.Empty : Guid.NewGuid()));
        Assert.AreEqual(0, native.Calls.Count);
    }
}
