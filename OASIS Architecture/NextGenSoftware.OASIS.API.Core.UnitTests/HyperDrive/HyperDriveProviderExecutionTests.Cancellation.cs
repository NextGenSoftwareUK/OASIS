using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)]
    [InlineData(false,true)] [InlineData(true,true)]
    public async Task ProviderOwnedTransportCancellationAllowsFailoverButNeverCorruption(bool asynchronous,bool corrupt)
    {
        var id=Guid.NewGuid();
        Exception inner=corrupt?new AggregateException(new OperationCanceledException(),new InvalidDataException("corrupt")):new OperationCanceledException();
        var cause=new IOException("typed provider transport fixture",inner);
        var primary=new Mock<IOASISStorageProvider>();
        primary.As<IProviderTransportFailureClassifier>().Setup(x=>x.IsProviderTransportFailure(cause)).Returns(true);
        primary.SetupAllProperties();primary.Object.ProviderType=new(ProviderType.LocalFileOASIS);
        primary.Object.ProviderCategory=new(ProviderCategory.Storage);primary.Object.IsProviderActivated=true;
        var backup=CreateActiveProvider(ProviderType.SQLLiteDBOASIS);
        primary.Setup(x=>x.LoadAvatar(id,0)).Returns(new OASISResult<IAvatar>{IsError=true,Exception=cause});
        primary.Setup(x=>x.LoadAvatarAsync(id,0)).ReturnsAsync(new OASISResult<IAvatar>{IsError=true,Exception=cause});
        backup.Setup(x=>x.LoadAvatar(id,0)).Returns(new OASISResult<IAvatar>(new Avatar{Id=id}));
        backup.Setup(x=>x.LoadAvatarAsync(id,0)).ReturnsAsync(new OASISResult<IAvatar>(new Avatar{Id=id}));
        var manager=new ProviderManager(null,CreateDna(HyperDriveModes.V2)){IsAutoFailOverEnabled=true};
        manager.RegisterProvider(primary.Object);manager.RegisterProvider(backup.Object);
        manager.SetAndReplaceAutoFailOverListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)});
        var router=new OASISHyperDrive(manager);var request=new StorageOperationRequest{Operation="LoadAvatar",AvatarId=id,PreferredProvider=ProviderType.LocalFileOASIS};
        var result=asynchronous?await router.RouteRequestAsync<IAvatar>(request):router.RouteRequest<IAvatar>(request);
        result.IsError.Should().Be(corrupt);
        if(corrupt){result.Exception.InnerException.Should().BeSameAs(cause);result.Result.Should().BeNull();}
        else result.Result.Id.Should().Be(id);
        (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests??0).Should().Be(corrupt?0:1);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CancelledProviderOperationNeverStartsFallbackOrReplica(bool asynchronous)
    {
        foreach(var mutation in new[]{false,true})foreach(var shape in new[]{"direct","wrapped","aggregate"}) {
            var id=Guid.NewGuid();var token=new CancellationToken(true);
            var cancellation=new OperationCanceledException("Owned cancelled operation.",null,token);
            Exception cause=shape=="direct"?cancellation:shape=="wrapped"?new IOException("Owned wrapper.",cancellation)
                :new AggregateException(new IOException("Other failure."),cancellation);
            var primary=CreateActiveProvider(ProviderType.LocalFileOASIS,"cancelled-primary");
            var secondary=CreateActiveProvider(ProviderType.SQLLiteDBOASIS,"must-not-execute");
            var avatarFailure=new OASISResult<IAvatar> {IsError=true,Exception=cause,Message="cancelled"};
            primary.Setup(x=>x.LoadAvatar(id,0)).Returns(avatarFailure);
            primary.Setup(x=>x.LoadAvatarAsync(id,0)).ReturnsAsync(avatarFailure);
            secondary.Setup(x=>x.LoadAvatar(id,0)).Returns(new OASISResult<IAvatar>(new Avatar {Id=id}));
            secondary.Setup(x=>x.LoadAvatarAsync(id,0)).ReturnsAsync(new OASISResult<IAvatar>(new Avatar {Id=id}));
            var holon=new Holon {Id=id};
            var holonFailure=new OASISResult<IHolon> {IsError=true,Exception=cause,Message="cancelled"};
            primary.Setup(x=>x.SaveHolon(holon,false,true,0,true,false)).Returns(holonFailure);
            primary.Setup(x=>x.SaveHolonAsync(holon,false,true,0,true,false)).ReturnsAsync(holonFailure);
            secondary.Setup(x=>x.SaveHolon(It.IsAny<IHolon>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>()))
                .Returns(new OASISResult<IHolon>(holon));
            secondary.Setup(x=>x.SaveHolonAsync(It.IsAny<IHolon>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>()))
                .ReturnsAsync(new OASISResult<IHolon>(holon));
            var manager=new ProviderManager(null,CreateDna(HyperDriveModes.V2)) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(primary.Object);manager.RegisterProvider(secondary.Object);
            var providers=new[]{new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            manager.SetAndReplaceAutoFailOverListForProviders(providers);manager.SetAndReplaceAutoReplicationListForProviders(providers);
            var router=new OASISHyperDrive(manager);
            var request=new StorageOperationRequest {Operation=mutation?"SaveHolon":"LoadAvatar",AvatarId=id,
                Payload=holon,SaveChildren=false,PreferredProvider=ProviderType.LocalFileOASIS};
            if(mutation){
                var result=asynchronous?await router.RouteRequestAsync<IHolon>(request):router.RouteRequest<IHolon>(request);
                result.IsError.Should().BeTrue();result.Exception.Should().BeSameAs(cause);result.Result.Should().BeNull();
            }else{
                var result=asynchronous?await router.RouteRequestAsync<IAvatar>(request):router.RouteRequest<IAvatar>(request);
                result.IsError.Should().BeTrue();result.Exception.Should().BeSameAs(cause);result.Result.Should().BeNull();
            }
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests??0).Should().Be(0);
            secondary.Verify(x=>x.LoadAvatar(It.IsAny<Guid>(),It.IsAny<int>()),Times.Never);
            secondary.Verify(x=>x.LoadAvatarAsync(It.IsAny<Guid>(),It.IsAny<int>()),Times.Never);
            secondary.Verify(x=>x.SaveHolon(It.IsAny<IHolon>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>()),Times.Never);
            secondary.Verify(x=>x.SaveHolonAsync(It.IsAny<IHolon>(),It.IsAny<bool>(),It.IsAny<bool>(),It.IsAny<int>(),It.IsAny<bool>(),It.IsAny<bool>()),Times.Never);
        }
    }
}
