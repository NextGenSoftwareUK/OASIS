using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlVirtualProjectionTests
{
    [Fact]
    public void SharedProjectionKeepsExactTypedMetadataAndIdentity()
    {
        var request = Request();
        var projection = CqlV1Compiler.CompileVirtualProjection(request);
        var match = Assert.Single(projection.Matches);
        Assert.Equal("category", match.Field);
        Assert.Equal("coffee", match.Value);
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

    [Fact]
    public void MixedProjectionPreservesTheStoredBranchBudgetUntilAfterMerge()
    {
        var request = Request();
        request.Take = 1;
        request.Skip = 2;
        request.Descending = true;
        CqlV1Compiler.CompileVirtualProjection(request);
        var actor = Guid.NewGuid();
        var mixed = CqlV1Compiler.CompileProjection(request, actor, ProviderType.MongoDBOASIS,
            null, CqlVirtualProjection.MaximumRows);
        Assert.Equal(CqlVirtualProjection.MaximumRows, mixed.Source.Limit);
        Assert.Equal(0, mixed.Source.Offset);
        Assert.Equal(1, request.Take);
        Assert.Equal(2, request.Skip);

        request.VirtualMappingId = null;
        var ordinary = CqlV1Compiler.CompileProjection(request, actor, ProviderType.MongoDBOASIS);
        Assert.Equal(1, ordinary.Source.Limit);
        Assert.Equal(2, ordinary.Source.Offset);

        request.Skip = CqlVirtualProjection.MaximumRows * 2;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
    }

    [Theory]
    [InlineData("customer name", true)]
    [InlineData(" ", false)]
    [InlineData("OASIS.Native.key", false)]
    [InlineData("oasis.virtual.snapshot", false)]
    [InlineData("name\nline", false)]
    public void MappingAndQueryUseTheSameMetadataAliases(string field, bool allowed)
    {
        Assert.Equal(allowed, CqlV1Compiler.IsVirtualField(field));
        Assert.True(CqlV1Compiler.IsVirtualField(new string('a', 64)));
        Assert.False(CqlV1Compiler.IsVirtualField(new string('a', 65)));
    }

    [Fact]
    public void MixedProjectionPreservesAllTypedPredicatesAndSnapshotBindings()
    {
        var request = Request();
        request.Filters = request.Filters.Append(new CqlV1Filter { MetadataPath = new[] { "active" },
            Scalar = CqlV1Scalar.Boolean, Operator = CqlV1Operator.Equal,
            Value = JsonSerializer.SerializeToElement(true) }).ToArray();

        var projection = CqlV1Compiler.CompileVirtualProjection(request);
        Assert.Equal(new[] { "category", "active" }, projection.Matches.Select(match => match.Field));
        Assert.Equal(true, Assert.IsType<bool>(projection.Matches[1].Value));
        Assert.Equal(new[] { "name", "category", "active" },
            NativeDataController.VirtualSnapshotFields(projection.Matches.Select(match => match.Field), new[] { "name" }));
    }

    [Fact]
    public void SnapshotRetainsTheFilterFieldWithoutExceedingTheNativeProjectionLimit()
    {
        Assert.Equal(new[] { "name", "tier" }, NativeDataController.VirtualSnapshotFields(new[] { "tier" }, new[] { "name" }));
        Assert.Equal(new[] { "tier", "name" }, NativeDataController.VirtualSnapshotFields(new[] { "tier" }, new[] { "tier", "name" }));
        Assert.Null(NativeDataController.VirtualSnapshotFields(new[] { "tier" }, new[] { "name", "name" }));
        Assert.Null(NativeDataController.VirtualSnapshotFields(new[] { "tier" }, new[] { "" }));

        var maximum = Enumerable.Range(1, 32).Select(index => "field" + index).ToArray();
        Assert.Null(NativeDataController.VirtualSnapshotFields(new[] { "tier" }, maximum));
        Assert.Equal(maximum, NativeDataController.VirtualSnapshotFields(new[] { "field32" }, maximum));
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
