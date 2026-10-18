using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class GoogleSearchVisibilityTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task DeletedRootsAreExcludedButInactiveRootsRemainSearchable(bool synchronous,bool v2)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var provider=new GoogleCloudOASIS(projectId:"oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);
        Assert.AreEqual(0,(await provider.LoadAllAvatarsAsync()).Result.Count());
        Assert.AreEqual(0,(await provider.LoadAllHolonsAsync(loadChildren:false)).Result.Count());
        var liveAvatar=new Avatar{Id=Guid.NewGuid(),Username="owned inactive search",IsActive=false};
        var deletedAvatar=new Avatar{Id=Guid.NewGuid(),Username="owned deleted search",DeletedDate=DateTime.UtcNow};
        var liveHolon=new Holon{Id=Guid.NewGuid(),Name="owned inactive search",IsActive=false};
        var deletedHolon=new Holon{Id=Guid.NewGuid(),Name="owned deleted search",DeletedDate=DateTime.UtcNow};
        var verified=false;
        try{
            foreach(var avatar in new[]{liveAvatar,deletedAvatar})Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            foreach(var holon in new[]{liveHolon,deletedHolon})Assert.IsFalse((await provider.SaveHolonAsync(holon,saveChildren:false)).IsError);
            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new(){PlanType="Gold"}}};
            var manager=new ProviderManager(null,dna);manager.RegisterProvider(provider);var router=new OASISHyperDrive(manager);
            foreach(var deleted in new[]{false,true}){
                var avatarId=deleted?deletedAvatar.Id:liveAvatar.Id;var holonId=deleted?deletedHolon.Id:liveHolon.Id;
                var avatarRequest=new StorageOperationRequest{Operation="LoadAvatar",AvatarId=avatarId,PreferredProvider=ProviderType.GoogleCloudOASIS};
                var holonRequest=new StorageOperationRequest{Operation="LoadHolon",HolonId=holonId,LoadChildren=false,PreferredProvider=ProviderType.GoogleCloudOASIS};
                var avatarRead=v2?(synchronous?router.RouteRequest<IAvatar>(avatarRequest):await router.RouteRequestAsync<IAvatar>(avatarRequest)):
                    (synchronous?provider.LoadAvatar(avatarId):await provider.LoadAvatarAsync(avatarId));
                var holonRead=v2?(synchronous?router.RouteRequest<IHolon>(holonRequest):await router.RouteRequestAsync<IHolon>(holonRequest)):
                    (synchronous?provider.LoadHolon(holonId,loadChildren:false):await provider.LoadHolonAsync(holonId,loadChildren:false));
                Assert.AreEqual(deleted&&!v2,avatarRead.IsError,avatarRead.Message);Assert.AreEqual(deleted&&!v2,holonRead.IsError,holonRead.Message);
                if(deleted){Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,avatarRead.ErrorCode);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,holonRead.ErrorCode);
                    Assert.IsNull(avatarRead.Result);Assert.IsNull(holonRead.Result);}
                else{Assert.AreEqual(avatarId,avatarRead.Result.Id);Assert.AreEqual(holonId,holonRead.Result.Id);
                    Assert.IsFalse(avatarRead.Result.IsActive);Assert.IsFalse(holonRead.Result.IsActive);}
            }
            var query=new SearchParams{SearchOnlyForCurrentAvatar=false};
            var request=new StorageOperationRequest{Operation="Search",SearchParams=query,LoadChildren=false,PreferredProvider=ProviderType.GoogleCloudOASIS};
            var result=v2?(synchronous?router.RouteRequest<ISearchResults>(request):await router.RouteRequestAsync<ISearchResults>(request)):
                (synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false));
            Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(2,result.Result.NumberOfResults);
            Assert.AreEqual(liveAvatar.Id,result.Result.SearchResultAvatars.Single().Id);
            Assert.AreEqual(liveHolon.Id,result.Result.SearchResultHolons.Single().Id);
            Assert.IsFalse(result.Result.SearchResultAvatars.Single().IsActive);Assert.IsFalse(result.Result.SearchResultHolons.Single().IsActive);
            verified=true;
        }finally{
            if(verified){foreach(var avatar in new[]{liveAvatar,deletedAvatar})Assert.IsFalse((await provider.DeleteAvatarAsync(avatar.Id,false)).IsError);
                foreach(var holon in new[]{liveHolon,deletedHolon})Assert.IsFalse((await provider.DeleteHolonAsync(holon.Id)).IsError);}
            await provider.DeActivateProviderAsync();
        }
    }
}
