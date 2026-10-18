using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TypedReadsPreserveLatestExactAndMissingVersions(bool synchronous)
    {
        Directory.CreateDirectory(_root);using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();var id=Guid.NewGuid();
        (await sqlite.SaveAvatarAsync(new Avatar {Id=id,Version=7,Username="typed-current",Email="typed@example.invalid",AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
        (await sqlite.SaveHolonAsync(new Holon {Id=id,Version=7,Name="typed-current"},saveChildren:false)).IsError.Should().BeFalse();
        var manager=CreateManager(autoFailover:true);manager.RegisterProvider(sqlite);var router=new OASISHyperDrive(manager);
        foreach(var version in new[] {0,7,1,-1})
        foreach(var avatars in new[] {false,true})
        {
            IRequest request=avatars ? new LoadAvatarRequest {AvatarId=id,Version=version,PreferredProvider=ProviderType.SQLLiteDBOASIS}:new LoadHolonRequest {HolonId=id,Version=version,PreferredProvider=ProviderType.SQLLiteDBOASIS};
            var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            var result=synchronous ? router.RouteRequest<IHolonBase>(request):await router.RouteRequestAsync<IHolonBase>(request);
            if(version<0)
            {
                result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("HYPERDRIVE_VERSION_INVALID");result.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
            }
            else
            {
                result.IsError.Should().BeFalse(result.Message);
                if(version==1) {result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();}
                else {result.Result.Version.Should().Be(7);result.Result.Id.Should().Be(id);result.IsLoaded.Should().BeTrue();}
            }
        }
    }
}
