using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

[TestClass]
public class AzureNativeStorageTests
{
    [TestMethod]
    public async Task OfficialBlobSdkPreservesProfileDetailAndHolon()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_AZURE_NATIVE_TEST"),"Use the owned reusable runner.");
        const string connection="UseDevelopmentStorage=true";
        var names=new[] {"oasis-native-holons","oasis-native-avatars","oasis-native-details"};
        var service=new BlobServiceClient(connection);
        foreach(var name in names) Assert.IsFalse((await service.GetBlobContainerClient(name).ExistsAsync()).Value,"Refusing existing fixture container.");
        var provider=new AzureStorageOASIS(connection,names[0],names[1],names[2]);
        try {
            var active=await provider.ActivateProviderAsync();
            Assert.IsFalse(active.IsError,active.Message);Assert.IsTrue(provider.IsProviderActivated);
            var id=Guid.NewGuid();
            var avatar=new Avatar {Id=id,Username="owned-native",Email="owned@example.invalid",FirstName="Actual",LastName="Profile",Description="full profile"};
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            var loaded=await provider.LoadAvatarAsync(id);
            Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual("Actual",loaded.Result.FirstName);Assert.AreEqual("full profile",loaded.Result.Description);
            Assert.AreEqual(id,(await provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id);
            var detail=new AvatarDetail {Id=id,Username=avatar.Username,Email=avatar.Email,XP=321,Karma=29};
            detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned seed"});
            var saved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Message);
            var read=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(read.IsError,read.Message);
            Assert.AreEqual(321,read.Result.XP);Assert.AreEqual(29L,read.Result.Karma);
            Assert.AreEqual(detail.Inventory[0].Id,read.Result.Inventory.Single().Id);
            Assert.AreEqual(id,(await provider.LoadAvatarDetailByEmailAsync("OWNED@example.invalid")).Result.Id);
            Assert.AreEqual(1,(await provider.LoadAllAvatarDetailsAsync()).Result.Count());
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=Guid.NewGuid(),Username=detail.Username,Email=detail.Email})).IsError);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(Guid.NewGuid())).ErrorCode);
            var holon=new Holon {Id=Guid.NewGuid(),Name="owned root",Description="stored description",ParentHolonId=Guid.NewGuid(),CreatedByAvatarId=id};
            var written=await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(written.IsError,written.Message);
            var stored=await provider.LoadHolonAsync(holon.Id,loadChildren:false);Assert.IsFalse(stored.IsError,stored.Message);
            Assert.AreEqual(holon.Id,stored.Result.Id);Assert.AreEqual(holon.ParentHolonId,stored.Result.ParentHolonId);Assert.AreEqual(holon.Name,stored.Result.Name);
            await VerifyV2Async(provider,holon);
            var absentContainer=new AzureStorageOASIS(connection,"oasis-native-absent",names[1],names[2]);
            var operational=await absentContainer.LoadHolonAsync(Guid.NewGuid(),loadChildren:false);
            Assert.IsTrue(operational.IsError);Assert.IsInstanceOfType(operational.Exception,typeof(Azure.RequestFailedException));
            Assert.AreNotEqual(OASISResultErrorCodes.RecordNotFound,operational.ErrorCode);
        }
        finally {
            await provider.DeActivateProviderAsync();
            foreach(var name in names) await service.GetBlobContainerClient(name).DeleteIfExistsAsync();
        }
        Assert.IsFalse(provider.IsProviderActivated);
    }

    private static async Task VerifyV2Async(AzureStorageOASIS azure,Holon native)
    {
        var directory=Path.GetFullPath("TestResults/AzureRoutingFiles");
        if(Directory.Exists(directory) && Directory.EnumerateFiles(directory,"*",SearchOption.AllDirectories).Any())
            Assert.Fail("Owned Azure routing fixture contains prior files; refusing overwrite.");
        var local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:directory);
        Assert.IsFalse((await local.ActivateProviderAsync()).IsError);
        try {
            Assert.IsFalse((await local.SaveHolonAsync(new Holon {Id=native.Id,Name="distinct-local"},saveChildren:false)).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna) {
                IsAutoLoadBalanceEnabled=true,IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(azure);manager.RegisterProvider(local);
            var providers=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.LocalFileOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            var router=new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var counts=new Dictionary<string,int>();
            for(int i=0;i<6;i++) {
                var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="LoadHolon",HolonId=native.Id};
                var read=i%2==0 ? router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request,Core.Enums.LoadBalancingStrategy.RoundRobin)
                    :await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request,Core.Enums.LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError,read.Message);counts[read.Result.Name]=counts.GetValueOrDefault(read.Result.Name)+1;
            }
            Assert.AreEqual(3,counts[native.Name]);Assert.AreEqual(3,counts["distinct-local"]);
            manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoReplicationEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
            foreach(var primary in providers)
            foreach(var asynchronous in new[] {false,true}) {
                var value=new Holon {Id=native.Id,Name="replicated-"+primary.Value+"-"+asynchronous};
                var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="SaveHolon",Payload=value,SaveChildren=false,PreferredProvider=primary.Value};
                var saved=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request)
                    :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
                Assert.AreEqual(value.Name,(await azure.LoadHolonAsync(native.Id,loadChildren:false)).Result.Name);
                Assert.AreEqual(value.Name,(await local.LoadHolonAsync(native.Id,loadChildren:false)).Result.Name);
            }
            manager.IsAutoReplicationEnabled=false;manager.IsAutoFailOverEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            var missingId=Guid.NewGuid();
            try {
                Assert.IsFalse((await local.SaveHolonAsync(new Holon {Id=missingId,Name="must-not-return-stale"},saveChildren:false)).IsError);
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests;
                foreach(var asynchronous in new[] {false,true}) {
                    var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {
                        Operation="LoadHolon",HolonId=missingId,PreferredProvider=Core.Enums.ProviderType.AzureStorageOASIS};
                    var read=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request)
                        :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>(request);
                    Assert.IsNull(read.Result);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,read.ErrorCode);
                }
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await azure.LoadAvatarAsync(missingId)).ErrorCode);
            }
            finally {await local.DeleteHolonAsync(missingId);}
        }
        finally {await local.DeleteHolonAsync(native.Id);await local.DeActivateProviderAsync();}
    }
}
