using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlMembershipWireTests
{
    private static CqlV1Request Request(string json) => new()
    {
        Provider = "ActivityPubOASIS", OrderBy = "None", Take = 25,
        Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "MastodonStatusId" },
            Scalar = CqlV1Scalar.String, Operator = CqlV1Operator.In, Value = JsonDocument.Parse(json).RootElement.Clone() } }
    };

    private static CosmicQuery<Holon> Compile(CqlV1Request request, ProviderType provider = ProviderType.ActivityPubOASIS)
        => CqlV1Compiler.Compile(request, Guid.NewGuid(), provider);

    [Fact]
    public void HttpJsonBecomesOneNativeKeySetPlanRatherThanAnOrGroupOrPointFanout()
    {
        const string json = """
            {"provider":"ActivityPubOASIS","orderBy":"None","take":25,
             "filters":[{"metadataPath":["MastodonStatusId"],"scalar":"String","operator":"In","value":["101","102","103"]}]}
            """;
        var request = JsonSerializer.Deserialize<CqlV1Request>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var query = Compile(request);
        var membership = Assert.IsType<CosmicQueryMembership>(Assert.Single(query.BusinessPredicates));
        Assert.Equal(new[] { "101", "102", "103" }, membership.Values.Select(value => (string)value.Value));
        var capability = CosmicQueryCapabilities.ForStringMetadataKeyLookup("MastodonStatusId", 128, 40);
        Assert.True(capability.StringKeySetLookup.IsKeySetQuery(query));
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ActivityPubOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ActivityPubOASIS, capability) });
        Assert.False(plan.IsError, plan.Message);
        Assert.Equal(new[] { "101", "102", "103" }, capability.StringKeySetLookup.SelectKeys(query).Result);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("\"101\"")]
    [InlineData("null")]
    [InlineData("[\"101\",null]")]
    [InlineData("[\"101\",102]")]
    [InlineData("[[\"101\"]]")]
    public void InvalidMembershipDoesNotProduceAQuery(string json)
        => Assert.Throws<ArgumentException>(() => Compile(Request(json)));

    [Fact]
    public void HttpBoundAndNativeBoundRemainIndependentAndFailBeforeDispatch()
    {
        Assert.Throws<ArgumentException>(() => Compile(Request(JsonSerializer.Serialize(Enumerable.Range(1, 101).Select(value => value.ToString())))));
        var query = Compile(Request(JsonSerializer.Serialize(Enumerable.Range(1, 41).Select(value => value.ToString()))));
        var capability = CosmicQueryCapabilities.ForStringMetadataKeyLookup("MastodonStatusId", 128, 40);
        Assert.True(CosmicQueryPlanner.Plan(query, ProviderType.ActivityPubOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ActivityPubOASIS, capability) }).IsError);
    }

    [Fact]
    public void PointOnlyAdapterRejectsMembershipInsteadOfSplittingItIntoQueries()
    {
        var query = Compile(Request("[\"101\",\"102\"]"));
        var capability = CosmicQueryCapabilities.ForStringMetadataPointLookup("MastodonStatusId");
        Assert.True(CosmicQueryPlanner.Plan(query, ProviderType.ActivityPubOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ActivityPubOASIS, capability) }).IsError);
    }

    [Theory]
    [InlineData(CqlV1Scalar.UInt64, "[\"18446744073709551615\"]", typeof(ulong))]
    [InlineData(CqlV1Scalar.Decimal, "[\"79228162514264337593543950335\"]", typeof(decimal))]
    [InlineData(CqlV1Scalar.Double, "[\"9007199254740992\"]", typeof(double))]
    [InlineData(CqlV1Scalar.Single, "[\"16777216\"]", typeof(float))]
    [InlineData(CqlV1Scalar.Integer, "[9223372036854775807]", typeof(long))]
    [InlineData(CqlV1Scalar.Boolean, "[true,false]", typeof(bool))]
    [InlineData(CqlV1Scalar.Guid, "[\"c0172431-2542-4960-bf2d-0cf5fc8389a4\"]", typeof(Guid))]
    [InlineData(CqlV1Scalar.DateTime, "[\"2026-10-31T01:02:03.1234567Z\"]", typeof(DateTime))]
    public void ExactScalarContractsAreRetainedForEveryMembershipValue(CqlV1Scalar scalar, string json, Type expected)
    {
        var request = Request(json); request.Provider = "MongoDBOASIS"; request.OrderBy = "Name";
        request.Filters[0].Scalar = scalar;
        request.Filters[0].MetadataPath = new[] { "sample" };
        var membership = Assert.IsType<CosmicQueryMembership>(Assert.Single(Compile(request, ProviderType.MongoDBOASIS).BusinessPredicates));
        Assert.Equal(expected, membership.Path.ValueType);
        // Core normalizes UInt64 to exact Decimal and Single to exact Double;
        // the path retains the declared storage domain and its native type guard.
        var literalType = expected == typeof(ulong) ? typeof(decimal) : expected == typeof(float) ? typeof(double) : expected;
        Assert.All(membership.Values, value => Assert.IsType(literalType, value.Value));
        if (scalar == CqlV1Scalar.UInt64) Assert.Equal((decimal)ulong.MaxValue, membership.Values[0].Value);
        if (scalar == CqlV1Scalar.Single) Assert.Equal(16777216d, membership.Values[0].Value);
        if (scalar == CqlV1Scalar.Decimal) Assert.Equal(decimal.MaxValue, membership.Values[0].Value);
        if (scalar == CqlV1Scalar.DateTime)
            Assert.Equal(DateTime.ParseExact("2026-10-31T01:02:03.1234567Z", "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal), membership.Values[0].Value);
    }

    [Theory]
    [InlineData(CqlV1Scalar.UInt64, "[18446744073709551615]")]
    [InlineData(CqlV1Scalar.Double, "[\"9007199254740993\"]")]
    [InlineData(CqlV1Scalar.Single, "[\"16777217\"]")]
    [InlineData(CqlV1Scalar.Decimal, "[\"1.00000000000000000000000000001\"]")]
    [InlineData(CqlV1Scalar.DateTime, "[\"2026-10-31T01:02:03.12345678Z\"]")]
    [InlineData(CqlV1Scalar.Integer, "[1,1.5]")]
    public void LossyOrWronglyEncodedMembersRejectTheWholeQuery(CqlV1Scalar scalar, string json)
    {
        var request = Request(json); request.Provider = "MongoDBOASIS"; request.OrderBy = "Name";
        request.Filters[0].Scalar = scalar;
        Assert.Throws<ArgumentException>(() => Compile(request, ProviderType.MongoDBOASIS));
    }

    [Fact]
    public void CanonicalGuidMembershipUsesTheDeclaredPropertyType()
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        var request = Request(JsonSerializer.Serialize(new[] { first, second }));
        request.Provider = "MongoDBOASIS"; request.OrderBy = "Name";
        request.Filters[0].MetadataPath = null; request.Filters[0].Field = "Id";
        var membership = Assert.IsType<CosmicQueryMembership>(Assert.Single(Compile(request, ProviderType.MongoDBOASIS).BusinessPredicates));
        Assert.Equal(new[] { first, second }, membership.Values.Select(value => (Guid)value.Value));
    }

    [Fact]
    public void ExistingScalarComparisonUsesTheSameLiteralContract()
    {
        var request = Request("\"101\""); request.Filters[0].Operator = CqlV1Operator.Equal;
        Assert.IsType<CosmicQueryComparison>(Assert.Single(Compile(request).BusinessPredicates));
        request.Filters[0].Value = JsonSerializer.SerializeToElement(new[] { "101" });
        Assert.Throws<ArgumentException>(() => Compile(request));
    }
}
