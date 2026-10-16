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
    [InlineData(false,false,0)]
    [InlineData(false,false,1)]
    [InlineData(false,false,2)]
    [InlineData(false,true,0)]
    [InlineData(false,true,1)]
    [InlineData(false,true,2)]
    [InlineData(true,false,0)]
    [InlineData(true,false,1)]
    [InlineData(true,false,2)]
    [InlineData(true,true,0)]
    [InlineData(true,true,1)]
    [InlineData(true,true,2)]
    public async Task LocalProfileCorruptionCannotBecomeStaleSqliteOrSuccessfulAbsence(bool synchronous,bool details,int kind)
    {
        Directory.CreateDirectory(_root);var local=new LocalFileOASIS(storageDirectory:FilesPath);
        using var sqlite=new SQLLiteDBOASIS($"Data Source={SqlitePath};Pooling=False");
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();(await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id=Guid.NewGuid();const string username="owned-integrity";const string email="owned-integrity@example.invalid";
        foreach(var store in new IOASISStorageProvider[] {local,sqlite})
        {
            (await store.SaveAvatarAsync(new Avatar {Id=id,Username=username,Email=email,FirstName="retained",AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
            (await store.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=username,Email=email,XP=47})).IsError.Should().BeFalse();
        }
        var path=Path.Combine(FilesPath,details ? "AvatarDetails":"Avatars",id+".json");
        var original=await File.ReadAllTextAsync(path);
        var corrupt=kind==0 ? "{ invalid":kind==1 ? "null":"{\"Version\":\"not-an-integer\"}";
        _preserveFixture=true;await File.WriteAllTextAsync(path,corrupt);
        var manager=CreateManager(autoFailover:true,autoReplication:true);manager.RegisterProvider(local);manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoFailOverListForProviders(new[] {new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        var operations=details ? new[] {"LoadAvatarDetail","LoadAvatarDetailByUsername","LoadAvatarDetailByEmail","LoadAllAvatarDetails"}:new[] {"LoadAvatar","LoadAvatarByProviderKey","LoadAvatarByUsername","LoadAvatarByEmail","LoadAllAvatars"};
        foreach(var operation in operations)
        {
            var request=new StorageOperationRequest {Operation=operation,AvatarId=id,Username=username,Email=email,ProviderKey=id.ToString(),PreferredProvider=ProviderType.LocalFileOASIS};
            // object preserves the concrete provider payload type for both single and collection routes.
            var result=synchronous ? router.RouteRequest<object>(request):await router.RouteRequestAsync<object>(request);
            result.IsError.Should().BeTrue(operation);result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();result.Exception.Should().BeOfType<InvalidDataException>();
            if(kind!=1) result.Exception.InnerException.Should().BeAssignableTo<Newtonsoft.Json.JsonException>();
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
            (await File.ReadAllTextAsync(path)).Should().Be(corrupt);
        }
        (await sqlite.LoadAvatarAsync(id)).Result.FirstName.Should().Be("retained");(await sqlite.LoadAvatarDetailAsync(id)).Result.XP.Should().Be(47);
        await File.WriteAllTextAsync(path,original);
        (await local.LoadAvatarAsync(id)).IsError.Should().BeFalse();(await local.LoadAvatarDetailAsync(id)).IsError.Should().BeFalse();
        await local.DeActivateProviderAsync();_preserveFixture=false;
    }
}
