using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task TypedGraphWritesReplicateSelectedNodesOnly(bool synchronous,bool sqlitePrimary)
    {
        Directory.CreateDirectory(_root);var local=new LocalFileOASIS(storageDirectory:FilesPath);
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();(await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var manager=CreateManager(autoReplication:true);manager.RegisterProvider(local);manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoReplicationListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        foreach(var mode in new[] {"none","direct","all"})
        {
            var root=new Holon {Id=Guid.NewGuid(),Name="typed-write-root"};var child=new Holon {Id=Guid.NewGuid(),Name="typed-write-child"};
            var grandchild=new Holon {Id=Guid.NewGuid(),Name="typed-write-grandchild"};root.Children.Add(child);child.Children.Add(grandchild);
            var request=new SaveHolonRequest {Holon=root,HolonId=root.Id,PreferredProvider=sqlitePrimary ? ProviderType.SQLLiteDBOASIS:ProviderType.LocalFileOASIS,SaveChildren=mode!="none",Recursive=mode=="all",ContinueOnError=false};
            var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            result.IsError.Should().BeFalse(result.Message);result.IsWarning.Should().BeFalse(result.Message);
            foreach(var store in new IOASISStorageProvider[] {sqlite,local})
            {
                var native=await store.LoadHolonAsync(root.Id,loadChildren:false);
                native.IsError.Should().BeFalse(native.Message);
                var rows=await store.LoadAllHolonsAsync(loadChildren:false);rows.IsError.Should().BeFalse(rows.Message);
                rows.Result.Any(r=>r.Id==root.Id).Should().BeTrue();
                rows.Result.Any(r=>r.Id==child.Id).Should().Be(mode!="none");
                rows.Result.Any(r=>r.Id==grandchild.Id).Should().Be(mode=="all");
            }
        }
        await local.DeActivateProviderAsync();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TypedWriteValidationPrecedesProviderActivation(bool synchronous)
    {
        Directory.CreateDirectory(_root);using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        var manager=CreateManager(autoFailover:true,autoReplication:true);manager.RegisterProvider(sqlite);var router=new OASISHyperDrive(manager);
        foreach(var depth in new[] {false,true})
        {
            var request=new SaveHolonRequest {Holon=depth ? new Holon():new object(),MaxChildDepth=depth ? -1:0,PreferredProvider=ProviderType.SQLLiteDBOASIS};
            var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            result.IsError.Should().BeTrue();result.ErrorCode.Should().Be(depth ? "HYPERDRIVE_DEPTH_INVALID":"HYPERDRIVE_PAYLOAD_INVALID");
            sqlite.IsProviderActivated.Should().BeFalse();(manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
        }
        File.Exists(SqlitePath).Should().BeFalse();
    }
}
