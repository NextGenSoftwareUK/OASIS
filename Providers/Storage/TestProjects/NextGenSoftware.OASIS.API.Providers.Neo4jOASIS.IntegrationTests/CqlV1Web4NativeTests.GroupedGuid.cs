using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task GroupedDistinctGuidDomainsUseRealNativeProvider(string providerName)
        => RunGroupedDistinctNativeFixture(providerName, false, guidDomains: true);

    private static async Task<int> VerifyGroupedGuidDomains(IOASISStorageProvider provider,
        ProviderType authority, MongoClient? mongo, string database, List<Guid> ownedIds, Dictionary<string, long> timings)
    {
        var adapter = (ICosmicQueryGroupedStorageProvider)provider;
        var first = Guid.Parse("abcdef01-2345-6789-abcd-ef0123456789");
        var second = Guid.Parse("abcdef01-2345-6789-abcd-ef012345678a");
        async Task<Guid> Save(object? value, bool missing = false)
        {
            var id = Guid.NewGuid();
            ownedIds.Add(id);
            var metadata = new Dictionary<string, object> { ["category"] = "a" };
            if (!missing) metadata["stock"] = value!;
            var saved = await provider.SaveHolonAsync(new Holon { Id = id, Name = "owned GUID count",
                IsActive = true, MetaData = metadata }, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            return id;
        }
        var valid = new List<Guid>
        {
            await Save(first), await Save(first.ToString("D")),
            await Save(first.ToString("D").ToUpperInvariant()),
            await Save(second), await Save(second.ToString("D").ToUpperInvariant()),
            await Save(null), await Save(null, missing: true)
        };
        if (mongo != null)
        {
            Assert.AreEqual(ProviderType.MongoDBOASIS, authority);
            Assert.IsTrue(database.StartsWith("cosmic_distinct_", StringComparison.Ordinal));
            var records = mongo.GetDatabase(database).GetCollection<BsonDocument>("Holon");
            foreach (var representation in new[] { GuidRepresentation.Standard, GuidRepresentation.CSharpLegacy })
            {
                var id = await Save("owned binary placeholder");
                var filter = new BsonDocument("HolonId", id.ToString("D"));
                var binary = new BsonBinaryData(first, representation);
                var updated = await records.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("MetaData",
                    new BsonDocument { { "category", "a" }, { "stock", binary } }));
                Assert.AreEqual(1L, updated.MatchedCount);
                Assert.AreEqual(1L, updated.ModifiedCount);
                var actual = await records.Find(filter).SingleAsync();
                Assert.AreEqual(binary.SubType, actual["MetaData"]["stock"].AsBsonBinaryData.SubType);
                CollectionAssert.AreEqual(binary.Bytes, actual["MetaData"]["stock"].AsBsonBinaryData.Bytes);
                valid.Add(id);
            }
        }
        async Task<(long Present, long Unique)> Run<TValue>(string label, Guid[] ids,
            Expression<Func<Holon, TValue>> selector, bool invalid = false)
        {
            var grouping = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Result;
            var selected = grouping.SelectColumns(new[]
            {
                CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Present", selector).Result,
                CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Unique", selector, true).Result
            });
            Assert.IsFalse(selected.IsError, selected.Message);
            var query = invalid ? selected.Result.Take(0).Result : selected.Result;
            var planned = CosmicGroupedPlanner.Plan(query, authority,
                new[] { new CosmicGroupedTarget(authority, adapter.QueryCapabilities, adapter.GroupedCapabilities) });
            Assert.IsFalse(planned.IsError, planned.Message);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var result = await CosmicGroupedExecutor.ExecuteAsync(planned.Result,
                new[] { (ICosmicQueryStorageProvider)provider });
            timings.Add(label, watch.ElapsedMilliseconds);
            if (invalid)
            {
                Assert.IsTrue(result.IsError);
                Assert.AreEqual("CQL_DATA_INVALID", result.ErrorCode);
                return default;
            }
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            return ((long)row["Present"], (long)row["Unique"]);
        }
        var mixed = await Run("mixedGuid", valid.ToArray(), h => CosmicQueryMetadata.Value<Guid?>(h, "stock"));
        Assert.AreEqual(((long)valid.Count - 2, 2L), mixed);
        var canonical = await Run("canonicalGuid", valid.ToArray(), h => h.Id);
        Assert.AreEqual(((long)valid.Count, (long)valid.Count), canonical);
        var requiredIds = valid.Where((id, index) => index != 5 && index != 6).ToArray();
        var required = await Run("requiredGuid", requiredIds, h => CosmicQueryMetadata.Value<Guid>(h, "stock"));
        Assert.AreEqual(((long)requiredIds.Length, 2L), required);
        var empty = await Run("missingNull", new[] { valid[5], valid[6] }, h => CosmicQueryMetadata.Value<Guid?>(h, "stock"));
        Assert.AreEqual((0L, 0L), empty);
        var invalidId = await Save("not-a-guid");
        await Run("invalidOffPage", new[] { valid[0], invalidId }, h => CosmicQueryMetadata.Value<Guid?>(h, "stock"), invalid: true);
        Assert.AreEqual(mongo == null ? 8 : 10, ownedIds.Count);
        return 5;
    }
}
