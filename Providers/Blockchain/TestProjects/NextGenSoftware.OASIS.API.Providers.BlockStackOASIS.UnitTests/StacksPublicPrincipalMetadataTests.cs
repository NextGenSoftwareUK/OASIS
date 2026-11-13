using System.Net;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS.UnitTests;

public sealed class StacksPublicPrincipalMetadataTests
{
    private static readonly string[] Principals = { "SP000000000000000000002Q6VF78", "SP000000000000000000002Q6VF78.pox-4", "SP000000000000000000002Q6VF78.pox-5" };
    private const string Tip = "ccb149a1faca3a57f68fb0ed0b9474931c8036ba6d7f4eeffb9de61dd177b8b5";

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public int InfoCalls;
        public List<string> AccountCalls = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Content);
            Assert.Null(request.Headers.Authorization);
            Assert.False(request.Headers.Contains("Cookie"));
            Assert.Equal("", request.RequestUri!.Fragment);
            string text;
            bool denied = false;
            if (request.RequestUri.AbsolutePath == "/v2/info")
            {
                InfoCalls++;
                Assert.Equal("", request.RequestUri.Query);
                var info = new Dictionary<string, object> {
                    ["network_id"] = Fault == "network" ? 2147483648L : 1,
                    ["is_fully_synced"] = Fault != "unsynced", ["stacks_tip_height"] = 9394169,
                    ["stacks_tip"] = Fault == "tip" ? "latest" : "2440e05daa65501aa70503d61cc4f4b92e1c37770f74688bd8bab927bc8cdd5f",
                    ["stacks_tip_consensus_hash"] = Fault == "consensus" ? "0x" + new string('a',40) : "57172fc7713ec90f7c2742593a4087f66dea234e",
                    ["genesis_chainstate_hash"] = "74237aa39aa50a83de11a4f53e9d3bb7d43461d1de9873f402e5453ae60bc59b"
                };
                if (Fault == "height") info["stacks_tip_height"] = 1.5;
                text = JsonSerializer.Serialize(info);
                if (Fault == "duplicate-info") text = "{\"network_id\":1,\"network_id\":1}";
            }
            else
            {
                const string prefix = "/v2/accounts/";
                Assert.StartsWith(prefix, request.RequestUri.AbsolutePath);
                Assert.Equal("?proof=0&tip=" + Tip, request.RequestUri.Query);
                string principal = Uri.UnescapeDataString(request.RequestUri.AbsolutePath[prefix.Length..]);
                Assert.Contains(principal, Principals);
                AccountCalls.Add(principal);
                var account = new Dictionary<string, object?> {
                    ["balance"] = "0x" + new string('f', 32), ["locked"] = "0x" + new string('0', 32),
                    ["unlock_height"] = 0, ["nonce"] = ulong.MaxValue
                };
                if (Fault == "zero") account["balance"] = "0x" + new string('0', 32);
                if (Fault == "width") account["balance"] = "0x1";
                if (Fault == "case") account["balance"] = "0x" + new string('F', 32);
                if (Fault == "null") account["locked"] = null;
                if (Fault == "negative") account["nonce"] = -1;
                if (Fault == "fraction") account["nonce"] = 1.5;
                if (Fault == "overflow") account["nonce"] = JsonSerializer.Deserialize<JsonElement>("18446744073709551616");
                if (Fault == "missing") account.Remove("unlock_height");
                if (Fault == "proof") account["balance_proof"] = "0xdeadbeef";
                text = JsonSerializer.Serialize(account);
                if (Fault == "duplicate") text = "{\"nonce\":0,\"nonce\":1}";
                if (Fault == "oversize") text = new string(' ', 65537);
                denied = Fault == "http" || (Fault == "last" && principal == Principals[^1]);
            }
            return Task.FromResult(new HttpResponseMessage(denied ? HttpStatusCode.NotFound : HttpStatusCode.OK) {
                Content = new StringContent(text, Encoding.UTF8, "application/json")
            });
        }
    }

    [Theory]
    [InlineData("normal")] [InlineData("point")] [InlineData("subset")] [InlineData("window")] [InlineData("zero")]
    [InlineData("network")] [InlineData("unsynced")] [InlineData("tip")] [InlineData("consensus")] [InlineData("height")]
    [InlineData("duplicate-info")] [InlineData("width")] [InlineData("case")] [InlineData("null")]
    [InlineData("negative")] [InlineData("fraction")] [InlineData("overflow")] [InlineData("missing")]
    [InlineData("proof")] [InlineData("duplicate")] [InlineData("oversize")] [InlineData("http")]
    [InlineData("last")] [InlineData("empty-window-error")] [InlineData("budget")]
    [InlineData("unknown")] [InlineData("cancel")] [InlineData("inactive")]
    public async Task PublishesOnlyCompleteStatesAtOneValidExplicitTip(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        var owner = Guid.NewGuid();
        using var source = new StacksPublicPrincipalMetadataSource(client, new Uri("https://example.invalid/"), Principals, owner);
        if (scenario != "inactive") Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        native.InfoCalls = 0; native.AccountCalls.Clear();
        native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { "SP2J6ZY48GV1EZ5V2V5RB9MP66SW86PYKKNRV9EJ7" }
            : scenario == "subset" ? Principals.Take(2).ToArray() : Principals;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlockStackOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, StacksPublicPrincipalMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string principal = Principals[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlockStackOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, StacksPublicPrincipalMetadataSource.AddressMetadata) == principal).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.BlockStackOASIS,
            new[] { new CosmicQueryTarget(ProviderType.BlockStackOASIS, source.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancel") cancellation.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancellation.Token);
        if (scenario is "normal" or "point" or "subset" or "window" or "zero")
        {
            Assert.False(result.IsError, result.Exception?.ToString());
            int count = scenario is "point" or "window" ? 1 : scenario == "subset" ? 2 : 3;
            Assert.Equal(count, result.Result.Count);
            Assert.Equal(1, native.InfoCalls);
            Assert.Equal(scenario == "point" ? Principals.Take(1) : selected, native.AccountCalls);
            Assert.Equal(scenario == "window" ? Principals[1] : Principals[0], result.Result[0].Name);
            Assert.All(result.Result, row => {
                Assert.True(row.IsPublic); Assert.Equal(owner, row.CreatedByAvatarId);
                Assert.Equal(Tip, row.MetaData["StacksBlockId"]);
                Assert.Equal("node-reported-explicit-tip", row.MetaData["StacksObservationPolicy"]);
                Assert.Equal(scenario == "zero" ? "0" : "340282366920938463463374607431768211455", row.MetaData["StacksBalanceMicroStx"]);
                Assert.Equal("18446744073709551615", row.MetaData["StacksNonce"]);
            });
        }
        else { Assert.True(result.IsError); Assert.True(result.Result == null || result.Result.Count == 0); }
        if (scenario is "inactive" or "cancel" or "unknown") { Assert.Equal(0, native.InfoCalls); Assert.Empty(native.AccountCalls); }
        if (scenario is "network" or "unsynced" or "tip" or "consensus" or "height" or "duplicate-info")
            Assert.Empty(native.AccountCalls);
        if (scenario is "last" or "empty-window-error") Assert.Equal(3, native.AccountCalls.Count);
        if (scenario == "http") Assert.Single(native.AccountCalls); // Never retry or change tip.
    }

    [Fact]
    public async Task UnsupportedPredicatesAndWritesCannotSubmitNativeOperations()
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new StacksPublicPrincipalMetadataSource(client, new Uri("https://example.invalid/"), Principals, Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlockStackOASIS).Result).Where(row => row.Name == "anything").Result;
        Assert.True(CosmicQueryPlanner.Plan(query, ProviderType.BlockStackOASIS,
            new[] { new CosmicQueryTarget(ProviderType.BlockStackOASIS, source.QueryCapabilities) }).IsError);
        Assert.True((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.Equal(0, native.InfoCalls);
        Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        var loaded = await source.LoadHolonAsync(Guid.NewGuid(), loadChildren: false);
        Assert.True(loaded.IsError);
        // The fake transport rejects every non-GET method, including delete.
        Assert.True((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
    }

    [Theory]
    [InlineData("SP000000000000000000002Q6VF78")]
    [InlineData("SP2J6ZY48GV1EZ5V2V5RB9MP66SW86PYKKNRV9EJ7")]
    [InlineData("SM1Y6EXF21RZ9739DFTEQKB1H044BMM0XVCM4A4NY")]
    [InlineData("SP000000000000000000002Q6VF78.pox-4")]
    public void OfficialMainnetC32VectorsAreAcceptedWithoutNetwork(string principal)
    {
        var native = new Native(); using var client = new HttpClient(native);
        using var source = new StacksPublicPrincipalMetadataSource(client, new Uri("https://example.invalid/"), new[] { principal }, Guid.NewGuid());
        Assert.Equal(0, native.InfoCalls);
    }

    [Theory]
    [InlineData("checksum")] [InlineData("lowercase")] [InlineData("testnet")] [InlineData("contract")]
    [InlineData("owner")] [InlineData("duplicate")] [InlineData("authorization")] [InlineData("cookie")]
    [InlineData("url-credential")] [InlineData("secret-config")]
    public void InvalidConfigurationRejectsBeforeNativeRead(string scenario)
    {
        var native = new Native(); using var client = new HttpClient(native);
        if (scenario == "authorization") client.DefaultRequestHeaders.Authorization = new("Bearer", "inert-test-marker");
        if (scenario == "cookie") client.DefaultRequestHeaders.Add("Cookie", "inert-test-marker");
        var principals = scenario switch {
            "checksum" => new[] { "SP000000000000000000002Q6VF79" },
            "lowercase" => new[] { Principals[0].ToLowerInvariant() },
            "testnet" => new[] { "ST000000000000000000002AMW42H" },
            "contract" => new[] { Principals[0] + ".pox/escape" },
            "duplicate" => new[] { Principals[0], Principals[0] }, _ => Principals };
        if (scenario == "secret-config") Assert.Throws<ArgumentException>(() => StacksPublicPrincipalMetadataSource.FromJson("{\"password\":\"forbidden\"}"));
        else Assert.Throws<ArgumentException>(() => new StacksPublicPrincipalMetadataSource(client,
            new Uri(scenario == "url-credential" ? "https://user:secret@example.invalid/" : "https://example.invalid/"),
            principals, scenario == "owner" ? Guid.Empty : Guid.NewGuid()));
        Assert.Equal(0, native.InfoCalls);
    }
}
