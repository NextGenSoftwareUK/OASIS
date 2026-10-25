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
    public Task NativeSinglePredicatesUseOneExactDomain(int operation)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var members = new[] { 0.1f, float.Epsilon };
            Expression<Func<Holon, bool>> predicate = operation switch
            {
                0 => h => CosmicQueryMetadata.Value<float>(h, "price") == 0.1f,
                1 => h => CosmicQueryMetadata.Value<float>(h, "price") > 0f,
                2 => h => members.Contains(CosmicQueryMetadata.Value<float>(h, "price")),
                3 => h => CosmicQueryMetadata.Value<float?>(h, "optional") == null,
                4 => h => CosmicQueryMetadata.Value<float?>(h, "price") != 0.2f,
                5 => h => CosmicQueryMetadata.Value<float>(h, "price") == CosmicQueryMetadata.Value<float>(h, "reference"),
                6 => h => !(CosmicQueryMetadata.Value<float>(h, "price") == 0.2f),
                _ => h => CosmicQueryMetadata.Value<float>(h, "price") < 0.2f
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
            root.MetaData["price"] = 0.1f;
            root.MetaData["reference"] = 0.100f;
            // Null predicates require a present null member; absence is distinct.
            root.MetaData["optional"] = null;
        });

    [Theory]
    [InlineData("0")] [InlineData("-1E-45")]
    [InlineData("1E-45")] [InlineData("1.0000001")]
    [InlineData("-3.4028235E38")] [InlineData("3.4028235E38")]
    public Task NativeSinglePersistsAndQueriesExactSingleBoundary(string text)
    {
        var number = float.Parse(text, CultureInfo.InvariantCulture);
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var stored = await fixture.Records.Find(new BsonDocument("HolonId", id.ToString("D"))).SingleAsync();
            Assert.True(stored["MetaData"]["counter"].IsDouble);
            // Persisted numeric text preserves the original Single round trip.
            // Native normalization owns widening to the exact binary32 value.
            Assert.Equal(number, (float)stored["MetaData"]["counter"].AsDouble);
            var members = new[] { number };
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(row => row.Id == id
                && CosmicQueryMetadata.Value<float>(row, "counter") == number
                && members.Contains(CosmicQueryMetadata.Value<float>(row, "counter")))
                .OrderBy(row => CosmicQueryMetadata.Value<float>(row, "counter"));
            Assert.False(source.IsError, source.Message);
            var selected = source.Select(row => CosmicQueryMetadata.Value<float>(row, "counter"));
            Assert.False(selected.IsError, selected.Message);
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected.Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(number, Assert.Single(Assert.Single(result.Result.Providers).Rows));
            var grouped = source.GroupBy(row => CosmicQueryMetadata.Value<float>(row, "counter"))
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
    public Task NativeSingleRejectsWrongBsonDomainsWithoutPartialProjection(string kind)
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
            var selected = source.Select(row => CosmicQueryMetadata.Value<float?>(row, "counter")).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode);
            var matching = source.Where(row => CosmicQueryMetadata.Value<float>(row, "counter") == 0.1f).Result;
            var plan = CosmicQueryPlanner.Plan(matching, ProviderType.MongoDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities) });
            Assert.False(plan.IsError, plan.Message);
            var rows = await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(rows.IsError, rows.Message + rows.Exception); Assert.Empty(rows.Result);
        }, width: 0, configure: root => root.MetaData["counter"] = 0f);

    [Fact]
    public Task NativeNullableSingleGroupingCombinesEquivalentValues()
        => WithMongoGraph(async fixture =>
        {
            var ids = new System.Collections.Generic.List<Guid> { fixture.Root.Id };
            fixture.KnownOutcome = false;
            foreach (var value in new float?[] { 1.000f, null })
            {
                var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["price"] = value } };
                var saved = await fixture.Provider.SaveHolonAsync(row, saveChildren: false);
                Assert.False(saved.IsError, saved.Message + saved.Exception);
                ids.Add(row.Id);
            }
            fixture.KnownOutcome = true;
            var keys = ids.ToArray();
            var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => keys.Contains(h.Id))
                .GroupBy(h => CosmicQueryMetadata.Value<float?>(h, "price"))
                .Select(g => new { Price = g.Key, Rows = g.LongCount() })
                .Where(g => g.Price == 1f).OrderBy(g => g.Price).Take(1);
            Assert.False(query.IsError, query.Message);
            var result = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(query.Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var group = Assert.Single(result.Result.Providers.Single().Rows);
            Assert.Equal(1f, group.Price);
            Assert.Equal(2L, group.Rows);
        }, width: 0, configure: root => root.MetaData["price"] = 1f);

    [Fact]
    public Task NativeSingleOrderingGroupingAndExtremaDistinguishAdjacentFractions()
        => WithMongoGraph(async fixture =>
        {
            var ids = new System.Collections.Generic.List<Guid> { fixture.Root.Id };
            fixture.KnownOutcome = false;
            foreach (var number in new[] { 1.0000001f, 1.0000002f })
            {
                var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["counter"] = number } };
                var saved = await fixture.Provider.SaveHolonAsync(row, saveChildren: false);
                Assert.False(saved.IsError, saved.Message + saved.Exception); ids.Add(row.Id);
            }
            fixture.KnownOutcome = true;
            var keys = ids.ToArray();
            var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => keys.Contains(h.Id));
            var selected = source.OrderBy(h => CosmicQueryMetadata.Value<float>(h, "counter"))
                .Skip(1).Take(2).Select(h => CosmicQueryMetadata.Value<float>(h, "counter")).Result;
            var rows = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, selected),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(rows.IsError, rows.Message + rows.Exception);
            Assert.Equal(new[] { 1.0000001f, 1.0000002f }, rows.Result.Providers.Single().Rows);
            var grouped = source.GroupBy(h => CosmicQueryMetadata.Value<float>(h, "counter"))
                .Select(g => new { Value = g.Key, Rows = g.LongCount() })
                .Where(g => g.Value > 1f).OrderBy(g => g.Value).Take(2).Result;
            var groups = await Manager(fixture.Provider).ExecuteGroupedQueryAsync(GroupedPlan(grouped),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(groups.IsError, groups.Message + groups.Exception);
            Assert.Equal(new[] { 1.0000001f, 1.0000002f }, groups.Result.Providers.Single().Rows.Select(g => g.Value));
            var maximum = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Result.Max(h => CosmicQueryMetadata.Value<float>(h, "counter")).Result));
            Assert.False(maximum.IsError, maximum.Message + maximum.Exception); Assert.Equal(1.0000002f, maximum.Result);
        }, width: 0, configure: root => root.MetaData["counter"] = 0f);

    [Fact]
    public Task NativeSingleOriginalBsonDomainsAndPromotedLiteralsAgree()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var examples = new (BsonValue Stored, float Expected)[]
            {
                (new BsonInt32(16777217), 16777216f),
                (new BsonInt64(long.MaxValue), (float)long.MaxValue),
                (new BsonDecimal128(Decimal128.Parse("1.000000059604644775390625")), 1f),
                (new BsonDecimal128(Decimal128.Parse("1.000000059604644775390626")), 1.0000001f),
                (new BsonDouble(0.1d), 0.1f)
            };

            foreach (var example in examples)
            {
                var update = await fixture.Records.UpdateOneAsync(
                    new BsonDocument("HolonId", id.ToString("D")),
                    new BsonDocument("$set", new BsonDocument("MetaData.reading", example.Stored)));
                Assert.Equal(1, update.MatchedCount);

                var expected = example.Expected;
                var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => h.Id == id);
                var selected = source.Select(h => CosmicQueryMetadata.Value<float>(h, "reading"));
                Assert.False(selected.IsError, selected.Message);
                var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(
                    ProjectionPlan(fixture.Provider, selected.Result),
                    new ICosmicQueryStorageProvider[] { fixture.Provider });
                Assert.False(result.IsError, result.Message + result.Exception);
                Assert.Equal(expected, Assert.Single(result.Result.Providers.Single().Rows));

                // Widening changes the comparison space, not stored normalization.
                var members = new[] { (double)expected };
                var matching = source.Where(h => members.Contains(
                    (double)CosmicQueryMetadata.Value<float>(h, "reading")));
                Assert.False(matching.IsError, matching.Message);
                var plan = CosmicQueryPlanner.Plan(matching.Result, ProviderType.MongoDBOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities) });
                Assert.False(plan.IsError, plan.Message);
                var rows = await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.False(rows.IsError, rows.Message + rows.Exception);
                Assert.Equal(id, Assert.Single(rows.Result).Id);
            }

            // The last sample is 0.1f after normalization; 0.1d differs exactly.
            var unequal = CosmicQuery<Holon>.Create().Where(h => h.Id == id
                && (double)CosmicQueryMetadata.Value<float>(h, "reading") == 0.1d);
            Assert.False(unequal.IsError, unequal.Message);
            var noMatch = CosmicQueryPlanner.Plan(unequal.Result, ProviderType.MongoDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities) });
            Assert.False(noMatch.IsError, noMatch.Message);
            var excluded = await fixture.Provider.ExecuteCosmicQueryAsync(noMatch.Result);
            Assert.False(excluded.IsError, excluded.Message + excluded.Exception);
            Assert.Empty(excluded.Result);
        }, width: 0, configure: root => root.MetaData["reading"] = 0f);

}
