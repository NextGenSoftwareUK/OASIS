using System;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlProjectionCompilerTests
{
    private static CqlProjectionV1Request Request(params CqlProjectionV1Column[] columns) => new() { Columns = columns };
    private static CqlProjectionV1Column Name(string alias = "name") => new() { Alias = alias, Field = "Name" };

    [Fact]
    public void ProjectionSourceRetainsAuthenticatedAccessDefaultPageAndOrdering()
    {
        var actor = Guid.NewGuid();
        var query = CqlV1Compiler.CompileProjection(Request(Name()), actor, ProviderType.Neo4jOASIS);
        Assert.Equal(actor, query.Source.ReadAccess.ActorId);
        Assert.Equal(25, query.Source.Limit);
        Assert.Equal(2, query.Source.Orderings.Count);
        Assert.Equal(typeof(string), query.Columns.Single().Path.ValueType);
        Assert.Equal("name", query.Columns.Single().Alias);
    }

    [Theory]
    [InlineData("Id", typeof(Guid))] [InlineData("Name", typeof(string))]
    [InlineData("Description", typeof(string))] [InlineData("Version", typeof(int))]
    [InlineData("HolonType", typeof(HolonType))] [InlineData("IsActive", typeof(bool))]
    [InlineData("IsPublic", typeof(bool))] [InlineData("ParentHolonId", typeof(Guid))]
    [InlineData("VersionId", typeof(Guid))] [InlineData("PreviousVersionId", typeof(Guid))]
    [InlineData("CreatedByAvatarId", typeof(Guid))] [InlineData("ModifiedByAvatarId", typeof(Guid))]
    public void CanonicalColumnTypesAreInferredFromTheContract(string field, Type expected)
    {
        var query = CqlV1Compiler.CompileProjection(Request(new CqlProjectionV1Column { Alias = "value", Field = field }), Guid.NewGuid(), null);
        Assert.Equal(expected, query.Columns.Single().Path.ValueType);
        Assert.False(query.Columns.Single().Path.IsMetadata);
    }

    [Theory]
    [InlineData("Boolean", typeof(bool))] [InlineData("Int32", typeof(int))]
    [InlineData("Int64", typeof(long))] [InlineData("Guid", typeof(Guid))]
    [InlineData("DateTime", typeof(DateTime))] [InlineData("HolonType", typeof(HolonType))]
    [InlineData("ProviderType", typeof(ProviderType))] [InlineData("Byte", typeof(byte))]
    [InlineData("SByte", typeof(sbyte))] [InlineData("Int16", typeof(short))]
    [InlineData("UInt16", typeof(ushort))] [InlineData("UInt32", typeof(uint))]
    public void MetadataUsesTheExplicitExactValueDomainAndNullableLift(string type, Type expected)
    {
        foreach (var nullable in new[] { false, true })
        {
            var path = new[] { "nested", "stock" };
            var request = Request(new CqlProjectionV1Column { Alias = "value", MetadataPath = path, ValueType = type, Nullable = nullable });
            var query = CqlV1Compiler.CompileProjection(request, Guid.NewGuid(), null);
            path[0] = "mutated";
            Assert.Equal(nullable ? typeof(Nullable<>).MakeGenericType(expected) : expected, query.Columns.Single().Path.ValueType);
            Assert.Equal(new[] { "nested", "stock" }, query.Columns.Single().Path.Segments);
        }
    }

    [Fact]
    public void StringMetadataRetainsNullableStringSemantics()
    {
        var column = new CqlProjectionV1Column { Alias = "value", MetadataPath = new[] { "note" }, ValueType = "String" };
        Assert.Equal(typeof(string), CqlV1Compiler.CompileProjection(Request(column), Guid.NewGuid(), null).Columns.Single().Path.ValueType);
        column.Nullable = false;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(Request(column), Guid.NewGuid(), null));
    }

    [Fact]
    public void AliasShapeAndSchemaBoundsRejectAmbiguousRequests()
    {
        foreach (var alias in new[] { "", "1name", "bad-name", "bad.name", "name\n", new string('x', 65) })
            Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(Request(Name(alias)), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(Request(Name(), Name()), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(Request(), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(Request(Enumerable.Range(0, 33).Select(i => Name("name" + i)).ToArray()), Guid.NewGuid(), null));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(Request(null), Guid.NewGuid(), null));
    }

    [Fact]
    public void FieldPathAndTypesCannotBorrowOrOverrideAnotherContract()
    {
        var invalid = new[]
        {
            new CqlProjectionV1Column { Alias = "x", Field = "Name", ValueType = "Int64" },
            new CqlProjectionV1Column { Alias = "x", Field = "Name", Nullable = true },
            new CqlProjectionV1Column { Alias = "x", Field = "SnapshotJson" },
            new CqlProjectionV1Column { Alias = "x", Field = "Name", MetadataPath = new[] { "name" }, ValueType = "String" },
            new CqlProjectionV1Column { Alias = "x", MetadataPath = Array.Empty<string>(), ValueType = "String" },
            new CqlProjectionV1Column { Alias = "x", MetadataPath = new[] { "value" }, ValueType = "System.Int64" },
            new CqlProjectionV1Column { Alias = "x", MetadataPath = new[] { "value" }, ValueType = "Double" }
        };
        foreach (var column in invalid)
            Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(Request(column), Guid.NewGuid(), null));
    }

    [Fact]
    public void PublicOutputWindowAndAuthenticatedActorAreRequired()
    {
        var request = Request(Name());
        foreach (var take in new[] { -1, 101 })
        {
            request.Take = take;
            Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(request, Guid.NewGuid(), null));
        }
        request.Take = 0;
        Assert.Equal(0, CqlV1Compiler.CompileProjection(request, Guid.NewGuid(), null).Source.Limit);
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(request, Guid.Empty, null));
    }

    [Fact]
    public void UnknownTransportMembersCannotSmuggleExpressionsOrAnActor()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlProjectionV1Request>("{\"ActorId\":\"injected\"}"));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlProjectionV1Request>("{\"Columns\":[{\"Expression\":\"arbitrary()\"}]}"));
    }
}
