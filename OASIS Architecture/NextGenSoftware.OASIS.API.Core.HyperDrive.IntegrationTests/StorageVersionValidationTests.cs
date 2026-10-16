using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NegativeStorageVersionsFailBeforeProviderSelection(bool synchronous)
    {
        Directory.CreateDirectory(_root);using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        var manager=CreateManager(autoFailover:true,autoReplication:true,autoLoadBalance:true);manager.RegisterProvider(sqlite);
        var router=new OASISHyperDrive(manager);
        foreach(var operation in new[] {"LoadAvatar","LoadAvatarByProviderKey","LoadAvatarByUsername","LoadAvatarByEmail",
            "LoadAllAvatars","LoadAvatarDetail","LoadAvatarDetailByUsername","LoadAvatarDetailByEmail","LoadAllAvatarDetails",
            "LoadHolon","LoadHolonByProviderKey","LoadAllHolons","LoadHolonsForParent","LoadHolonsForParentByProviderKey",
            "LoadHolonsByMetaData","LoadHolonsByMetaDataPairs","Search","ExportAll","ExportAllDataForAvatarById",
            "ExportAllDataForAvatarByUsername","ExportAllDataForAvatarByEmail"})
        {
            var request=new StorageOperationRequest {Operation=operation,Version=-1,AvatarId=Guid.NewGuid(),HolonId=Guid.NewGuid(),PreferredProvider=ProviderType.SQLLiteDBOASIS};
            var result=synchronous ? router.RouteRequest<object>(request):await router.RouteRequestAsync<object>(request);
            result.IsError.Should().BeTrue(operation);result.ErrorCode.Should().Be("HYPERDRIVE_VERSION_INVALID",operation);
            result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();result.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
            sqlite.IsProviderActivated.Should().BeFalse(operation);
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
        }
        File.Exists(SqlitePath).Should().BeFalse("invalid version must not activate/create native storage");
    }
}
