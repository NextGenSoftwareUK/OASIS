using System.Text.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlStringLengthWireTests_MappedProjection
{
    private static CqlProjectionV1Request Request() => new()
    {
        VirtualMappingId = Guid.NewGuid(), OrderBy = "Id", Take = 5,
        Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "display" },
            Scalar = CqlV1Scalar.String, Operator = CqlV1Operator.Equal,
            Value = JsonSerializer.SerializeToElement("😀") } },
        Columns = new[] {
            new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
            new CqlProjectionV1Column { MetadataPath = new[] { "display" }, ValueType = "String", Alias = "Text" },
            new CqlProjectionV1Column { MetadataPath = new[] { "display" }, ValueType = "String", Alias = "Units",
                Function = CqlV1Function.Utf16Length, Nullable = true },
            new CqlProjectionV1Column { MetadataPath = new[] { "display" }, ValueType = "String", Alias = "Again",
                Function = CqlV1Function.Utf16Length, Nullable = true }
        }
    };

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public void MixedStoredAndVirtualColumnsShareInputAndOutputDomains(ProviderType provider)
    {
        var request = Request();
        var projection = CqlV1Compiler.CompileVirtualProjection(request);
        var stored = CqlV1Compiler.CompileProjection(request, Guid.NewGuid(), provider, null, 100);
        Assert.Equal(new[] { "display" }, projection.ProjectedFields);
        Assert.Equal(new[] { "Text", "Units", "Again" }, projection.Columns.Select(column => column.Alias));
        Assert.All(projection.Columns, column => Assert.Equal(typeof(string), column.InputType));
        Assert.Equal(typeof(string), projection.Columns[0].ValueType);
        Assert.Equal(typeof(int), projection.Columns[1].ValueType);
        Assert.Equal(NativeRecordFunction.Utf16Length, projection.Columns[2].Function);
        Assert.Equal(typeof(string), stored.Columns[2].Path.ValueType);
        Assert.Equal(typeof(int?), stored.Columns[2].ValueType);
        Assert.Equal(100, stored.Source.Limit);
    }

    [Fact]
    public void MixedFunctionsCannotRelabelTheirInputOrDuplicateAnAlias()
    {
        var request = Request();
        request.Columns[2].ValueType = "Int32";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
        request = Request(); request.Columns[2].Alias = "Text";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
        request = Request(); request.Columns[2].Alias = "Id";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
        request = Request(); request.Columns[0].Function = CqlV1Function.Utf16Length;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
        request = Request(); request.Columns[2].Function = (CqlV1Function)999;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(request));
    }

    private static (VirtualHolonMapping Mapping, Mock<INativeDataProjectionQueryProvider> Reader, Guid Owner) Fixture(
        IReadOnlyList<NativeProjectionRow> rows)
    {
        var owner = Guid.NewGuid();
        var reader = new Mock<INativeDataProjectionQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(value => value.NativeDataProviderType).Returns(ProviderType.MongoDBOASIS);
        reader.Setup(value => value.DescribeVirtualQueryConnection(null)).Returns("test-native://projection");
        reader.Setup(value => value.QueryNativeProjectionRecordsAsync("people",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            5, null, It.IsAny<CancellationToken>()))
            .Returns((string source, IReadOnlyList<NativeRecordFilter> filters,
                IReadOnlyList<NativeProjectionColumn> columns, int limit, NativeConnection connection, CancellationToken token) =>
            {
                Assert.Equal("name", Assert.Single(filters).Field);
                Assert.All(columns, column => Assert.Equal("name", column.Field));
                Assert.Equal(new[] { "Text", "Units", "Again" }, columns.Select(column => column.Alias));
                return Task.FromResult(new OASISResult<IReadOnlyList<NativeProjectionRow>>(rows) { IsLoaded = true });
            });
        return (new VirtualHolonMapping(Guid.NewGuid(), 2, owner, ProviderType.MongoDBOASIS, "people",
            VirtualHolonMapping.SandboxConnectionName, "test-native://projection",
            new Dictionary<string, string> { ["display"] = "name" }, Guid.NewGuid()), reader, owner);
    }

    private static NativeProjectionRow Row(object units) => new("people", "one", new[] {
        new NativeProjectionCell(NativeProjectionValueState.Present, "😀"),
        new NativeProjectionCell(NativeProjectionValueState.Present, units),
        new NativeProjectionCell(NativeProjectionValueState.Present, 2)
    });

    [Fact]
    public async Task TypedMappingAndMixedSelectionKeepIdentityCellsAndFreshSnapshotSeparate()
    {
        var projection = CqlV1Compiler.CompileVirtualProjection(Request());
        var fixture = Fixture(new[] { Row(2) });
        var read = await fixture.Mapping.QueryProjectionAsync(fixture.Owner, fixture.Reader.Object, null,
            projection.Matches, projection.Columns, 5);
        Assert.False(read.IsError, read.Message);
        var row = Assert.Single(read.Result);
        var values = projection.Select(row);
        Assert.Equal(row.Id, values[0]); Assert.Equal("😀", values[1]); Assert.Equal(2, values[2]); Assert.Equal(2, values[3]);
        Assert.Equal(fixture.Mapping.ApplicationId, row.ApplicationId);
        Assert.Equal(fixture.Mapping.Id, row.MappingId);
        Assert.Equal(2, row.MappingVersion);
        var original = VirtualHolonRead.FromNative(fixture.Owner, fixture.Mapping.Provider, fixture.Mapping.Id, 2,
            fixture.Mapping.ConnectionIdentity, "people", DateTimeOffset.UtcNow,
            new NativeRecord("people", "one", new Dictionary<string, object> { ["name"] = "😀" }),
            new Dictionary<string, string> { ["name"] = "display" }, true, fixture.Mapping.ApplicationId);
        Assert.Equal(original.Id, row.Id);
        var snapshot = original.PrepareSnapshot(fixture.Owner);
        Assert.Equal("😀", snapshot.MetaData["display"]);
        Assert.False(snapshot.MetaData.ContainsKey("Units"));
        Assert.Null(typeof(VirtualHolonProjectionRead).GetMethod("PrepareSnapshot"));
        var changed = Request(); changed.Columns[2].Alias = "Changed";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileVirtualProjection(changed).Select(row));
    }

    [Fact]
    public async Task WrongNativeOutputDomainRefusesTheWholeMappedResult()
    {
        var projection = CqlV1Compiler.CompileVirtualProjection(Request());
        var fixture = Fixture(new[] { Row(2L) });
        var read = await fixture.Mapping.QueryProjectionAsync(fixture.Owner, fixture.Reader.Object, null,
            projection.Matches, projection.Columns, 5);
        Assert.Equal("NATIVE_PROJECTION_RESULT_INVALID", read.ErrorCode);
        Assert.Null(read.Result);
    }

    [Fact]
    public async Task ForeignAvatarCannotExecuteTheMappedProjection()
    {
        var projection = CqlV1Compiler.CompileVirtualProjection(Request());
        var fixture = Fixture(new[] { Row(2) });
        var read = await fixture.Mapping.QueryProjectionAsync(Guid.NewGuid(), fixture.Reader.Object, null,
            projection.Matches, projection.Columns, 5);
        Assert.Equal("VIRTUAL_HOLON_FORBIDDEN", read.ErrorCode);
        fixture.Reader.Verify(value => value.QueryNativeProjectionRecordsAsync(It.IsAny<string>(),
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(), It.IsAny<IReadOnlyList<NativeProjectionColumn>>(),
            It.IsAny<int>(), It.IsAny<NativeConnection>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
