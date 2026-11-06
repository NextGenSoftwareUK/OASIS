using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.FastlyOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class FastlyCqlTests
{
    [TestMethod]
    public async Task ExactReadProfileReturnsPublicRowsAndRejectsPrivateRowsBeforePaging()
    {
        const ProviderType authority = ProviderType.FastlyOASIS;
        var first = Guid.Parse("aaaaaaaa-2222-4222-8222-222222222222");
        var second = Guid.Parse("aaaaaaaa-3333-4333-8333-333333333333");
        var deleted = Guid.Parse("aaaaaaaa-4444-4444-8444-444444444444");
        var missing = Guid.Parse("aaaaaaaa-5555-4555-8555-555555555555");
        var ids = new[] { first, second, deleted, missing };
        var privateRow = false;
        using var service = new FakeService((request, _) =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("api.fastly.com", request.RequestUri.Host);
            var path = Uri.UnescapeDataString(request.RequestUri.AbsolutePath);
            const string prefix = "/resources/stores/kv/teachingStore/keys/oasis/holon/";
            StringAssert.StartsWith(path, prefix);
            var id = Guid.ParseExact(path[prefix.Length..], "N");
            Assert.IsTrue(ids.Contains(id));
            if (id == missing) return FakeService.Status(HttpStatusCode.NotFound);
            return FakeService.Text(OasisJson.Serialize(new Holon
            {
                Id = id,
                IsPublic = id != second || !privateRow,
                DeletedDate = id == deleted ? DateTime.UtcNow : DateTime.MinValue
            }));
        });
        using var source = new FastlyQuerySource("teachingStore", "fixture-token", first, service);
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
}
