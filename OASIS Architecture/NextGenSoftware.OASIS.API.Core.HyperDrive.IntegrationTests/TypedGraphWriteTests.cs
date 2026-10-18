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
    [InlineData(false)] [InlineData(true)]
    public async Task LocalGraphPartialWriteRetainsPathsAndNeverReplaysToSqlite(bool synchronous)
    {
        Directory.CreateDirectory(_root);var local=new LocalFileOASIS(storageDirectory:FilesPath);
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();(await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon {Id=Guid.NewGuid()};var child=new Holon {Id=Guid.NewGuid()};root.Children.Add(child);
        var rootPath=Path.GetFullPath(Path.Combine(FilesPath,"Holons",root.Id+".json"));var childPath=Path.GetFullPath(Path.Combine(FilesPath,"Holons",child.Id+".json"));
        Directory.CreateDirectory(childPath);
        var manager=CreateManager(autoFailover:true,autoReplication:true);manager.RegisterProvider(local);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);var request=new SaveHolonRequest {Holon=root,PreferredProvider=ProviderType.LocalFileOASIS};
        var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
        result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.Result.Should().BeNull();
        var evidence=result.Exception.Should().BeOfType<LocalGraphWriteReconciliationException>().Subject;
        evidence.AcknowledgedPaths.Should().Equal(rootPath);evidence.UncertainPath.Should().Be(childPath);
        (evidence.InnerException is IOException or UnauthorizedAccessException).Should().BeTrue("writing a directory must retain the native filesystem rejection");
        File.Exists(rootPath).Should().BeTrue();child.ParentHolonId.Should().Be(Guid.Empty);
        (await sqlite.LoadAllHolonsAsync(loadChildren:false)).Result.Should().BeEmpty();
        (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
        (await local.LoadHolonAsync(root.Id,loadChildren:false)).IsError.Should().BeFalse();
        await local.DeActivateProviderAsync();
    }

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
