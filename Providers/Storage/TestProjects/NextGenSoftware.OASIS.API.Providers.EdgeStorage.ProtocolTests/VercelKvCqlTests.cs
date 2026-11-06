using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.VercelKVOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class VercelKvCqlTests
{
    [TestMethod]
    public async Task ExactReadProfileReturnsPublicRowsAndRejectsPrivateRowsBeforePaging()
    {
        const ProviderType authority = ProviderType.VercelKVOASIS;
        var first = Guid.Parse("aaaaaaaa-2222-4222-8222-222222222222");
        var second = Guid.Parse("aaaaaaaa-3333-4333-8333-333333333333");
        var deleted = Guid.Parse("aaaaaaaa-4444-4444-8444-444444444444");
        var missing = Guid.Parse("aaaaaaaa-5555-4555-8555-555555555555");
        var ids = new[] { first, second, deleted, missing };
        var privateRow = false;
        using var service = new FakeService((request, body) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("fixture-token", request.Headers.Authorization?.Parameter);
            Assert.AreEqual("https://teaching.upstash.io/", request.RequestUri.AbsoluteUri);
            var command = JsonSerializer.Deserialize<string[]>(body);
            Assert.AreEqual(2, command.Length);
            Assert.AreEqual("GET", command[0]);
            const string prefix = "oasis/holon/";
            StringAssert.StartsWith(command[1], prefix);
            var id = Guid.ParseExact(command[1][prefix.Length..], "N");
            Assert.IsTrue(ids.Contains(id));
            if (id == missing) return FakeService.Json(new { result = (string)null });
            return FakeService.Json(new { result = OasisJson.Serialize(new Holon
            {
                Id = id,
                IsPublic = id != second || !privateRow,
                DeletedDate = id == deleted ? DateTime.UtcNow : DateTime.MinValue
            }) });
        });
        using var source = new VercelKvQuerySource(new Uri("https://teaching.upstash.io/"), "fixture-token", first, service);
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
        using var service = new FakeService((_, _) =>
        {
            requests++;
            return requests == 1 ? new HttpResponseMessage(HttpStatusCode.Forbidden) :
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[1024 * 1024 + 1]) };
        });
        using var source = new VercelKvQuerySource(new Uri("https://teaching.upstash.io/"), "fixture-token", Guid.NewGuid(), service);
        using var backend = new UpstashRestBackend("https://teaching.upstash.io/", "fixture-token", service, queryOnly: true);
        var key = $"oasis/holon/{Guid.NewGuid():N}";
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.PutAsync(key, "new value"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.DeleteAsync(key));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.ListKeysAsync("oasis/"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.GetAsync($"oasis/avatar/{Guid.NewGuid():N}"));
        Assert.AreEqual(0, requests);

        var denied = await source.ActivateQuerySourceAsync(default);
        Assert.IsTrue(denied.IsError);
        Assert.AreEqual(HttpStatusCode.Forbidden, (denied.Exception as HttpRequestException)?.StatusCode);
        Assert.IsTrue((await source.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(2, requests);
    }
}
