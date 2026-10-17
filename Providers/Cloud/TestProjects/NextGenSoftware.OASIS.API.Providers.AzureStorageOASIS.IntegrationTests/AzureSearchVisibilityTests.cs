using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task SearchExcludesTombstonesButKeepsInactiveRoots(bool synchronous,bool v2)
    {
        RequireLive();
        var liveAvatar=new Avatar{Id=Guid.NewGuid(),Username="owned inactive search",IsActive=false};
        var deletedAvatar=new Avatar{Id=Guid.NewGuid(),Username="owned deleted search",DeletedDate=DateTime.UtcNow};
        var liveHolon=new Holon{Id=Guid.NewGuid(),Name="owned inactive search",IsActive=false};
        var deletedHolon=new Holon{Id=Guid.NewGuid(),Name="owned deleted search",DeletedDate=DateTime.UtcNow};
        foreach(var avatar in new[]{liveAvatar,deletedAvatar})Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
        foreach(var holon in new[]{liveHolon,deletedHolon})Assert.IsFalse((await _provider.SaveHolonAsync(holon,saveChildren:false)).IsError);
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new(){PlanType="Gold"}}};
        var manager=new ProviderManager(null,dna);manager.RegisterProvider(_provider);var router=new OASISHyperDrive(manager);
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false};
        var request=new StorageOperationRequest{Operation="Search",SearchParams=query,LoadChildren=false,PreferredProvider=ProviderType.AzureStorageOASIS};
        var result=v2?(synchronous?router.RouteRequest<ISearchResults>(request):await router.RouteRequestAsync<ISearchResults>(request)):
            (synchronous?_provider.Search(query,loadChildren:false):await _provider.SearchAsync(query,loadChildren:false));
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(2,result.Result.NumberOfResults);
        Assert.AreEqual(liveAvatar.Id,result.Result.SearchResultAvatars.Single().Id);Assert.AreEqual(liveHolon.Id,result.Result.SearchResultHolons.Single().Id);
        Assert.IsFalse(result.Result.SearchResultAvatars.Single().IsActive);Assert.IsFalse(result.Result.SearchResultHolons.Single().IsActive);
    }
}
