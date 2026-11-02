using System.Net;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.BlockscoutOASIS;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class BlockscoutPublicAddressMetadataTests
{
    private static readonly string[] Addresses = { "0x" + new string('1',40), "0x" + new string('2',40), "0x" + new string('3',40) };

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<string> Calls = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.Equal(HttpMethod.Get, request.Method); Assert.Null(request.Content);
            Assert.Null(request.Headers.Authorization); Assert.False(request.Headers.Contains("Cookie"));
            Assert.False(request.Headers.Contains("x-api-key"));
            Assert.Empty(request.RequestUri!.Query); Assert.Empty(request.RequestUri.Fragment);
            const string prefix = "/api/v2/addresses/";
            Assert.StartsWith(prefix, request.RequestUri.AbsolutePath);
            string address = request.RequestUri.AbsolutePath[prefix.Length..];
            Assert.Contains(address, Addresses); Calls.Add(address);
            var row = new Dictionary<string, object?>
            {
                ["hash"] = Fault == "identity" ? "0x" + new string('9',40) : address,
                ["is_contract"] = true,
                ["coin_balance"] = new string('9',78),
                ["block_number_balance_updated_at"] = ulong.MaxValue,
                ["name"] = "Indexed contract", ["private_tags"] = new[] { "inert-secret" },
                ["watchlist_names"] = new[] { "inert-secret" },
                ["token"] = new { name = "Never copied" }, ["metadata"] = new { secret = "inert-secret" }
            };
            if (Fault == "nullable") { row["coin_balance"] = null; row["block_number_balance_updated_at"] = null; row["name"] = null; }
            if (Fault == "balance-number") row["coin_balance"] = 123;
            if (Fault == "balance-width") row["coin_balance"] = new string('9',79);
            if (Fault == "balance-canonical") row["coin_balance"] = "01";
            if (Fault == "balance-newline") row["coin_balance"] = "1\n";
            if (Fault == "balance-negative") row["coin_balance"] = "-1";
            if (Fault == "balance-fraction") row["coin_balance"] = "1.5";
            if (Fault == "block-string") row["block_number_balance_updated_at"] = "123";
            if (Fault == "block-fraction") row["block_number_balance_updated_at"] = 1.5;
            if (Fault == "block-negative") row["block_number_balance_updated_at"] = -1;
            if (Fault == "block-overflow") row["block_number_balance_updated_at"] = JsonSerializer.Deserialize<JsonElement>("18446744073709551616");
            if (Fault == "contract") row["is_contract"] = "true";
            if (Fault == "name-type") row["name"] = 123;
            if (Fault == "name-control") row["name"] = "bad\nname";
            if (Fault == "name-width") row["name"] = new string('a',257);
            if (Fault == "missing") row.Remove("coin_balance");
            if (Fault == "error") row["error"] = false;
            string text = JsonSerializer.Serialize(row);
            if (Fault == "duplicate") text = "{\"hash\":\"x\",\"hash\":\"y\"}";
            if (Fault == "oversize") text = new string(' ',1048577);
            bool denied = Fault == "http" || Fault == "last" && address == Addresses.Last();
            return Task.FromResult(new HttpResponseMessage(denied ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            {
                Content = new StringContent(text, Encoding.UTF8, "application/json")
            });
        }
    }

    [Theory]
    [InlineData("normal")] [InlineData("point")] [InlineData("subset")] [InlineData("window")] [InlineData("nullable")]
    [InlineData("identity")] [InlineData("balance-number")] [InlineData("balance-width")] [InlineData("balance-canonical")]
    [InlineData("balance-newline")] [InlineData("balance-negative")] [InlineData("balance-fraction")]
    [InlineData("block-string")] [InlineData("block-fraction")] [InlineData("block-negative")] [InlineData("block-overflow")]
    [InlineData("contract")] [InlineData("name-type")] [InlineData("name-control")] [InlineData("name-width")]
    [InlineData("missing")] [InlineData("error")] [InlineData("duplicate")] [InlineData("oversize")]
    [InlineData("http")] [InlineData("last")] [InlineData("empty-window-error")]
    [InlineData("unknown")] [InlineData("cancel")] [InlineData("inactive")] [InlineData("budget")]
    public async Task CompleteAddressSelectionPreservesPrecisionAbsenceAndPrivacyOrFails(string scenario)
    {
        var native = new Native(); using var client = new HttpClient(native); var owner = Guid.NewGuid();
        using var source = new BlockscoutPublicAddressMetadataSource(client, new Uri("https://example.invalid/api/v2/"), Addresses, owner);
        if (scenario != "inactive") Assert.False((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear(); native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var keys = scenario == "subset" ? Addresses.Take(2).ToArray() : scenario == "unknown" ? new[] { "0x"+new string('9',40) } : Addresses;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlockscoutOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, BlockscoutPublicAddressMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            string key = Addresses[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlockscoutOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, BlockscoutPublicAddressMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.BlockscoutOASIS,
            new[] { new CosmicQueryTarget(ProviderType.BlockscoutOASIS, source.QueryCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        using var cancelled = new CancellationTokenSource(); if (scenario == "cancel") cancelled.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancelled.Token);
        if (scenario is "normal" or "point" or "subset" or "window" or "nullable")
        {
            Assert.False(result.IsError, result.Exception?.ToString());
            Assert.Equal(scenario is "point" or "window" ? 1 : scenario == "subset" ? 2 : 3, result.Result.Count);
            Assert.Equal(scenario == "point" ? Addresses.Take(1).ToArray() : keys, native.Calls);
            foreach (var row in result.Result)
            {
                Assert.True(row.IsPublic); Assert.Equal(owner, row.CreatedByAvatarId); Assert.Empty(row.Children);
                Assert.Equal("explorer-reported-address-state", row.MetaData["BlockscoutObservationPolicy"]);
                Assert.Equal("false", row.MetaData["BlockscoutConsensusVerified"]);
                Assert.Equal(row.Name, row.MetaData["BlockscoutAddress"]);
                Assert.DoesNotContain(row.MetaData.Values, v => v?.ToString()?.Contains("inert-secret") == true);
                Assert.DoesNotContain("private_tags", row.MetaData.Keys); Assert.DoesNotContain("token", row.MetaData.Keys);
                Assert.Equal(scenario == "nullable" ? "false" : "true", row.MetaData["BlockscoutBalanceObserved"]);
                Assert.Equal(scenario != "nullable", row.MetaData.ContainsKey("BlockscoutCoinBalance"));
                Assert.Equal(scenario != "nullable", row.MetaData.ContainsKey("BlockscoutBalanceUpdatedAtBlock"));
                Assert.Equal(scenario != "nullable", row.MetaData.ContainsKey("BlockscoutIndexedName"));
                if (scenario != "nullable")
                {
                    Assert.Equal(new string('9',78), row.MetaData["BlockscoutCoinBalance"]);
                    Assert.Equal("18446744073709551615", row.MetaData["BlockscoutBalanceUpdatedAtBlock"]);
                }
            }
        }
        else { Assert.True(result.IsError); Assert.True(result.Result == null || result.Result.Count == 0); }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.Empty(native.Calls);
        if (scenario is "last" or "empty-window-error") Assert.Equal(3, native.Calls.Count);
    }

    [Fact]
    public async Task UnsupportedContentQueriesAndNativeWritesNeverDispatch()
    {
        var native = new Native(); using var client = new HttpClient(native);
        using var source = new BlockscoutPublicAddressMetadataSource(client, new Uri("https://example.invalid/api/v2/"), Addresses, Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlockscoutOASIS).Result).Where(h => h.Name == "anything").Result;
        Assert.True(CosmicQueryPlanner.Plan(query, ProviderType.BlockscoutOASIS,
            new[] { new CosmicQueryTarget(ProviderType.BlockscoutOASIS, source.QueryCapabilities) }).IsError);
        Assert.True((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.True((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
        Assert.Empty(native.Calls);
    }

    [Theory]
    [InlineData("scheme")] [InlineData("path")] [InlineData("credential")] [InlineData("query")]
    [InlineData("owner")] [InlineData("duplicate")] [InlineData("canonical")] [InlineData("count")] [InlineData("header")]
    public void InvalidCatalogueFailsBeforeNativeRequests(string fault)
    {
        var native = new Native(); using var client = new HttpClient(native); var keys = Addresses.ToList();
        if (fault == "header") client.DefaultRequestHeaders.Add("x-api-key", "inert-marker");
        if (fault == "duplicate") keys.Add(keys[0]);
        if (fault == "canonical") keys[0] = "0x"+new string('A',40);
        if (fault == "count") keys.Clear();
        string url = fault switch
        {
            "scheme" => "http://example.invalid/api/v2/", "path" => "https://example.invalid/",
            "credential" => "https://inert:inert@example.invalid/api/v2/", "query" => "https://example.invalid/api/v2/?key=inert",
            _ => "https://example.invalid/api/v2/"
        };
        Assert.Throws<ArgumentException>(() => new BlockscoutPublicAddressMetadataSource(client, new Uri(url), keys, fault == "owner" ? Guid.Empty : Guid.NewGuid()));
        Assert.Empty(native.Calls);
    }

    [Fact]
    public async Task SeparateExplorerInstancesCannotAliasTheSameOwnerAddressIdentity()
    {
        using var client = new HttpClient(new Native()); var owner = Guid.NewGuid();
        using var first = new BlockscoutPublicAddressMetadataSource(client, new Uri("https://one.invalid/api/v2/"), Addresses, owner);
        using var second = new BlockscoutPublicAddressMetadataSource(client, new Uri("https://two.invalid/api/v2/"), Addresses, owner);
        Assert.False((await first.ActivateQuerySourceAsync(default)).IsError);
        Assert.False((await second.ActivateQuerySourceAsync(default)).IsError);
        async Task<Guid> Id(BlockscoutPublicAddressMetadataSource source)
        {
            string key = Addresses[0];
            var q = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BlockscoutOASIS).Result)
                .Where(h => CosmicQueryMetadata.Value<string>(h, BlockscoutPublicAddressMetadataSource.AddressMetadata) == key).Result;
            var plan = CosmicQueryPlanner.Plan(q, ProviderType.BlockscoutOASIS, new[] { new CosmicQueryTarget(ProviderType.BlockscoutOASIS, source.QueryCapabilities) });
            var r = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(r.IsError, r.Exception?.ToString()); return Assert.Single(r.Result).Id;
        }
        Assert.NotEqual(await Id(first), await Id(second));
    }
}
