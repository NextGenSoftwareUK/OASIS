using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class UnfilteredSnapshotTests
{
    [Fact]
    public async Task SnapshotReadsOriginalFieldsThroughTheDeclaredBoundedCapability()
    {
        var actor=Guid.NewGuid();
        var reader=new Mock<INativeDataSnapshotQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(r=>r.NativeDataProviderType).Returns(ProviderType.MongoDBOASIS);
        reader.Setup(r=>r.DescribeVirtualQueryConnection(null)).Returns("snapshot-fixture");
        var original=new Dictionary<string,object>{{"native_name","original source value"}};
        var record=new NativeRecord("items","one",original);
        reader.Setup(r=>r.QueryNativeSnapshotRecordsAsync("items",
            It.Is<IReadOnlyList<NativeRecordFilter>>(f=>f.Count==0),
            It.Is<IReadOnlyList<string>>(f=>f.Count==1 && f[0]=="native_name"),
            200,null,It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<NativeRecord>>(new[]{record}){IsLoaded=true});
        var mapping=new VirtualHolonMapping(Guid.NewGuid(),1,actor,ProviderType.MongoDBOASIS,
            "items",VirtualHolonMapping.SandboxConnectionName,"snapshot-fixture",
            new Dictionary<string,string>{{"name","native_name"}});
        var result=await mapping.QuerySnapshotCandidatesAsync(actor,reader.Object,null,
            Array.Empty<NativeRecordFilter>(),new[]{"name"},200);
        Assert.False(result.IsError,result.Message);
        var row=Assert.Single(result.Result);
        var snapshot=row.PrepareSnapshot(actor);
        Assert.Equal(row.Id,snapshot.Id);
        Assert.False(snapshot.IsPublic);
        Assert.Equal(actor,snapshot.CreatedByAvatarId);
        Assert.Equal("original source value",snapshot.MetaData["name"]);
        Assert.Equal("original source value",original["native_name"]);
        var foreign=await mapping.QuerySnapshotCandidatesAsync(Guid.NewGuid(),reader.Object,null,
            Array.Empty<NativeRecordFilter>(),new[]{"name"},200);
        Assert.True(foreign.IsError);
        Assert.Equal("VIRTUAL_HOLON_FORBIDDEN",foreign.ErrorCode);
        reader.VerifyGet(r=>r.NativeDataProviderType,Times.Once);
        reader.Verify(r=>r.DescribeVirtualQueryConnection(null),Times.Once);
        reader.Verify(r=>r.QueryNativeSnapshotRecordsAsync("items",
            It.IsAny<IReadOnlyList<NativeRecordFilter>>(),It.IsAny<IReadOnlyList<string>>(),
            200,null,It.IsAny<CancellationToken>()),Times.Once);
        reader.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OrdinaryNativeMappingReadsStillRequireAPredicate()
    {
        var actor=Guid.NewGuid();
        var reader=new Mock<INativeDataSnapshotQueryProvider>(MockBehavior.Strict);
        reader.SetupGet(r=>r.NativeDataProviderType).Returns(ProviderType.MongoDBOASIS);
        var mapping=new VirtualHolonMapping(Guid.NewGuid(),1,actor,ProviderType.MongoDBOASIS,
            "items",VirtualHolonMapping.SandboxConnectionName,"snapshot-fixture",
            new Dictionary<string,string>{{"name","native_name"}});
        var result=await mapping.QueryAsync(actor,reader.Object,null,
            Array.Empty<NativeRecordFilter>(),new[]{"name"},100);
        Assert.True(result.IsError);
        Assert.Equal("VIRTUAL_HOLON_QUERY_INVALID",result.ErrorCode);
        reader.VerifyGet(r=>r.NativeDataProviderType,Times.Once);
        reader.VerifyNoOtherCalls();
    }
}
