using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public Task NativeDecimalPredicatesUseOneExactDomain(int operation)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var members = new[] { 0.1m, 1e-28m };
            Expression<Func<Holon, bool>> predicate = operation switch
            {
                0 => h => CosmicQueryMetadata.Value<decimal>(h, "price") == 0.1m,
                1 => h => CosmicQueryMetadata.Value<decimal>(h, "price") > 0m,
                2 => h => members.Contains(CosmicQueryMetadata.Value<decimal>(h, "price")),
                3 => h => CosmicQueryMetadata.Value<decimal?>(h, "optional") == null,
                4 => h => CosmicQueryMetadata.Value<decimal?>(h, "price") != 0.2m,
                5 => h => CosmicQueryMetadata.Value<decimal>(h, "price") == CosmicQueryMetadata.Value<decimal>(h, "reference"),
                6 => h => !(CosmicQueryMetadata.Value<decimal>(h, "price") == 0.2m),
                _ => h => CosmicQueryMetadata.Value<decimal>(h, "price") < 0.2m
            };
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Where(predicate);
            Assert.False(source.IsError, source.Message);
            var plan = CosmicQueryPlanner.Plan(source.Result, ProviderType.MongoDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities) });
            Assert.False(plan.IsError, plan.Message);
            var result = await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(id, Assert.Single(result.Result).Id);
        }, width: 0, configure: root =>
        {
            root.MetaData["price"] = 0.1m;
            root.MetaData["reference"] = 0.100m;
            // Null predicates require a present null member; absence is distinct.
            root.MetaData["optional"] = null;
        });

    [Theory]
    [InlineData("0")] [InlineData("-0.0000000000000000000000000001")]
    [InlineData("0.0000000000000000000000000001")] [InlineData("1.0000000000000000000000000001")]
    [InlineData("-79228162514264337593543950335")] [InlineData("79228162514264337593543950335")]
    public Task NativeDecimalPersistsAndQueriesExactDecimalBoundary(string text)
    {
        var number = decimal.Parse(text, CultureInfo.InvariantCulture);
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var stored = await fixture.Records.Find(new BsonDocument("HolonId", id.ToString("D"))).SingleAsync();
            Assert.True(stored["MetaData"]["counter"].IsDecimal128);
            Assert.Equal((decimal)number, Decimal128.ToDecimal(stored["MetaData"]["counter"].AsDecimal128));
            var members = new[] { number };
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => row.Id == id
                && CosmicQueryMetadata.Value<decimal>(row, "counter") == number
                && members.Contains(CosmicQueryMetadata.Value<decimal>(row, "counter")))
                .OrderBy(row => CosmicQueryMetadata.Value<decimal>(row, "counter"));
            Assert.False(source.IsError, source.Message);
            var selected = source.Select(row => CosmicQueryMetadata.Value<decimal>(row, "counter"));
            Assert.False(selected.IsError, selected.Message);
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected.Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(number, Assert.Single(Assert.Single(result.Result.Providers).Rows));
            var grouped = source.GroupBy(row => CosmicQueryMetadata.Value<decimal>(row, "counter"))
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
    [InlineData("precision")] [InlineData("underflow")] [InlineData("overflow")]
    [InlineData("double")] [InlineData("nan")] [InlineData("infinity")] [InlineData("string")]
    public Task NativeDecimalRejectsWrongBsonDomainsWithoutPartialProjection(string kind)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            BsonValue value = kind switch
            {
                "precision" => new BsonDecimal128(Decimal128.Parse("1.00000000000000000000000000001")),
                "underflow" => new BsonDecimal128(Decimal128.Parse("1E-1000")),
                "overflow" => new BsonDecimal128(Decimal128.Parse("79228162514264337593543950336")),
                "double" => new BsonDouble(1d), "nan" => new BsonDecimal128(Decimal128.Parse("NaN")),
                "infinity" => new BsonDecimal128(Decimal128.PositiveInfinity),
                _ => new BsonString("18446744073709551615")
            };
            var update = await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId", id.ToString("D")),
                new BsonDocument("$set", new BsonDocument("MetaData.counter", value)));
            Assert.Equal(1, update.MatchedCount);
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => row.Id == id);
            var selected = source.Select(row => CosmicQueryMetadata.Value<decimal>(row, "counter")).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
            var matching = source.Where(row => CosmicQueryMetadata.Value<decimal>(row, "counter") == decimal.MaxValue).Result;
            var plan = CosmicQueryPlanner.Plan(matching, ProviderType.MongoDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities) });
            Assert.False(plan.IsError, plan.Message);
            var rows = await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(rows.IsError, rows.Message + rows.Exception); Assert.Empty(rows.Result);
        }, width: 0, configure: root => root.MetaData["counter"] = 0m);

    [Fact]
    public Task NativeNullableDecimalGroupingCombinesEquivalentScales()
        => WithMongoGraph(async fixture =>
        {
            var ids = new System.Collections.Generic.List<Guid> { fixture.Root.Id };
            fixture.KnownOutcome = false;
            foreach (var value in new decimal?[] { 1.000m, null })
            {
                var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["price"] = value } };
                var saved = await fixture.Provider.SaveHolonAsync(row, saveChildren: false);
                Assert.False(saved.IsError, saved.Message + saved.Exception);
                ids.Add(row.Id);
            }
            fixture.KnownOutcome = true;
            var keys = ids.ToArray();
            var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => keys.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<decimal?>(h, "price"))
                .Select(g => new { Price = g.Key, Rows = g.LongCount() })
                .Where(g => g.Price == 1m).OrderBy(g => g.Price).Take(1);
            Assert.False(query.IsError, query.Message);
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query.Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var group = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal(1m, group.Price);
            Assert.Equal(2L, group.Rows);
        }, width: 0, configure: root => root.MetaData["price"] = 1m);

    [Fact]
    public Task NativeDecimalOrderingGroupingAndExtremaDistinguishAdjacentFractions()
        => WithMongoGraph(async fixture =>
        {
            var ids = new System.Collections.Generic.List<Guid> { fixture.Root.Id };
            fixture.KnownOutcome = false;
            foreach (var number in new[] { 1.0000000000000000000000000001m, 1.0000000000000000000000000002m })
            {
                var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["counter"] = number } };
                var saved = await fixture.Provider.SaveHolonAsync(row, saveChildren: false);
                Assert.False(saved.IsError, saved.Message + saved.Exception); ids.Add(row.Id);
            }
            fixture.KnownOutcome = true;
            var keys = ids.ToArray();
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => keys.Contains(h.Id));
            var selected = source.OrderBy(h => CosmicQueryMetadata.Value<decimal>(h, "counter"))
                .Skip(1).Take(2).Select(h => CosmicQueryMetadata.Value<decimal>(h, "counter")).Result;
            var rows = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(rows.IsError, rows.Message + rows.Exception);
            Assert.Equal(new[] { 1.0000000000000000000000000001m, 1.0000000000000000000000000002m }, rows.Result.Providers.Single().Rows);
            var grouped = source.GroupBy(h => CosmicQueryMetadata.Value<decimal>(h, "counter"))
                .Select(g => new { Value = g.Key, Rows = g.LongCount() })
                .Where(g => g.Value > 1m).OrderBy(g => g.Value).Take(2).Result;
            var groups = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(grouped),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(groups.IsError, groups.Message + groups.Exception);
            Assert.Equal(new[] { 1.0000000000000000000000000001m, 1.0000000000000000000000000002m }, groups.Result.Providers.Single().Rows.Select(g => g.Value));
            var maximum = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Result.Max(h => CosmicQueryMetadata.Value<decimal>(h, "counter")).Result));
            Assert.False(maximum.IsError, maximum.Message + maximum.Exception); Assert.Equal(1.0000000000000000000000000002m, maximum.Result);
        }, width: 0, configure: root => root.MetaData["counter"] = 0m);

}
