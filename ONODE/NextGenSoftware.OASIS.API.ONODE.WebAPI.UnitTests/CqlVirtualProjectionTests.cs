using System;
using System.Collections.Generic;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlVirtualProjectionTests
{
    [Fact]
    public void SharedProjectionKeepsExactTypedMetadataAndIdentity()
    {
        var request = Request();
        var projection = CqlV1Compiler.CompileVirtualProjection(request);
        Assert.Equal("category", projection.MatchField);
        Assert.Equal("coffee", projection.MatchValue);
        Assert.Equal(new[] { "stock" }, projection.ProjectedFields);

        var row = VirtualHolonRead.FromNative(Guid.NewGuid(), ProviderType.MongoDBOASIS,
            request.VirtualMappingId!.Value, 1, "mongodb://example.test/retail", "orders",
            DateTimeOffset.UtcNow, new NativeRecord("orders", "one",
                new Dictionary<string, object> { ["quantity"] = 9007199254740993L }),
            new Dictionary<string, string> { ["quantity"] = "stock" });
        var values = projection.Select(row);
        Assert.Equal(row.Id, Assert.IsType<Guid>(values[0]));
        Assert.Equal(9007199254740993L, Assert.IsType<long>(values[1]));
    }

    [Fact]
    public void MappingCannotBeDroppedBySingleSourceCompilerOrUnsupportedPredicate()
    {
        var request = Request();
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(request, Guid.NewGuid(), ProviderType.MongoDBOASIS));
        request.Filters[0].Operator = CqlV1Operator.Contains;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
    }

    private static CqlProjectionV1Request Request() => new()
    {
        VirtualMappingId = Guid.NewGuid(),
        OrderBy = "Id",
        Take = 10,
        Filters = new[]
        {
            new CqlV1Filter { MetadataPath = new[] { "category" }, Scalar = CqlV1Scalar.String,
                Operator = CqlV1Operator.Equal, Value = JsonSerializer.SerializeToElement("coffee") }
        },
        Columns = new[]
        {
            new CqlProjectionV1Column { Alias = "Id", Field = "Id" },
            new CqlProjectionV1Column { Alias = "Stock", MetadataPath = new[] { "stock" }, ValueType = "Int64" }
        }
    };
}
