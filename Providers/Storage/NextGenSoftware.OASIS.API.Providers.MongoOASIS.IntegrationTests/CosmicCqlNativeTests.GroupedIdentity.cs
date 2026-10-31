using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeGroupedGuidIdentityAndExtremaAgreeAcrossStoredRepresentations(bool legacy)
    {
        var group = Guid.Parse("a0000000-bbbb-cccc-dddd-eeeeeeeeeeee");
        var first = Guid.Parse("00000001-0000-0000-0000-000000000000");
        var second = Guid.Parse("00000002-0000-0000-0000-000000000000");
        return WithMongoGraph(async fixture =>
        {
            var root = fixture.Root.Id; var child = fixture.Root.Children.First().Id;
            foreach (var item in new[] { (root, first, false), (child, second, legacy) })
            {
                var metadata = new BsonDocument
                {
                    { "groupId", item.Item1 == root ? (BsonValue)group.ToString("D").ToUpperInvariant()
                        : new BsonBinaryData(group, item.Item3 ? GuidRepresentation.CSharpLegacy : GuidRepresentation.Standard) },
                    { "code", new BsonBinaryData(item.Item2, item.Item3 ? GuidRepresentation.CSharpLegacy : GuidRepresentation.Standard) }
                };
                fixture.KnownOutcome = false;
                var updated = await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId", item.Item1.ToString("D")),
                    Builders<BsonDocument>.Update.Set("MetaData", metadata));
                fixture.KnownOutcome = true; Assert.Equal(1, updated.MatchedCount);
            }
            var ids = new[] { root, child };
            var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<Guid>(h, "groupId"))
                .Select(g => new
                {
                    g.Key, Count = g.LongCount(),
                    Min = g.Min(h => CosmicQueryMetadata.Value<Guid>(h, "code")),
                    Max = g.Max(h => CosmicQueryMetadata.Value<Guid>(h, "code"))
                }).Result;
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.Equal(group, row.Key); Assert.Equal(2L, row.Count);
            Assert.Equal(first, row.Min); Assert.Equal(second, row.Max);
        });
    }
}
