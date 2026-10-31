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
    public async Task NativeLocalGraphCorruptionAndCyclesCannotReadStaleSecondary(bool synchronous)
    {
        Directory.CreateDirectory(_root);var local=new LocalFileOASIS(storageDirectory:FilesPath);
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();(await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon {Id=Guid.NewGuid(),Name="native graph"};var child=new Holon {Id=Guid.NewGuid()};var leaf=new Holon {Id=Guid.NewGuid()};
        root.Children.Add(child);child.Children.Add(leaf);(await local.SaveHolonAsync(root)).IsError.Should().BeFalse();
        (await sqlite.SaveHolonAsync(new Holon {Id=root.Id,Name="stale graph"},saveChildren:false)).IsError.Should().BeFalse();
        var manager=CreateManager(autoFailover:true);manager.RegisterProvider(local);manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);var request=new LoadHolonRequest {HolonId=root.Id,PreferredProvider=ProviderType.LocalFileOASIS,ChildrenFromProvider=true};
        foreach(var cycle in new[] {false,true})
        {
            var path=Path.Combine(FilesPath,"Holons",(cycle ? root.Id:child.Id)+".json");var original=await File.ReadAllTextAsync(path);
            var json=Newtonsoft.Json.Linq.JObject.Parse(original);json["ParentHolonId"]=leaf.Id.ToString();
            var corrupt=cycle ? json.ToString():"{ corrupt child";
            _preserveFixture=true;await File.WriteAllTextAsync(path,corrupt);
            var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            result.IsError.Should().BeTrue();result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();result.Exception.Should().BeOfType<InvalidDataException>();
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
            (await File.ReadAllTextAsync(path)).Should().Be(corrupt);(await sqlite.LoadHolonAsync(root.Id,loadChildren:false)).Result.Name.Should().Be("stale graph");
            await File.WriteAllTextAsync(path,original);var healthy=await local.LoadHolonAsync(root.Id);
            healthy.IsError.Should().BeFalse(healthy.Message);healthy.Result.Children.Single().Children.Single().Id.Should().Be(leaf.Id);
            _preserveFixture=false;
        }
        await local.DeActivateProviderAsync();
    }
}
