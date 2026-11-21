using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlBooleanWhereV1Tests
{
    private static CqlWhereV1Predicate Leaf(string name) => new() {
        Kind = "Filter", Filter = new() { Field = "Name", Scalar = CqlV1Scalar.String,
            Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement(name) }
    };

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public void NestedBooleanAndRelatedIntentRetainsActorAndRootWindow(ProviderType provider)
    {
        var actor = Guid.NewGuid();
        var request = new CqlV1Request { Skip = 2, Take = 3, Where = new() {
            Kind = "And", Terms = new[] {
                new CqlWhereV1Predicate { Kind = "Or", Terms = new[] { Leaf("Coffee"), Leaf("Tea") } },
                new CqlWhereV1Predicate { Kind = "Not", Terms = new[] {
                    new CqlWhereV1Predicate { Kind = "Child", Match = Leaf("Hidden") }
                } }
            }
        }};
        var query = CqlV1Compiler.Compile(request, actor, provider);
        Assert.Equal(actor, query.ReadAccess.ActorId);
        Assert.Equal(2, query.Offset); Assert.Equal(3, query.Limit);
        var predicate = Assert.IsType<CosmicQueryLogical>(Assert.Single(query.BusinessPredicates));
        Assert.True(predicate.IsAnd);
        Assert.False(Assert.IsType<CosmicQueryLogical>(predicate.Left).IsAnd);
    }

    [Fact]
    public void CallerMutationCannotReplaceCapturedScalarOrProjectionWhere()
    {
        var node = Leaf("Coffee");
        var scalar = CqlRequestCapture.Capture(new CqlScalarV1Request { Where = node, Operation = "LongCount" });
        var projection = CqlRequestCapture.Capture(new CqlProjectionV1Request { Where = node,
            Columns = new[] { new CqlProjectionV1Column { Field = "Id", Alias = "Id" } } });
        node.Filter.Value = JsonSerializer.SerializeToElement("Tea");
        node.Kind = "Unknown";
        Assert.Equal("Filter", scalar.Where.Kind);
        Assert.Equal("Coffee", scalar.Where.Filter.Value.GetString());
        Assert.Equal("Coffee", projection.Where.Filter.Value.GetString());
        var actor = Guid.NewGuid();
        var compiled = CqlV1Compiler.CompileScalarSource(scalar, actor, ProviderType.Neo4jOASIS);
        Assert.Equal(actor, compiled.ReadAccess.ActorId);
        Assert.Single(compiled.BusinessPredicates);
    }

    [Fact]
    public void SharedBudgetsCyclesAndMixedNodeShapesRefuseBeforeExecution()
    {
        var actor = Guid.NewGuid();
        var request = new CqlV1Request {
            Filters = Enumerable.Range(0,11).Select(_ => Leaf("A").Filter).ToArray(),
            Where = new() { Kind = "Or", Terms = new[] { Leaf("B"), Leaf("C") } }
        };
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(request, actor, ProviderType.MongoDBOASIS));
        request.Filters = Array.Empty<CqlV1Filter>();
        request.Where = new() { Kind = "Not" };
        request.Where.Terms = new[] { request.Where };
        Assert.Throws<ArgumentException>(() => CqlRequestCapture.Capture(request));
        request.Where = new() { Kind = "Or", Terms = new[] { Leaf("A"), Leaf("B") }, Filter = Leaf("C").Filter };
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.Compile(request, actor, ProviderType.SQLLiteDBOASIS));
    }
}
