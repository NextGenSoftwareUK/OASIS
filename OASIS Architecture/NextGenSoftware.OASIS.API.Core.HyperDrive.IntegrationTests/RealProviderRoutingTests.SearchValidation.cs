using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task InvalidSqliteSearchCannotFailOverToAnotherStore(bool synchronous)
    {
        Directory.CreateDirectory(_root);
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        var local=new LocalFileOASIS(storageDirectory:FilesPath);
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        try
        {
            var marker=new Holon {Id=Guid.NewGuid(),Name="owned backup must not be searched"};
            (await local.SaveHolonAsync(marker,saveChildren:false)).IsError.Should().BeFalse();
            var manager=CreateManager(autoFailover:true);
            manager.RegisterProvider(sqlite);manager.RegisterProvider(local);
            manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),
                new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)}).IsError.Should().BeFalse();
            var before=manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS)?.TotalRequests ?? 0;
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,MetaKeyValuePairMatchMode=(MetaKeyValuePairMatchMode)999,
                SearchGroups=new() {new SearchTextGroup {SearchQuery="owned",SearchHolons=true}}};
            var request=new StorageOperationRequest {Operation="Search",PreferredProvider=ProviderType.SQLLiteDBOASIS,SearchParams=query};
            var router=new OASISHyperDrive(manager);
            var result=synchronous ? router.RouteRequest<ISearchResults>(request):await router.RouteRequestAsync<ISearchResults>(request);
            result.IsError.Should().BeTrue();result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
            result.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
            result.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
            (manager.PerformanceMonitor.GetMetrics(ProviderType.LocalFileOASIS)?.TotalRequests ?? 0).Should().Be(before);
            (await local.LoadHolonAsync(marker.Id,loadChildren:false)).Result.Name.Should().Be(marker.Name);
        }
        finally {await local.DeActivateProviderAsync();}
    }
}
