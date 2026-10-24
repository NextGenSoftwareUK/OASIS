using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData("0")] [InlineData("9007199254740993")] [InlineData("9223372036854775807")]
    [InlineData("9223372036854775808")] [InlineData("18446744073709551614")] [InlineData("18446744073709551615")]
    public Task NativeUInt64PersistsAndQueriesExactUnsignedBoundary(string text)
    {
        var number = ulong.Parse(text, CultureInfo.InvariantCulture);
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var stored = await fixture.Records.Find(new BsonDocument("HolonId", id.ToString("D"))).SingleAsync();
            Assert.True(stored["MetaData"]["counter"].IsDecimal128);
            Assert.Equal((decimal)number, Decimal128.ToDecimal(stored["MetaData"]["counter"].AsDecimal128));
            var members = new[] { number };
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => row.Id == id
                && CosmicQueryMetadata.Value<ulong>(row, "counter") == number
                && members.Contains(CosmicQueryMetadata.Value<ulong>(row, "counter")))
                .OrderBy(row => CosmicQueryMetadata.Value<ulong>(row, "counter"));
            Assert.False(source.IsError, source.Message);
            var selected = source.Select(row => CosmicQueryMetadata.Value<ulong>(row, "counter"));
            Assert.False(selected.IsError, selected.Message);
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected.Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(number, Assert.Single(Assert.Single(result.Result.Providers).Rows));
            var grouped = source.GroupBy(row => CosmicQueryMetadata.Value<ulong>(row, "counter"))
                .Select(group => new { Value = group.Key, Rows = group.LongCount() })
                .Where(row => row.Value == number).OrderBy(row => row.Value).Take(1);
            Assert.False(grouped.IsError, grouped.Message);
            var groups = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(grouped.Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(groups.IsError, groups.Message + groups.Exception);
            Assert.Equal(number, Assert.Single(Assert.Single(groups.Result.Providers).Rows).Value);
        }, width: 0, configure: root => root.MetaData["counter"] = number);
    }

    [Theory]
    [InlineData("negative")] [InlineData("fraction")] [InlineData("overflow")]
    [InlineData("double")] [InlineData("nan")] [InlineData("infinity")] [InlineData("string")]
    public Task NativeUInt64RejectsWrongBsonDomainsWithoutPartialProjection(string kind)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            BsonValue value = kind switch
            {
                "negative" => new BsonInt64(-1), "fraction" => new BsonDecimal128(1.5m),
                "overflow" => new BsonDecimal128(18446744073709551616m),
                "double" => new BsonDouble(1d), "nan" => new BsonDecimal128(Decimal128.Parse("NaN")),
                "infinity" => new BsonDecimal128(Decimal128.PositiveInfinity),
                _ => new BsonString("18446744073709551615")
            };
            var update = await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId", id.ToString("D")),
                new BsonDocument("$set", new BsonDocument("MetaData.counter", value)));
            Assert.Equal(1, update.MatchedCount);
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => row.Id == id);
            var selected = source.Select(row => CosmicQueryMetadata.Value<ulong>(row, "counter")).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
            var matching = source.Where(row => CosmicQueryMetadata.Value<ulong>(row, "counter") == ulong.MaxValue).Result;
            var plan = CosmicQueryPlanner.Plan(matching, ProviderType.MongoDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities) });
            Assert.False(plan.IsError, plan.Message);
            var rows = await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(rows.IsError, rows.Message + rows.Exception); Assert.Empty(rows.Result);
        }, width: 0, configure: root => root.MetaData["counter"] = 0UL);
    [Fact]
    public Task NativeUInt64OrderingGroupingAndExtremaDistinguishAdjacentMaxima()
        => WithMongoGraph(async fixture =>
        {
            var ids = new System.Collections.Generic.List<Guid> { fixture.Root.Id };
            fixture.KnownOutcome = false;
            foreach (var number in new[] { ulong.MaxValue - 1UL, ulong.MaxValue })
            {
                var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["counter"] = number } };
                var saved = await fixture.Provider.SaveHolonAsync(row, saveChildren: false);
                Assert.False(saved.IsError, saved.Message + saved.Exception); ids.Add(row.Id);
            }
            fixture.KnownOutcome = true;
            var keys = ids.ToArray();
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => keys.Contains(h.Id));
            var selected = source.OrderBy(h => CosmicQueryMetadata.Value<ulong>(h, "counter"))
                .Skip(1).Take(2).Select(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result;
            var rows = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(rows.IsError, rows.Message + rows.Exception);
            Assert.Equal(new[] { ulong.MaxValue - 1UL, ulong.MaxValue }, rows.Result.Providers.Single().Rows);
            var grouped = source.GroupBy(h => CosmicQueryMetadata.Value<ulong>(h, "counter"))
                .Select(g => new { Value = g.Key, Rows = g.LongCount() })
                .Where(g => g.Value > (ulong)long.MaxValue).OrderBy(g => g.Value).Take(2).Result;
            var groups = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(grouped),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(groups.IsError, groups.Message + groups.Exception);
            Assert.Equal(new[] { ulong.MaxValue - 1UL, ulong.MaxValue }, groups.Result.Providers.Single().Rows.Select(g => g.Value));
            var maximum = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Result.Max(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result));
            Assert.False(maximum.IsError, maximum.Message + maximum.Exception); Assert.Equal(ulong.MaxValue, maximum.Result);
        }, width: 0, configure: root => root.MetaData["counter"] = 0UL);

}
