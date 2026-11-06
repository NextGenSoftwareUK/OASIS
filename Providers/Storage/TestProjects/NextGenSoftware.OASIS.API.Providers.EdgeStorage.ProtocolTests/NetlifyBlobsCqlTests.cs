using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.NetlifyBlobsOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class NetlifyBlobsCqlTests
{
    private static readonly Guid Site = Guid.Parse("aaaaaaaa-1111-4111-8111-111111111111");
    private static readonly Guid First = Guid.Parse("aaaaaaaa-2222-4222-8222-222222222222");
    private static readonly Guid Last = Guid.Parse("aaaaaaaa-3333-4333-8333-333333333333");
    private static readonly Uri Origin = new("https://signed.example");

    [TestMethod]
    public async Task PublicRowsUseExactNativeKeysAndKeepBearerOnTheSigningRequest()
    {
        var privateRow = false;
        using var service = new FakeService((request, _) =>
        {
            var uri = request.RequestUri;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            var id = Guid.ParseExact(uri.AbsolutePath.Split('/').Last(), "N");
            if (uri.Host == "api.netlify.com")
            {
                Assert.AreEqual($"/api/v1/blobs/{Site:D}/site:oasis/holon/{id:N}", Uri.UnescapeDataString(uri.AbsolutePath));
                Assert.AreEqual("Bearer fixture-token", request.Headers.Authorization.ToString());
                Assert.AreEqual("application/json;type=signed-url", request.Headers.Accept.Single().ToString().Replace(" ", ""));
                return FakeService.Json(new { url = $"https://signed.example/holon/{id:N}?signature=fixture" });
            }
            Assert.AreEqual(Origin.Host, uri.Host);
            Assert.IsNull(request.Headers.Authorization);
            return FakeService.Text(OasisJson.Serialize(new Holon { Id = id, IsPublic = id != Last || !privateRow }));
        });
        using var source = new NetlifyBlobsQuerySource(Site, "fixture-token", "oasis", Origin, First, service);
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        var plan = Plan(source);
        var result = await source.ExecuteCosmicQueryAsync(plan);
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(First, result.Result.Single().Id);

        privateRow = true;
        var rejected = await source.ExecuteCosmicQueryAsync(plan);
        Assert.IsTrue(rejected.IsError);
        Assert.IsNull(rejected.Result);
    }

    [DataTestMethod]
    [DataRow("https://foreign.example/item?signature=fixture")]
    [DataRow("https://signed.example/item")]
    [DataRow("https://user@signed.example/item?signature=fixture")]
    [DataRow("http://signed.example/item?signature=fixture")]
    public async Task SignedDestinationMustMatchTheReviewedHttpsOrigin(string signedUrl)
    {
        using var service = new FakeService((_, _) => FakeService.Json(new { url = signedUrl }));
        using var source = new NetlifyBlobsQuerySource(Site, "fixture-token", "oasis", Origin, First, service);
        var result = await source.ActivateQuerySourceAsync(default);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(1, service.Requests.Count);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SigningAndObjectBodiesAreBounded(bool oversizedEnvelope)
    {
        using var service = new FakeService((request, _) =>
        {
            if (request.RequestUri.Host == "api.netlify.com")
                return oversizedEnvelope ? FakeService.Text(new string(' ', 8193))
                    : FakeService.Json(new { url = "https://signed.example/item?signature=fixture" });
            return FakeService.Text(new string(' ', 1024 * 1024 + 1));
        });
        using var source = new NetlifyBlobsQuerySource(Site, "fixture-token", "oasis", Origin, First, service);
        var result = await source.ActivateQuerySourceAsync(default);
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType<System.IO.InvalidDataException>(result.Exception);
        Assert.AreEqual(oversizedEnvelope ? 1 : 2, service.Requests.Count);
    }

    private static CosmicQueryPlan<Holon> Plan(NetlifyBlobsQuerySource source)
    {
        const ProviderType authority = ProviderType.NetlifyBlobsOASIS;
        var ids = new[] { First, Last };
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result.Take(1).Result;
        var plan = CosmicQueryPlanner.Plan(query, authority, new[] { new CosmicQueryTarget(authority, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }
}
