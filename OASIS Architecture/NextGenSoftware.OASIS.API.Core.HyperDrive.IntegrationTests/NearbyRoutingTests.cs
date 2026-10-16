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
