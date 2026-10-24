using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

internal static class CqlDateWireFixtures
{
    internal static CqlV1Request Source(JsonElement value) => new()
    {
        Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "stamp" }, Scalar = CqlV1Scalar.DateTime,
            Operator = CqlV1Operator.Equal, Value = value } }
    };
    internal static CqlGroupedV1Request Group(JsonElement value, bool nullable = false) => new()
    {
        Keys = new[] { new CqlProjectionV1Column { Alias = "stamp", MetadataPath = new[] { "stamp" },
            ValueType = "DateTime", Nullable = nullable } },
        Columns = new[] { new CqlGroupedV1Column { Alias = "Stamp", KeyAlias = "stamp" } },
        ResultFilters = new[] { new CqlGroupedV1Filter { Alias = "Stamp", Operator = CqlV1Operator.Equal, Value = value } }
    };
}

public sealed class CqlUtcDateWireTests
{
    [Theory]
    [InlineData("2026-10-24T00:00:00Z", 0)]
    [InlineData("2026-10-24T00:00:00.1Z", 1000000)]
    [InlineData("2026-10-24T00:00:00.12Z", 1200000)]
    [InlineData("2026-10-24T00:00:00.123Z", 1230000)]
    [InlineData("2026-10-24T00:00:00.1234Z", 1234000)]
    [InlineData("2026-10-24T00:00:00.12345Z", 1234500)]
    [InlineData("2026-10-24T00:00:00.123456Z", 1234560)]
    [InlineData("2026-10-24T00:00:00.1234567Z", 1234567)]
    public void SourceUtcFormatsRetainExactTicksAndKind(string text, int ticks)
    {
        var query = CqlV1Compiler.Compile(CqlDateWireFixtures.Source(JsonSerializer.SerializeToElement(text)), Guid.NewGuid(), null);
        Assert.Equal(2, query.Predicates.Count);
        Assert.IsType<CosmicQueryLogical>(query.Predicates[0]);
        var comparison = Assert.Single(query.Predicates.OfType<CosmicQueryComparison>().Where(value => value.Path.IsMetadata));
        var value = Assert.IsType<DateTime>(comparison.Literal.Value);
        Assert.Equal(new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc).AddTicks(ticks), value);
        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(typeof(DateTime), comparison.Path.ValueType);
        Assert.NotNull(query.ReadAccess);
    }

    [Theory]
    [InlineData("0001-01-01T00:00:00Z", false)]
    [InlineData("9999-12-31T23:59:59.9999999Z", true)]
    public void UtcWireRetainsTheFullDateTimeCalendar(string text, bool maximum)
    {
        var query = CqlV1Compiler.Compile(CqlDateWireFixtures.Source(JsonSerializer.SerializeToElement(text)), Guid.NewGuid(), null);
        Assert.Equal(2, query.Predicates.Count);
        Assert.IsType<CosmicQueryLogical>(query.Predicates[0]);
        var comparison = Assert.Single(query.Predicates.OfType<CosmicQueryComparison>().Where(value => value.Path.IsMetadata));
        Assert.Equal(maximum ? DateTime.MaxValue.Ticks : DateTime.MinValue.Ticks, ((DateTime)comparison.Literal.Value).Ticks);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void GroupedAliasesUseTheSameUtcParserAndPreserveNullableType(bool nullable)
    {
        var request = CqlDateWireFixtures.Group(JsonSerializer.SerializeToElement("2026-10-24T00:00:00.0000001Z"), nullable);
        var query = CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null);
        var comparison = Assert.IsType<CosmicQueryComparison>(Assert.Single(query.ResultPredicates));
        var date = Assert.IsType<DateTime>(comparison.Literal.Value);
        Assert.Equal(1, date.Ticks % TimeSpan.TicksPerSecond);
        Assert.Equal(DateTimeKind.Utc, date.Kind);
        Assert.Equal(nullable ? typeof(DateTime?) : typeof(DateTime), comparison.Path.ValueType);
    }

    [Theory]
    [InlineData("")] [InlineData("2026-02-29T00:00:00Z")]
    [InlineData("1900-02-29T00:00:00Z")] [InlineData("2026-04-31T00:00:00Z")]
    [InlineData("2026-10-24T24:00:00Z")] [InlineData("2026-10-24T00:00:60Z")]
    [InlineData("2026-10-24T00:00:00")]
    [InlineData("2026-10-24T00:00:00+00:00")] [InlineData("2026-10-24T00:00:00-05:00")]
    [InlineData("2026-10-24T00:00:00z")] [InlineData("2026-10-24t00:00:00Z")]
    [InlineData("2026-10-24T00:00:00.12345678Z")]
    [InlineData("2026-10-24T00:00:00Z\n")]
    public void InvalidCalendarOffsetOrPrecisionCannotBecomeSourceOrGroupedIntent(string text)
    {
        var value = JsonSerializer.SerializeToElement(text);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(CqlDateWireFixtures.Source(value), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(CqlDateWireFixtures.Group(value), Guid.NewGuid(), null));
    }

    [Theory]
    [InlineData("123")] [InlineData("true")] [InlineData("null")]
    public void NonStringDateValuesAreRejectedAtBothWireBoundaries(string json)
    {
        var value = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(CqlDateWireFixtures.Source(value), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(CqlDateWireFixtures.Group(value), Guid.NewGuid(), null));
    }
}

public partial class CqlV1Web4Tests
{
    [Fact]
    public async Task InvalidUtcWireDoesNotResolveOrDispatchAProvider()
    {
        var provider = ProviderType.Neo4jOASIS;
        var source = new Source(provider); var admitted = Admission(provider);
        var value = JsonSerializer.SerializeToElement("2026-10-24T00:00:00+00:00");
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, CqlDateWireFixtures.Source(value), source));
        Assert.Equal(400, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.Equal(0, source.Lookups); Assert.Equal(0, source.Managers); Assert.Null(source.Observed);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ValidUtcWireCannotGrantAMissingActualDateCapability(ProviderType provider)
    {
        var source = new Source(provider); var admitted = Admission(provider);
        var request = CqlDateWireFixtures.Source(JsonSerializer.SerializeToElement("2026-10-24T00:00:00.0000001Z"));
        var response = await admitted.Policy.RunAsync(() => CqlV1Endpoint.ExecuteAsync(admitted.Http, request, source));
        Assert.Equal(400, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.Null(source.Observed);
    }
}
