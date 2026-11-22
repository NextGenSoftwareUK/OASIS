using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlUnfilteredMappingTests
{
    private static CqlProjectionV1Request Request() => new() {
        VirtualMappingId = Guid.NewGuid(), OrderBy = "Id", Take = 3, Skip = 2,
        Filters = Array.Empty<CqlV1Filter>(),
        Columns = new[] {
            new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
            new CqlProjectionV1Column { MetadataPath = new[] { "name" }, ValueType = "String", Alias = "Name" }
        }
    };

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public void BoundedUnfilteredMixedSourceRetainsActorAndCompleteBranchWindow(ProviderType provider)
    {
        var request = Request();
        var projection = CqlV1Compiler.CompileVirtualProjection(request);
        Assert.Empty(projection.Matches);
        Assert.Equal("name", Assert.Single(projection.Columns).Field);
        var actor = Guid.NewGuid();
        var compiled = CqlV1Compiler.CompileProjection(request, actor, provider, null, CqlVirtualProjection.MaximumRows);
        Assert.Equal(actor, compiled.Source.ReadAccess.ActorId);
        Assert.Equal(0, compiled.Source.Offset);
        Assert.Equal(CqlVirtualProjection.MaximumRows, compiled.Source.Limit);
        Assert.Equal(2, request.Skip);
        Assert.Equal(3, request.Take);
    }

    [Fact]
    public void EmptyPredicateIsExplicitlyProjectionOnlyAndCannotHideMalformedInputs()
    {
        Assert.Empty(NativeRecordFilter.CaptureForProjection(Array.Empty<NativeRecordFilter>()));
        Assert.Throws<ArgumentException>(() => NativeRecordFilter.Capture(Array.Empty<NativeRecordFilter>()));
        Assert.Throws<ArgumentException>(() => NativeRecordFilter.CaptureForProjection(null));
        Assert.Throws<ArgumentException>(() => NativeRecordFilter.CaptureForProjection(new NativeRecordFilter[] { null }));
        Assert.Throws<ArgumentException>(() => NativeRecordFilter.CaptureForProjection(new NativeRecordFilter[9]));
    }

    [Fact]
    public void UnfilteredMappingRetainsWindowIdentityAndUnsupportedWhereChecks()
    {
        var request = Request();
        request.Take = CqlVirtualProjection.MaximumRows + 1;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
        request = Request(); request.Columns[0].Field = "Name";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
        request = Request(); request.Where = new CqlWhereV1Predicate { Kind = "Not" };
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
        request = Request(); request.Filters = null;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
    }
}
