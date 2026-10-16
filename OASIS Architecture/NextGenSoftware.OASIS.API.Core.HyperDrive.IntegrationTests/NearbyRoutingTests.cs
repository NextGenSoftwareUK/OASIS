using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task NearbyCapabilityMismatchUsesPolicyWithoutInventingResults(bool synchronous,bool failover)
    {
        Directory.CreateDirectory(_root);
        var local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:FilesPath);
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager=CreateManager(autoFailover:failover);manager.RegisterProvider(local);manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoFailOverListForProviders(new[] {
            new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.LocalFileOASIS),
            new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        var id=Guid.NewGuid();var metadata=new Dictionary<string,object> {{"Latitude","30.0005"},{"Longitude","20"}};
        (await sqlite.SaveHolonAsync(new Holon {Id=id,Name="actual network fallback",IsActive=true,MetaData=new(metadata)},saveChildren:false)).IsError.Should().BeFalse();
        (await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username="actual-network-fallback",Email="nearby@example.invalid",IsActive=true,MetaData=new(metadata),AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        foreach(var avatars in new[] {false,true})
        {
            var request=new StorageOperationRequest {Operation=avatars ? "GetAvatarsNearMe":"GetHolonsNearMe",GeoLatitude=30000000,GeoLongitude=20000000,RadiusInMeters=100,PreferredProvider=ProviderType.LocalFileOASIS};
            var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            var result=synchronous ? router.RouteRequest<IEnumerable<IHolonBase>>(request):await router.RouteRequestAsync<IEnumerable<IHolonBase>>(request);
            result.IsError.Should().Be(!failover);
            if(failover)
            {
                result.IsLoaded.Should().BeTrue();result.Result.Single().Id.Should().Be(id);
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().BeGreaterThan(before);
            }
            else
            {
                result.ErrorCode.Should().Be("HYPERDRIVE_NETWORK_PROVIDER_REQUIRED");result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
            }
        }
        (await local.LoadAllHolonsAsync(loadChildren:false)).Result.Should().BeEmpty();
        (await local.LoadAllAvatarsAsync()).Result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NearbyRoutesActualSqliteRecordsAndIntegrityErrors(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager=CreateManager(autoFailover:true,autoReplication:true);manager.RegisterProvider(sqlite);
        var router=new OASISHyperDrive(manager);var id=Guid.NewGuid();
        var metadata=new Dictionary<string,object> {{"Latitude","30.0005"},{"Longitude","20"}};
        var avatar=new Avatar {Id=id,Username="nearby",Email="nearby@example.invalid",IsActive=true,MetaData=new(metadata),AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)};
        var holon=new Holon {Id=id,Name="complete nearby",HolonType=HolonType.Park,IsActive=true,MetaData=new(metadata)};
        (await sqlite.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        (await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
        var request=new StorageOperationRequest {Operation="GetAvatarsNearMe",GeoLatitude=30000000,GeoLongitude=20000000,RadiusInMeters=100,PreferredProvider=ProviderType.SQLLiteDBOASIS};
        var profiles=synchronous ? router.RouteRequest<IEnumerable<IAvatar>>(request):await router.RouteRequestAsync<IEnumerable<IAvatar>>(request);
        profiles.IsError.Should().BeFalse(profiles.Message);profiles.IsLoaded.Should().BeTrue();profiles.Result.Single().Id.Should().Be(id);
        request.Operation="GetHolonsNearMe";request.HolonType=HolonType.Park;
        var rows=synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
        rows.IsError.Should().BeFalse(rows.Message);rows.IsLoaded.Should().BeTrue();rows.Result.Single().Name.Should().Be("complete nearby");
        holon.MetaData.Remove("Longitude");(await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
        var corrupt=synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
        corrupt.IsError.Should().BeTrue();corrupt.IsLoaded.Should().BeFalse();corrupt.Result.Should().BeNull();corrupt.Exception.Should().BeOfType<InvalidDataException>();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NearbyInvalidRequestsFailBeforeProviderSelection(bool synchronous)
    {
        var manager=CreateManager(autoFailover:true);var router=new OASISHyperDrive(manager);
        foreach(var scenario in new[] {"latitude","longitude","radius","type","version"})
        {
            var request=new StorageOperationRequest {Operation="GetHolonsNearMe",PreferredProvider=ProviderType.SQLLiteDBOASIS};
            if(scenario=="latitude") request.GeoLatitude=90000001;
            if(scenario=="longitude") request.GeoLongitude=180000001;
            if(scenario=="radius") request.RadiusInMeters=-1;
            if(scenario=="type") request.HolonType=(HolonType)int.MaxValue;
            if(scenario=="version") request.Version=1;
            var result=synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
            result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("HYPERDRIVE_NEARBY_REQUEST_INVALID");result.Result.Should().BeNull();
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
        }
    }
}
