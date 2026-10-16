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
    public async Task InvalidGraphWritesCannotCreateNativeDatabase(bool synchronous)
    {
        Directory.CreateDirectory(_root);using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        var manager=CreateManager(autoFailover:true,autoReplication:true,autoLoadBalance:true);manager.RegisterProvider(sqlite);
        var router=new OASISHyperDrive(manager);
        foreach(var operation in new[]{"SaveHolon","SaveHolons"})
        foreach(var children in new[]{false,true})
        foreach(var current in operation=="SaveHolons" ? new[]{false,true}:new[]{false})
        {
            var source=new NextGenSoftware.OASIS.API.Core.Holons.Holon();
            var request=new StorageOperationRequest {Operation=operation,Payload=operation=="SaveHolon" ? source:new NextGenSoftware.OASIS.API.Core.Interfaces.IHolon[]{source},
                SaveChildren=children,MaxChildDepth=current ? 0:-1,CurrentChildDepth=current ? -1:0,PreferredProvider=ProviderType.SQLLiteDBOASIS};
            var result=synchronous ? router.RouteRequest<object>(request):await router.RouteRequestAsync<object>(request);
            result.ErrorCode.Should().Be("HYPERDRIVE_DEPTH_INVALID");result.Result.Should().BeNull();result.IsSaved.Should().BeFalse();
            (await router.LoadBalanceRequestAsync<object>(request)).ErrorCode.Should().Be(result.ErrorCode);
            (await router.FailoverRequestAsync<object>(request)).ErrorCode.Should().Be(result.ErrorCode);
            (await router.ReplicateRequestAsync<object>(request)).ErrorCode.Should().Be(result.ErrorCode);
            sqlite.IsProviderActivated.Should().BeFalse();source.Id.Should().BeEmpty();
        }
        File.Exists(SqlitePath).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidCollectionQueriesCannotActivateNativeSqlite(bool synchronous)
    {
        Directory.CreateDirectory(_root);using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        var manager=CreateManager(autoFailover:true,autoReplication:true,autoLoadBalance:true);manager.RegisterProvider(sqlite);
        var router=new OASISHyperDrive(manager);
        foreach(var operation in new[]{"LoadAllHolons","LoadHolonsForParent","LoadHolonsForParentByProviderKey","LoadHolonsByMetaData","LoadHolonsByMetaDataPairs"})
        foreach(var field in new[]{"type","depth","currentDepth"})
        {
            var request=new StorageOperationRequest {Operation=operation,HolonId=Guid.NewGuid(),ProviderKey=Guid.NewGuid().ToString(),MetaKey="owned",MetaValue="value",
                MetaKeyValuePairs=new Dictionary<string,string>{{"owned","value"}},HolonType=field=="type" ? (HolonType)int.MaxValue:HolonType.All,
                MaxChildDepth=field=="depth" ? -1:0,CurrentChildDepth=field=="currentDepth" ? -1:0,PreferredProvider=ProviderType.SQLLiteDBOASIS};
            var result=synchronous ? router.RouteRequest<object>(request):await router.RouteRequestAsync<object>(request);
            result.IsError.Should().BeTrue();result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
            result.ErrorCode.Should().Be(field=="type" ? "HYPERDRIVE_HOLON_TYPE_INVALID":"HYPERDRIVE_DEPTH_INVALID");
            (await router.LoadBalanceRequestAsync<object>(request)).ErrorCode.Should().Be(result.ErrorCode);
            (await router.FailoverRequestAsync<object>(request)).ErrorCode.Should().Be(result.ErrorCode);
            (await router.ReplicateRequestAsync<object>(request)).ErrorCode.Should().Be(result.ErrorCode);
            sqlite.IsProviderActivated.Should().BeFalse();manager.LastProviderSelectionDiagnostic.Should().BeNull();
            router.LastFailoverDiagnostic.Should().BeNull();router.LastReplicationDiagnostic.Should().BeNull();
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
        }
        File.Exists(SqlitePath).Should().BeFalse("malformed queries must not create native storage");
    }

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
