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
    public Task NativeDoublePredicatesUseOneExactDomain(int operation)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var members = new[] { 0.1d, double.Epsilon };
            Expression<Func<Holon, bool>> predicate = operation switch
            {
                0 => h => CosmicQueryMetadata.Value<double>(h, "price") == 0.1d,
                1 => h => CosmicQueryMetadata.Value<double>(h, "price") > 0d,
                2 => h => members.Contains(CosmicQueryMetadata.Value<double>(h, "price")),
                3 => h => CosmicQueryMetadata.Value<double?>(h, "optional") == null,
                4 => h => CosmicQueryMetadata.Value<double?>(h, "price") != 0.2d,
                5 => h => CosmicQueryMetadata.Value<double>(h, "price") == CosmicQueryMetadata.Value<double>(h, "reference"),
                6 => h => !(CosmicQueryMetadata.Value<double>(h, "price") == 0.2d),
                _ => h => CosmicQueryMetadata.Value<double>(h, "price") < 0.2d
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
            root.MetaData["price"] = 0.1d;
            root.MetaData["reference"] = 0.100d;
            // Null predicates require a present null member; absence is distinct.
            root.MetaData["optional"] = null;
        });

    [Theory]
    [InlineData("0")] [InlineData("-5E-324")]
    [InlineData("5E-324")] [InlineData("1.0000000000000002")]
    [InlineData("-1.7976931348623157E308")] [InlineData("1.7976931348623157E308")]
    public Task NativeDoublePersistsAndQueriesExactDoubleBoundary(string text)
    {
        var number = double.Parse(text, CultureInfo.InvariantCulture);
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var stored = await fixture.Records.Find(new BsonDocument("HolonId", id.ToString("D"))).SingleAsync();
            Assert.True(stored["MetaData"]["counter"].IsDouble);
            Assert.Equal((double)number, stored["MetaData"]["counter"].AsDouble);
            var members = new[] { number };
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => row.Id == id
                && CosmicQueryMetadata.Value<double>(row, "counter") == number
                && members.Contains(CosmicQueryMetadata.Value<double>(row, "counter")))
                .OrderBy(row => CosmicQueryMetadata.Value<double>(row, "counter"));
            Assert.False(source.IsError, source.Message);
            var selected = source.Select(row => CosmicQueryMetadata.Value<double>(row, "counter"));
            Assert.False(selected.IsError, selected.Message);
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected.Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(number, Assert.Single(Assert.Single(result.Result.Providers).Rows));
            var grouped = source.GroupBy(row => CosmicQueryMetadata.Value<double>(row, "counter"))
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
    [InlineData("underflow")] [InlineData("overflow")] [InlineData("nan")]
    [InlineData("infinity")] [InlineData("string")] [InlineData("object")] [InlineData("bool")]
    public Task NativeDoubleRejectsWrongBsonDomainsWithoutPartialProjection(string kind)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            BsonValue value = kind switch
            {
                "underflow" => new BsonDecimal128(Decimal128.Parse("1E-1000")),
                "overflow" => new BsonDecimal128(Decimal128.Parse("1E1000")),
                "nan" => new BsonDouble(double.NaN),
                "infinity" => new BsonDouble(double.PositiveInfinity),
                "object" => new BsonDocument(),
                "bool" => new BsonBoolean(true),
                _ => new BsonString("0.1")
            };
            var update = await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId", id.ToString("D")),
                new BsonDocument("$set", new BsonDocument("MetaData.counter", value)));
            Assert.Equal(1, update.MatchedCount);
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => row.Id == id);
            var selected = source.Select(row => CosmicQueryMetadata.Value<double?>(row, "counter")).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
            var matching = source.Where(row => CosmicQueryMetadata.Value<double>(row, "counter") == 0.1d).Result;
            var plan = CosmicQueryPlanner.Plan(matching, ProviderType.MongoDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities) });
            Assert.False(plan.IsError, plan.Message);
            var rows = await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(rows.IsError, rows.Message + rows.Exception); Assert.Empty(rows.Result);
        }, width: 0, configure: root => root.MetaData["counter"] = 0d);

    [Fact]
    public Task NativeNullableDoubleGroupingCombinesEquivalentValues()
        => WithMongoGraph(async fixture =>
        {
            var ids = new System.Collections.Generic.List<Guid> { fixture.Root.Id };
            fixture.KnownOutcome = false;
            foreach (var value in new double?[] { 1.000d, null })
            {
                var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["price"] = value } };
                var saved = await fixture.Provider.SaveHolonAsync(row, saveChildren: false);
                Assert.False(saved.IsError, saved.Message + saved.Exception);
                ids.Add(row.Id);
            }
            fixture.KnownOutcome = true;
            var keys = ids.ToArray();
            var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => keys.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<double?>(h, "price"))
                .Select(g => new { Price = g.Key, Rows = g.LongCount() })
                .Where(g => g.Price == 1d).OrderBy(g => g.Price).Take(1);
            Assert.False(query.IsError, query.Message);
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query.Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var group = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal(1d, group.Price);
            Assert.Equal(2L, group.Rows);
        }, width: 0, configure: root => root.MetaData["price"] = 1d);

    [Fact]
    public Task NativeDoubleOrderingGroupingAndExtremaDistinguishAdjacentFractions()
        => WithMongoGraph(async fixture =>
        {
            var ids = new System.Collections.Generic.List<Guid> { fixture.Root.Id };
            fixture.KnownOutcome = false;
            foreach (var number in new[] { 1.0000000000000002d, 1.0000000000000004d })
            {
                var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["counter"] = number } };
                var saved = await fixture.Provider.SaveHolonAsync(row, saveChildren: false);
                Assert.False(saved.IsError, saved.Message + saved.Exception); ids.Add(row.Id);
            }
            fixture.KnownOutcome = true;
            var keys = ids.ToArray();
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => keys.Contains(h.Id));
            var selected = source.OrderBy(h => CosmicQueryMetadata.Value<double>(h, "counter"))
                .Skip(1).Take(2).Select(h => CosmicQueryMetadata.Value<double>(h, "counter")).Result;
            var rows = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(rows.IsError, rows.Message + rows.Exception);
            Assert.Equal(new[] { 1.0000000000000002d, 1.0000000000000004d }, rows.Result.Providers.Single().Rows);
            var grouped = source.GroupBy(h => CosmicQueryMetadata.Value<double>(h, "counter"))
                .Select(g => new { Value = g.Key, Rows = g.LongCount() })
                .Where(g => g.Value > 1d).OrderBy(g => g.Value).Take(2).Result;
            var groups = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(grouped),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(groups.IsError, groups.Message + groups.Exception);
            Assert.Equal(new[] { 1.0000000000000002d, 1.0000000000000004d }, groups.Result.Providers.Single().Rows.Select(g => g.Value));
            var maximum = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Result.Max(h => CosmicQueryMetadata.Value<double>(h, "counter")).Result));
            Assert.False(maximum.IsError, maximum.Message + maximum.Exception); Assert.Equal(1.0000000000000004d, maximum.Result);
        }, width: 0, configure: root => root.MetaData["counter"] = 0d);

}
