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
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task TypedHolonGraphReadsHonorOptionsAndRejectNegativeDepth(bool synchronous,bool fromProvider)
    {
        Directory.CreateDirectory(_root);using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon {Id=Guid.NewGuid(),Name="typed root"};var child=new Holon {Id=Guid.NewGuid(),Name="typed child"};
        var grandchild=new Holon {Id=Guid.NewGuid(),Name="typed grandchild"};root.Children.Add(child);child.Children.Add(grandchild);
        (await sqlite.SaveHolonAsync(root)).IsError.Should().BeFalse();
        var manager=CreateManager(autoFailover:true);manager.RegisterProvider(sqlite);var router=new OASISHyperDrive(manager);
        foreach(var mode in new[] {"none","direct","bounded","all","invalid"})
        {
            var request=new LoadHolonRequest {HolonId=root.Id,PreferredProvider=ProviderType.SQLLiteDBOASIS,
                LoadChildren=mode!="none",Recursive=mode!="direct",MaxChildDepth=mode=="bounded" ? 1:mode=="invalid" ? -1:0,ChildrenFromProvider=fromProvider,ContinueOnError=false};
            var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            if(mode=="invalid")
            {
                result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("HYPERDRIVE_DEPTH_INVALID");result.Exception.Should().BeOfType<ArgumentOutOfRangeException>();
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
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
    }
}
