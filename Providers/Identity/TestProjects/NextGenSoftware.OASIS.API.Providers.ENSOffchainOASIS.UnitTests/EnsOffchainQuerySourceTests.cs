using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS.UnitTests;

[TestClass]
public class EnsOffchainQuerySourceTests
{
    private const string Address = "0x0123456789012345678901234567890123456789";
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Names = { "vitalik.eth", "nick.eth", "luc.eth" };

    [TestMethod]
    public async Task CatalogueQueriesUseOnlyTheIndexedExactNameAndPublicProjection()
    {
        using var transport = new Reply((_, name) => new { identity = name, address = Address, platform = "ens", description = "ignored" });
        using var source = new EnsOffchainQuerySource(Owner, Names, Names[0], transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        var ids = Names.Select(name => EnsOffchainQuerySource.LogicalId(Owner, name)).ToArray();
        await QueryAsync(source, ids, ids);
        await QueryAsync(source, ids[..2], ids[..2]);
        Assert.AreEqual(6, transport.Requests);
    }

    [DataTestMethod]
    [DataRow("identity")]
    [DataRow("platform")]
    [DataRow("address")]
    public async Task ActivationRejectsForeignAndMalformedObservations(string invalid)
    {
        using var transport = new Reply((_, name) => new
        {
            identity = invalid == "identity" ? "other.eth" : name,
            platform = invalid == "platform" ? "ethereum" : "ens",
            address = invalid == "address" ? "0x1" : Address
        });
        using var source = new EnsOffchainQuerySource(Owner, Names, Names[0], transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
    }

    [TestMethod]
    public async Task UnconfiguredIdentityNeverLeavesTheCatalogue()
    {
        using var transport = new Reply((_, name) => new { identity = name, address = Address, platform = "ens" });
        using var source = new EnsOffchainQuerySource(Owner, Names, Names[0], transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        await QueryAsync(source, new[] { Guid.NewGuid() }, expected: null);
        Assert.AreEqual(1, transport.Requests);
    }

    [TestMethod, TestCategory("EnsOffchainPublicCqlNative")]
    public async Task ReviewedPublicEnsIndexReturnsThreeAndTwoExactRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_ENS_OFFCHAIN_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public Web3.bio probe explicitly.");
            return;
        }
        using var source = new EnsOffchainQuerySource(Owner, Names, Names[0]);
        var active = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(active.IsError, active.Exception?.Message);
        var ids = Names.Select(name => EnsOffchainQuerySource.LogicalId(Owner, name)).ToArray();
        await QueryAsync(source, ids, ids);
        await QueryAsync(source, ids[..2], ids[..2]);
    }

    private static async Task QueryAsync(EnsOffchainQuerySource source, Guid[] selected, Guid[]? expected)
    {
        const ProviderType provider = ProviderType.ENSOffchainOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, provider, selected).Result).Result
            .Where(row => selected.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        if (expected == null)
        {
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            return;
        }
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
            Assert.AreEqual(row.Name, row.ProviderUniqueStorageKey[provider]);
            Assert.AreEqual("web3.bio-index", row.MetaData["EnsObservationPolicy"].ToString());
            Assert.IsNull(row.Description);
        }
    }

    private sealed class Reply(Func<HttpRequestMessage, string, object> reply) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("api.web3.bio", request.RequestUri?.Host);
            var name = request.RequestUri!.Segments[^1];
            Assert.AreEqual($"/ns/ens/{name}", request.RequestUri.AbsolutePath);
            Assert.IsTrue(Names.Contains(name));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(reply(request, name)) });
        }
    }
}
