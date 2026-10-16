using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using SQLiteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false,false)] [DataRow(true,false,false)]
    [DataRow(false,true,false)] [DataRow(true,true,false)]
    [DataRow(false,false,true)] [DataRow(true,false,true)]
    [DataRow(false,true,true)] [DataRow(true,true,true)]
    public async Task NativeHardDeleteReplicatesBetweenCosmosAndSqlite(bool synchronous,bool sqlitePrimary,bool avatar)
    {
        var path=Path.GetFullPath("TestResults/CosmosDBRouting/delete-backup.db");
        if(File.Exists(path)) Assert.Fail("Retained delete fixture exists; reconcile it before rerunning.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var backup=new SQLiteProvider($"Data Source={path};Pooling=False");var verified=false;
        var id=Guid.NewGuid();
        try
        {
            Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            foreach(var store in new IOASISStorageProvider[] {_provider,backup})
            {
                if(avatar)
                {
                    var profile=await store.SaveAvatarAsync(new Avatar {Id=id,Username="owned delete",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)});
                    Assert.IsFalse(profile.IsError,profile.Message);
                    var detail=await store.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned delete",IsActive=true,XP=51});
                    Assert.IsFalse(detail.IsError,detail.Message);
                }
                else
                {
                    var saved=await store.SaveHolonAsync(new Holon {Id=id,Name="owned delete",IsActive=true},saveChildren:false);
                    Assert.IsFalse(saved.IsError,saved.Message);
                }
            }
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings() }};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoReplicationEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(backup);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(new[] {
                new EnumValue<ProviderType>(ProviderType.AzureCosmosDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new OASISHyperDrive(manager);
            var request=new StorageOperationRequest {Operation=avatar ? "DeleteAvatar":"DeleteHolon",HolonId=id,AvatarId=id,SoftDelete=false,
                PreferredProvider=sqlitePrimary ? ProviderType.SQLLiteDBOASIS:ProviderType.AzureCosmosDBOASIS};
            async Task Check<T>()
            {
                var result=synchronous ? router.RouteRequest<T>(request):await router.RouteRequestAsync<T>(request);
                Assert.IsFalse(result.IsError,result.Message);Assert.IsFalse(result.IsWarning,result.Message);
                Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,result.DeletedCount);
            }
            if(avatar) await Check<bool>();else await Check<IHolon>();
            foreach(var store in new IOASISStorageProvider[] {_provider,backup})
            {
                if(avatar)
                {
                    Assert.IsNull((await store.LoadAvatarAsync(id)).Result);
                    Assert.IsNull((await store.LoadAvatarDetailAsync(id)).Result);
                }
                else Assert.IsNull((await store.LoadHolonAsync(id,loadChildren:false)).Result);
            }
            verified=true;
        }
        finally
        {
            backup.Dispose();
            if(verified) File.Delete(path);else _ownsDatabase=false;
        }
    }
}
