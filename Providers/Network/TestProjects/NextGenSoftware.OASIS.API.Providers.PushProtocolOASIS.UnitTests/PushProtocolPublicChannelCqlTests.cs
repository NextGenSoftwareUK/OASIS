using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PushProtocolOASIS.UnitTests;

[TestClass]
public sealed class PushProtocolPublicChannelCqlTests
{
    private const ProviderType Provider = ProviderType.PushProtocolOASIS;
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Addresses =
    {
        "0xb88460bb2696cab9d66013a05dff29a28330689d",
        "0x90a48d5cf7343b08da12e067680b4c6dbfe551be",
        "0xdbc5936e4daae94f415c39d284f6a69c4d553f2f"
    };

    [DataTestMethod]
    [DataRow("0x0000000000000000000000000000000000000001", 1)]
    [DataRow("0xb88460bb2696cab9d66013a05dff29a28330689d", 9)]
    public async Task InvalidNativeIdentityOrVerificationCannotActivate(string address, int verification)
    {
        using var transport = new ChannelTransport(address, verification);
        using var source = new PushProtocolPublicChannelQuerySource(Owner, transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(1, transport.Requests);
    }

    [TestMethod]
    public async Task ExactChannelProjectionOmitsExtraNativeFieldsAndRejectsForeignKeys()
    {
        using var transport = new ChannelTransport(Addresses[0], 1);
        using var source = new PushProtocolPublicChannelQuerySource(Owner, transport);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        var id = PushProtocolPublicChannelQuerySource.LogicalId(Owner, Addresses[0]);
        var result = await source.ExecuteCosmicQueryAsync(Plan(source, new[] { id }));
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        var row = result.Result.Single();
        Assert.AreEqual(id, row.Id);
        Assert.AreEqual(Addresses[0], row.MetaData["PushChannelAddress"]);
        Assert.AreEqual("Teaching Channel", row.Name);
        Assert.IsFalse(row.MetaData.ContainsKey("subscriber_list"));
        Assert.IsFalse(row.MetaData.ContainsKey("channel_settings"));
        var foreign = await source.ExecuteCosmicQueryAsync(Plan(source, new[] { Guid.NewGuid() }));
        Assert.IsTrue(foreign.IsError);
        Assert.AreEqual(2, transport.Requests);
    }

    [TestMethod, TestCategory("PushProtocolPublicChannelNative")]
    public async Task OfficialPushApiReturnsExactThreeAndTwoPublicChannelRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_PUSH_PUBLIC_CHANNEL_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed Push Protocol public channel reads explicitly.");
            return;
        }

        using var source = new PushProtocolPublicChannelQuerySource(Owner);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.Message);
        foreach (var count in new[] { 3, 2 })
        {
            var selected = Addresses.Take(count).ToArray();
            var ids = selected.Select(address => PushProtocolPublicChannelQuerySource.LogicalId(Owner, address)).ToArray();
            var result = await source.ExecuteCosmicQueryAsync(Plan(source, ids));
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            CollectionAssert.AreEquivalent(ids, result.Result.Select(row => row.Id).ToArray());
            CollectionAssert.AreEquivalent(selected, result.Result.Select(row => row.ProviderUniqueStorageKey[Provider]).ToArray());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && row.CreatedByAvatarId == Owner &&
                row.MetaData["PushChannelChainId"].ToString() == "eip155:1"));
        }
    }

    private static CosmicQueryPlan<Holon> Plan(PushProtocolPublicChannelQuerySource source, Guid[] ids)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id));
        Assert.IsFalse(query.IsError, query.Message);
        var plan = CosmicQueryPlanner.Plan(query.Result, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    private sealed class ChannelTransport(string address, int verification) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("https://backend.epns.io/apis/v1/channels/eip155:1:" + Addresses[0], request.RequestUri!.AbsoluteUri);
            Assert.IsNull(request.Headers.Authorization);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new
            {
                channel = address, name = "Teaching Channel", verified_status = verification,
                subscriber_list = new[] { "synthetic-subscription" }, channel_settings = "synthetic-settings"
            }) });
        }
    }
}
