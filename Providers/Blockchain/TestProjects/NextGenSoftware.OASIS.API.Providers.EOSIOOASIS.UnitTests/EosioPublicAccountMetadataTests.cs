using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.UnitTests;

[TestClass]
public sealed class EosioPublicAccountMetadataTests
{
    private static readonly string[] Accounts = { "eosio", "eosio.token", "eosio.msig" };

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<string> Reads = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Post, request.Method);
            string method = request.RequestUri!.AbsolutePath;
            Reads.Add(method);
            using var input = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            string text;

            if (method == "/v1/chain/get_info")
                text = JsonSerializer.Serialize(new { chain_id = Fault == "chain" ? "wrong" : EosioPublicAccountMetadataSource.MainnetChainId });
            else
            {
                Assert.AreEqual("/v1/chain/get_account", method, "Never scan tables or sign transactions.");
                string account = input.RootElement.GetProperty("account_name").GetString()!;
                Assert.IsTrue(Accounts.Contains(account), "Only explicitly configured accounts may be read.");
                Reads[^1] += ":" + account;
                text = JsonSerializer.Serialize(new {
                    account_name = Fault == "identity" || (Fault == "last" && account == Accounts[^1]) ? "wrong" : account,
                    head_block_num = 528551468UL,
                    created = "2018-06-08T08:08:08.500",
                    ram_quota = 9007199254740993L
                });
                if (Fault == "fraction") text = text.Replace("9007199254740993", "1.5");
                if (Fault == "overflow") text = text.Replace("9007199254740993", "9223372036854775808");
                if (Fault == "head") text = text.Replace("528551468", "0");
                if (Fault == "date") text = text.Replace("2018-06-08T08:08:08.500", "yesterday");
            }

            if (Fault == "duplicate") text = "{\"chain_id\":\"x\",\"chain_id\":\"y\"}";
            if (Fault == "error") text = "{\"error\":{\"what\":\"unknown account\"}}";
            if (Fault == "oversize") text = new string(' ', 262145);
            return new HttpResponseMessage(Fault == "http" ? HttpStatusCode.Forbidden : HttpStatusCode.OK) {
                Content = new StringContent(text, Encoding.UTF8, "application/json")
            };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("subset")] [DataRow("window")]
    [DataRow("chain")] [DataRow("identity")] [DataRow("last")] [DataRow("fraction")]
    [DataRow("overflow")] [DataRow("head")] [DataRow("date")] [DataRow("duplicate")]
    [DataRow("error")] [DataRow("oversize")] [DataRow("http")] [DataRow("cancel")]
    [DataRow("budget")] [DataRow("unknown")] [DataRow("inactive")] [DataRow("empty-window-error")]
    public async Task PublishesOnlyCompleteIdentityCheckedSelections(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new EosioPublicAccountMetadataSource(client, new Uri("https://example.invalid/"),
            EosioPublicAccountMetadataSource.MainnetChainId, Accounts, Guid.NewGuid());

        if (scenario != "inactive")
            Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);

        native.Reads.Clear();
        native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        string[] selected = scenario == "unknown" ? new[] { "unknown" } : scenario == "subset" ? Accounts.Take(2).ToArray() : Accounts;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.EOSIOOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, EosioPublicAccountMetadataSource.AddressMetadata))).Result;

        if (scenario == "point")
        {
            string key = Accounts[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.EOSIOOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, EosioPublicAccountMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;

        var plan = CosmicQueryPlanner.Plan(query, ProviderType.EOSIOOASIS,
            new[] { new CosmicQueryTarget(ProviderType.EOSIOOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource();
        if (scenario == "cancel") cancel.Cancel();

        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "subset" or "window")
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : scenario == "subset" ? 2 : 1, result.Result.Count);
            Assert.IsTrue(result.Result.All(row => row.IsPublic));
            Assert.AreEqual("9007199254740993", result.Result[0].MetaData["EOSIORamQuota"]);
            Assert.AreEqual("2018-06-08T08:08:08.500", result.Result[0].MetaData["EOSIOCreated"]);
            Assert.AreEqual(scenario == "window" ? Accounts[1] : Accounts[0], result.Result[0].MetaData[EosioPublicAccountMetadataSource.AddressMetadata]);
            Assert.AreEqual(scenario == "point" ? 2 : selected.Length + 1, native.Reads.Count);
        }
        else
        {
            Assert.IsTrue(result.IsError, "No partial native result may escape.");
            Assert.IsTrue(result.Result == null || result.Result.Count == 0);
        }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, native.Reads.Count);
    }

    [TestMethod]
    public async Task PublicSourceRejectsSavingWithoutAnyNativeWrite()
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new EosioPublicAccountMetadataSource(client, new Uri("https://example.invalid/"),
            EosioPublicAccountMetadataSource.MainnetChainId, Accounts, Guid.NewGuid());
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Reads.Clear();

        var result = await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "No native write" }, saveChildren: false);

        Assert.IsTrue(result.IsError);
        Assert.AreEqual(0, native.Reads.Count);
    }

    [DataTestMethod]
    [DataRow("eosio.")] [DataRow("EOSIO")] [DataRow("eosio6")] [DataRow("abcdefghijklz")]
    [DataRow("")] [DataRow("eosio/token")]
    public void RejectsNonCanonicalAccountNames(string key)
    {
        using var client = new HttpClient(new Native());
        Assert.ThrowsException<ArgumentException>(() => new EosioPublicAccountMetadataSource(client,
            new Uri("https://example.invalid/"), EosioPublicAccountMetadataSource.MainnetChainId, new[] { key }, Guid.NewGuid()));
    }

    [TestMethod]
    public void RejectsSecretConfigurationAndWrongNetworkBeforeAnyRead()
    {
        Assert.ThrowsException<ArgumentException>(() => EosioPublicAccountMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        var native = new Native();
        using var client = new HttpClient(native);
        Assert.ThrowsException<ArgumentException>(() => new EosioPublicAccountMetadataSource(client,
            new Uri("https://example.invalid/"), "17777", Accounts, Guid.NewGuid()));
        Assert.AreEqual(0, native.Reads.Count);
    }
}
