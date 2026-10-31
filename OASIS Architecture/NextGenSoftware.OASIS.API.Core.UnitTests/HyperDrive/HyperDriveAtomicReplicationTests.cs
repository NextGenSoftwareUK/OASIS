using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task AtomicReplicationUsesOnlyCapableAuthorityWithoutPlainWriteDowngrade(bool synchronous,bool supported)
    {
        var dna=CreateDna(HyperDriveModes.V2);
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true,IsAutoLoadBalanceEnabled=false};
        var provider=new Mock<IOASISStorageProvider>();provider.SetupAllProperties();
        var conditional=provider.As<IConditionalHolonStorageProvider>();
        var atomic=supported ? provider.As<IAtomicHolonReplicationStorageProvider>():null;
        provider.Object.ProviderType=new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory=new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName="atomic-primary";provider.Object.IsProviderActivated=true;
        var root=new Holon{Id=Guid.NewGuid(),Name="committed",Version=1,VersionId=Guid.NewGuid()};
        var enrollment=new HolonReplicationEnrollment("source",new[]{"target"});
        if(atomic!=null)
        {
            atomic.Setup(value=>value.ExecuteReplicatingHolonWrite(It.IsAny<ConditionalHolonWriteRequest>(),enrollment))
                .Returns(new OASISResult<IEnumerable<IHolon>>(new[]{root}){IsSaved=true});
            atomic.Setup(value=>value.ExecuteReplicatingHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),enrollment,default))
                .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(new[]{root}){IsSaved=true});
        }
        manager.RegisterProvider(provider.Object);
        var router=new OASISHyperDrive(manager);
        var request=new StorageOperationRequest{Operation="SaveHolon",Payload=root,PreferredProvider=ProviderType.SQLLiteDBOASIS,
            SaveChildren=false,ConditionalWrite=new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.SQLLiteDBOASIS,
                new[]{new HolonWriteCondition(root.Id,null)},enrollment)};
        var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
        result.IsError.Should().Be(!supported);
        if(!supported)result.ErrorCode.Should().Be("COSMIC_ATOMIC_REPLICATION_UNSUPPORTED");
        else atomic!.Invocations.Should().ContainSingle(call=>call.Method.Name==(synchronous ? "ExecuteReplicatingHolonWrite":"ExecuteReplicatingHolonWriteAsync"));
        conditional.Verify(value=>value.ExecuteConditionalHolonWrite(It.IsAny<ConditionalHolonWriteRequest>()),Times.Never);
        conditional.Verify(value=>value.ExecuteConditionalHolonWriteAsync(It.IsAny<ConditionalHolonWriteRequest>(),It.IsAny<CancellationToken>()),Times.Never);
        provider.Verify(value=>value.SaveHolon(It.IsAny<IHolon>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>()),Times.Never);
    }
}
