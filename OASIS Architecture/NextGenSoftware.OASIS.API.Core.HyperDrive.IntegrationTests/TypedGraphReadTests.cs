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
    [InlineData(false,false,false)] [InlineData(true,false,false)] [InlineData(false,true,false)] [InlineData(true,true,false)]
    [InlineData(false,false,true)] [InlineData(true,false,true)] [InlineData(false,true,true)] [InlineData(true,true,true)]
    public async Task TypedHolonGraphReadsHonorOptionsAndRejectNegativeDepth(bool synchronous,bool fromProvider,bool localProvider)
    {
        Directory.CreateDirectory(_root);using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        IOASISStorageProvider store=localProvider ? new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:FilesPath):sqlite;
        var type=localProvider ? ProviderType.LocalFileOASIS:ProviderType.SQLLiteDBOASIS;
        (await store.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon {Id=Guid.NewGuid(),Name="typed root"};var child=new Holon {Id=Guid.NewGuid(),Name="typed child"};
        var grandchild=new Holon {Id=Guid.NewGuid(),Name="typed grandchild"};root.Children.Add(child);child.Children.Add(grandchild);
        (await store.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var manager=CreateManager(autoFailover:true);manager.RegisterProvider(store);var router=new OASISHyperDrive(manager);
        foreach(var mode in new[] {"none","direct","bounded","all","invalid"})
        {
            var request=new LoadHolonRequest {HolonId=root.Id,PreferredProvider=type,
                LoadChildren=mode!="none",Recursive=mode!="direct",MaxChildDepth=mode=="bounded" ? 1:mode=="invalid" ? -1:0,ChildrenFromProvider=fromProvider,ContinueOnError=false};
            var before=manager.PerformanceMonitor.GetMetrics(type)?.TotalRequests ?? 0;
            var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            if(mode=="invalid")
            {
                result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("HYPERDRIVE_DEPTH_INVALID");result.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
                (manager.PerformanceMonitor.GetMetrics(type)?.TotalRequests ?? 0).Should().Be(before);
            }
            else
            {
                result.IsError.Should().BeFalse(result.Message);result.IsLoaded.Should().BeTrue();result.Result.Id.Should().Be(root.Id);
                if(mode=="none") result.Result.Children.Should().BeEmpty();
                else
                {
                    var actual=result.Result.Children.Single();actual.Id.Should().Be(child.Id);
                    if(mode=="all") actual.Children.Single().Id.Should().Be(grandchild.Id);else actual.Children.Should().BeEmpty();
                }
            }
        }
        await store.DeActivateProviderAsync();
    }
}
