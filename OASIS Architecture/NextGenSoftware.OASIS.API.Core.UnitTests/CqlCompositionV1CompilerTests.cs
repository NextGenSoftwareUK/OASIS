using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlCompositionV1CompilerTests
{
    private static CqlCompositionV1Request Request() => new()
    {
        Source = new() { Skip = 1, Take = 2, Columns = new[]
        {
            new CqlProjectionV1Column { Alias = "name", Field = "Name" },
            new CqlProjectionV1Column { Alias = "version", Field = "Version" }
        } },
        Stages = new[] { new CqlCompositionV1Stage { Operation = "Take", Count = 1 } }
    };
    private static CqlGroupedV1Filter Filter(string alias = "version")
        => new() { Alias = alias, Operator = CqlV1Operator.GreaterThan, Value = JsonSerializer.SerializeToElement(1) };
    private static CqlCompositionV1Stage Where() => new() { Operation = "Where", Filters = new[] { Filter() } };
    private static CqlCompositionV1Stage Sort() => new() { Operation = "OrderBy", Order = new[] { new CqlGroupedV1Ordering { Alias = "name" } } };

    [Fact]
    public void OrderedStagesPreserveSourceWindowActorAndCapturedLiterals()
    {
        var actor = Guid.NewGuid(); var request = Request(); var where = Where();
        request.Stages = new[] { request.Stages[0], where, Sort(), new CqlCompositionV1Stage { Operation = "Skip", Count = 0 } };
        var query = CqlV1Compiler.CompileComposition(request, actor, ProviderType.MongoDBOASIS);
        Assert.Equal(actor, query.Source.ReadAccess.ActorId); Assert.Equal(1, query.Source.Offset); Assert.Equal(2, query.Source.Limit);
        Assert.Collection(query.ResultStages, stage => Assert.IsType<CosmicSelectedWindowStage>(stage),
            stage => Assert.IsType<CosmicSelectedFilterStage>(stage), stage => Assert.IsType<CosmicSelectedOrderStage>(stage),
            stage => Assert.IsType<CosmicSelectedWindowStage>(stage));
        where.Filters[0].Value = JsonSerializer.SerializeToElement(99);
        var predicate = Assert.IsType<CosmicQueryComparison>(Assert.IsType<CosmicSelectedFilterStage>(query.ResultStages[1]).Predicate);
        Assert.Equal(1L, predicate.Literal.Value);
    }

    [Fact]
    public void SeparateOrderingStagesDoNotBecomeThenByAcrossWindows()
    {
        var request = Request(); request.Stages = new[] { Sort(), request.Stages[0], Sort() };
        var query = CqlV1Compiler.CompileComposition(request, Guid.NewGuid(), ProviderType.Neo4jOASIS);
        Assert.Equal(3, query.ResultStages.Count);
        Assert.All(query.ResultStages.OfType<CosmicSelectedOrderStage>(), stage => Assert.Single(stage.Orderings));
    }

    [Fact]
    public void SelectedAndOrGroupsShareOneSafeParameter()
    {
        var request = Request(); request.Stages = new[] { new CqlCompositionV1Stage
        {
            Operation = "Where", Filters = new[] { Filter() }, AnyOf = new[]
            {
                new CqlGroupedV1Filter { Alias = "name", Value = JsonSerializer.SerializeToElement("coffee") },
                new CqlGroupedV1Filter { Alias = "name", Value = JsonSerializer.SerializeToElement("tea") }
            }
        } };
        var query = CqlV1Compiler.CompileComposition(request, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS);
        var and = Assert.IsType<CosmicQueryLogical>(Assert.IsType<CosmicSelectedFilterStage>(query.ResultStages.Single()).Predicate);
        Assert.True(and.IsAnd); Assert.False(Assert.IsType<CosmicQueryLogical>(and.Right).IsAnd);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    [InlineData(12)] [InlineData(13)] [InlineData(14)] [InlineData(15)] [InlineData(16)] [InlineData(17)]
    [InlineData(18)] [InlineData(19)] [InlineData(20)] [InlineData(21)] [InlineData(22)] [InlineData(23)]
    [InlineData(24)] [InlineData(25)]
    public void InvalidOrAmbiguousDescriptionsAreRejected(int fault)
    {
        var request = Request(); var where = Where(); var sort = Sort();
        switch (fault)
        {
            case 0: request.Source = null!; break;
            case 1: request.Source.Provider = "MongoDBOASIS"; break;
            case 2: request.Source.Graph = new(); break;
            case 3: request.Stages = Array.Empty<CqlCompositionV1Stage>(); break;
            case 4: request.Stages = Enumerable.Repeat(request.Stages[0], 17).ToArray(); break;
            case 5: request.Stages[0] = null!; break;
            case 6: request.Stages[0].Operation = "thenby"; break;
            case 7: request.Stages[0].Count = null; break;
            case 8: request.Stages[0].Count = 101; break;
            case 9: request.Stages[0] = new() { Operation = "Skip", Count = -1 }; break;
            case 10: where.Filters = Array.Empty<CqlGroupedV1Filter>(); break;
            case 11: where.Filters[0].Alias = "unselected"; break;
            case 12: where.Count = 0; break;
            case 13: where.Filters[0].Operator = CqlV1Operator.Exists; break;
            case 14: where.Filters[0].Value = JsonSerializer.SerializeToElement("1"); break;
            case 15: sort.Order = Array.Empty<CqlGroupedV1Ordering>(); break;
            case 16: sort.Order[0] = null!; break;
            case 17: sort.Order[0].Alias = "unselected"; break;
            case 18: sort.Count = 0; break;
            case 19: sort.Order = Enumerable.Repeat(sort.Order[0], 9).ToArray(); break;
            case 20: where.Filters = Enumerable.Repeat(Filter(), 33).ToArray(); break;
            case 21:
                where.Filters = Enumerable.Repeat(Filter(), 17).ToArray();
                request.Stages = new[] { where, where }; break;
            case 22:
                sort.Order = Enumerable.Repeat(sort.Order[0], 8).ToArray();
                request.Stages = Enumerable.Repeat(sort, 5).ToArray(); break;
            case 23: request.Source.Take = 101; break;
            case 24: where.Filters[0].Operator = CqlV1Operator.IsNull; where.Filters[0].Value = default; break;
            case 25: where.Filters[0].Alias = "Version"; break;
        }
        if (fault is >= 10 and <= 14 or 20 or 24 or 25) request.Stages = new[] { where };
        if (fault is >= 15 and <= 19) request.Stages = new[] { sort };
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileComposition(request, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS));
    }

    [Theory]
    [InlineData("Int64", "\"9007199254740993\"")]
    [InlineData("UInt64", "\"18446744073709551615\"")]
    [InlineData("Decimal", "\"0.0000000000000000000000000001\"")]
    [InlineData("Double", "\"1.0000000000000002\"")]
    [InlineData("Single", "\"1.0000001\"")]
    [InlineData("Guid", "\"10000000-0000-0000-0000-000000000001\"")]
    [InlineData("DateTime", "\"2026-10-25T12:00:00.0000001Z\"")]
    [InlineData("String", "\"coffee\"")]
    [InlineData("Boolean", "true")]
    [InlineData("Byte", "255")] [InlineData("SByte", "-128")]
    [InlineData("Int16", "-32768")] [InlineData("UInt16", "65535")] [InlineData("UInt32", "4294967295")]
    [InlineData("HolonType", "\"Quest\"")] [InlineData("ProviderType", "\"Neo4jOASIS\"")]
    public void ExactSelectedLeafDomainsBindWithoutLossOrDelegateExecution(string type, string json)
    {
        var request = Request(); request.Source.Columns = new[] { new CqlProjectionV1Column { Alias = "value", MetadataPath = new[] { "value" }, ValueType = type } };
        request.Stages = new[]
        {
            new CqlCompositionV1Stage { Operation = "Where", Filters = new[] { new CqlGroupedV1Filter { Alias = "value", Value = JsonSerializer.Deserialize<JsonElement>(json) } } },
            new CqlCompositionV1Stage { Operation = "OrderBy", Order = new[] { new CqlGroupedV1Ordering { Alias = "value" } } }
        };
        var query = CqlV1Compiler.CompileComposition(request, Guid.NewGuid(), ProviderType.Neo4jOASIS);
        Assert.Equal(2, query.ResultStages.Count); Assert.Single(query.Columns);
    }

    [Theory]
    [InlineData("{\"actor\":\"foreign\"}")]
    [InlineData("{\"stages\":[{\"operation\":\"Take\",\"count\":1,\"sql\":\"arbitrary\"}]}")]
    public void UnknownWireMembersCannotEnterCompilation(string json)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlCompositionV1Request>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }));
}
