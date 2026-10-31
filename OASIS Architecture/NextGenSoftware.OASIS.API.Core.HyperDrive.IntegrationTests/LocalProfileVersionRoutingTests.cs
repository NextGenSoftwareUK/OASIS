using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class RealProviderRoutingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LocalProfileProviderKeyHonorsExactVersionWithoutStaleFallback(bool synchronous)
    {
        Directory.CreateDirectory(_root);var local=new LocalFileOASIS(storageDirectory:FilesPath);
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();(await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();const string key="owned-local-version-profile";
        (await local.SaveAvatarAsync(new Avatar {Id=id,Username="current",Email="current@example.invalid",Version=7,
            ProviderUniqueStorageKey=new() {{ProviderType.LocalFileOASIS,key}}})).IsError.Should().BeFalse();
        (await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username="stale",Email="stale@example.invalid",Version=1,
            AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
        var manager=CreateManager(autoFailover:true);manager.RegisterProvider(local);manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoFailOverListForProviders(new[] {new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        foreach(var version in new[] {0,7,1})
        {
            var request=new StorageOperationRequest {Operation="LoadAvatarByProviderKey",ProviderKey=key,Version=version,PreferredProvider=ProviderType.LocalFileOASIS};
            var result=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
            result.IsError.Should().BeFalse(result.Message);
            if(version==1) {result.Result.Should().BeNull("missing exact version must not silently return current");result.IsLoaded.Should().BeFalse();}
            else {result.Result.Version.Should().Be(7);result.Result.Username.Should().Be("current");result.IsLoaded.Should().BeTrue();}
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
        }
        (await sqlite.LoadAvatarAsync(id)).Result.Version.Should().Be(1);
        await local.DeActivateProviderAsync();
    }
}
