using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.CloudflareOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CloudflareOASIS.UnitTests;

[TestClass]
public class CloudflareKvCqlTests
{
    [TestMethod]
    public async Task ExactReadProfileReturnsPublicRowsAndRejectsPrivateRowsBeforePaging()
    {
        const ProviderType authority = ProviderType.CloudflareOASIS;
        var first = Guid.Parse("aaaaaaaa-2222-4222-8222-222222222222");
        var second = Guid.Parse("aaaaaaaa-3333-4333-8333-333333333333");
        var deleted = Guid.Parse("aaaaaaaa-4444-4444-8444-444444444444");
        var missing = Guid.Parse("aaaaaaaa-5555-4555-8555-555555555555");
        var ids = new[] { first, second, deleted, missing };
        var privateRow = false;
        using var service = new FixtureHandler(request =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("fixture-token", request.Headers.Authorization?.Parameter);
            Assert.IsNotNull(request.RequestUri);
            Assert.AreEqual("api.cloudflare.com", request.RequestUri!.Host);
            var path = Uri.UnescapeDataString(request.RequestUri.AbsolutePath);
            const string prefix = "/client/v4/accounts/0123456789abcdef0123456789abcdef/storage/kv/namespaces/abcdef0123456789abcdef0123456789/values/holon:";
            StringAssert.StartsWith(path, prefix);
            var id = Guid.ParseExact(path[prefix.Length..], "D");
            Assert.IsTrue(ids.Contains(id));
            if (id == missing) return new HttpResponseMessage(HttpStatusCode.NotFound);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(OasisJson.Serialize(new Holon
            {
                Id = id,
                IsPublic = id != second || !privateRow,
                DeletedDate = id == deleted ? DateTime.UtcNow : DateTime.MinValue
            })) };
        });
        using var source = new CloudflareKvQuerySource("0123456789abcdef0123456789abcdef", "abcdef0123456789abcdef0123456789", "fixture-token", first, service);
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var targets = new[] { new CosmicQueryTarget(authority, source.QueryCapabilities) };
        var full = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query, authority, targets).Result);
        Assert.IsFalse(full.IsError, full.Message);
        CollectionAssert.AreEqual(new[] { first, second }, full.Result.Select(row => row.Id).ToArray());

        privateRow = true;
        var paged = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query.Take(1).Result, authority, targets).Result);
        Assert.IsTrue(paged.IsError);
        Assert.IsNull(paged.Result);
    }

    [TestMethod]
    public async Task Query_transport_rejects_storage_operations_and_unusable_probe_responses()
    {
        var requests = 0;
        using var service = new FixtureHandler(_ =>
        {
            requests++;
            return requests == 1 ? new HttpResponseMessage(HttpStatusCode.Forbidden) :
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[1024 * 1024 + 1]) };
        });
        using var source = new CloudflareKvQuerySource("0123456789abcdef0123456789abcdef", "abcdef0123456789abcdef0123456789", "fixture-token", Guid.NewGuid(), service);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid() })).IsError);
        Assert.IsTrue((await source.LoadAllHolonsAsync()).IsError);
        Assert.IsTrue((await source.LoadAvatarAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, requests);

        var denied = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(denied.IsError);
        Assert.AreEqual(HttpStatusCode.Forbidden, (denied.Exception as HttpRequestException)?.StatusCode);
        Assert.IsTrue((await source.ActivateQuerySourceAsync()).IsError);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(2, requests);
    }

    private sealed class FixtureHandler(Func<HttpRequestMessage, HttpResponseMessage> read) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(read(request));
    }
}
