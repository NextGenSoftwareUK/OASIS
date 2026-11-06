using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SafeOASIS.UnitTests;

[TestClass]
public sealed class SafeIndexingQuerySourceTests
{
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Chains = { "eth", "base", "arb1" };
    private static readonly Guid[] Ids = Chains.Select(chain => SafeIndexingQuerySource.LogicalId(Owner, chain)).ToArray();

    [TestMethod]
    public async Task ExactKeysProjectOnlyPublicIndexingData()
    {
        using var transport = new Reply(chain => Response());
        using var source = new SafeIndexingQuerySource(Owner, Chains, Chains[0], "fixture-key", transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        Check(await Execute(source, Ids), Ids);
        Check(await Execute(source, Ids[..2]), Ids[..2]);
        Assert.AreEqual(6, transport.Calls);
        var rejected = await Execute(source, new[] { Guid.NewGuid() });
        Assert.IsTrue(rejected.IsError);
        Assert.IsNull(rejected.Result);
        Assert.AreEqual(6, transport.Calls);
    }

    [TestMethod]
    public async Task FailedNetworkRejectsTheWholePage()
    {
        using var transport = new Reply(chain => chain == "base" ? new(HttpStatusCode.TooManyRequests) : Response());
        using var source = new SafeIndexingQuerySource(Owner, Chains, Chains[0], "fixture-key", transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        var failed = await Execute(source, Ids);
        Assert.IsTrue(failed.IsError);
        Assert.IsNull(failed.Result);
    }

    [TestMethod, TestCategory("SafeIndexingPublicCqlNative")]
    public async Task OfficialSafeGatewayProvidesThreeAndTwoPublicRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_SAFE_VERIFY_PUBLIC_INDEX") != "1")
        {
            Assert.Inconclusive("Explicitly enable the public Safe indexing verification.");
            return;
        }

        using var source = new SafeIndexingQuerySource(Owner, Chains, Chains[0]);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        Check(await Execute(source, Ids), Ids);
        Check(await Execute(source, Ids[..2]), Ids[..2]);
    }

    private static Task<OASISResult<IReadOnlyList<IHolon>>> Execute(SafeIndexingQuerySource source, Guid[] ids)
    {
        const ProviderType provider = ProviderType.SafeOASIS;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return source.ExecuteCosmicQueryAsync(plan.Result);
    }

    private static void Check(OASISResult<IReadOnlyList<IHolon>> result, Guid[] expected)
    {
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
            Assert.AreEqual(row.Name, row.ProviderUniqueStorageKey[ProviderType.SafeOASIS]);
            Assert.IsTrue(row.MetaData.ContainsKey("SafeCurrentBlockNumber"));
            Assert.IsFalse(row.MetaData.ContainsKey("owners"));
        }
    }

    private static HttpResponseMessage Response() => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new
        {
            currentBlockNumber = 100, erc20BlockNumber = 99, masterCopiesBlockNumber = 98,
            erc20Synced = true, masterCopiesSynced = true, synced = true, owners = new[] { "ignored" }
        })
    };

    private sealed class Reply(Func<string, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var uri = request.RequestUri!;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("https", uri.Scheme);
            Assert.AreEqual("api.safe.global", uri.Host);
            Assert.AreEqual("Bearer fixture-key", request.Headers.Authorization!.ToString());
            var chain = uri.Segments[2].TrimEnd('/');
            Assert.IsTrue(Chains.Contains(chain));
            Assert.AreEqual($"/tx-service/{chain}/api/v1/about/indexing/", uri.AbsolutePath);
            return Task.FromResult(reply(chain));
        }
    }
}
