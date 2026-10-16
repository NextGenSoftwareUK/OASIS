using System.Text;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
    [DataRow(false,"avatar")]
    [DataRow(true,"avatar")]
    [DataRow(false,"detail")]
    [DataRow(true,"detail")]
    [DataRow(false,"holon")]
    [DataRow(true,"holon")]
    public async Task NativeCorruptRecordCannotFallBackToStaleSqlite(bool synchronous,string kind)
    {
        var id=Guid.NewGuid();var key=id.ToString();
        var collection=kind=="avatar" ? "avatarItems":kind=="detail" ? "avatarDetailItems":"holonItems";
        var container=_client.GetContainer(DatabaseName,collection);
        var json=$"{{\"id\":\"{id}\",\"Id\":\"{id}\",\"IsActive\":true,\"Version\":\"not-an-integer\"}}";
        using(var payload=new MemoryStream(Encoding.UTF8.GetBytes(json)))
        using(var created=await container.CreateItemStreamAsync(payload,new PartitionKey(key))) created.EnsureSuccessStatusCode();
        var path=Path.GetFullPath("TestResults/CosmosDBRouting/corruption-backup.db");
        if(File.Exists(path)) Assert.Fail("Existing corruption fixture requires inspection; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var backup=new SQLiteProvider($"Data Source={path};Pooling=False");
        var verified=false;
        try
        {
            Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await backup.SaveAvatarAsync(new Avatar {Id=id,Username="stale avatar",AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError);
            Assert.IsFalse((await backup.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="stale detail",XP=41})).IsError);
            Assert.IsFalse((await backup.SaveHolonAsync(new Holon {Id=id,Name="stale holon"},saveChildren:false)).IsError);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings() }};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(backup);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {
                new EnumValue<ProviderType>(ProviderType.AzureCosmosDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new OASISHyperDrive(manager);
            async Task Check<T>(string operation)
            {
                var request=new StorageOperationRequest {Operation=operation,AvatarId=id,HolonId=id,LoadChildren=false,PreferredProvider=ProviderType.AzureCosmosDBOASIS};
                var result=synchronous ? router.RouteRequest<T>(request):await router.RouteRequestAsync<T>(request);
                Assert.IsTrue(result.IsError,result.Message);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
            }
            if(kind=="avatar") {await Check<IAvatar>("LoadAvatar");await Check<IEnumerable<IAvatar>>("LoadAllAvatars");}
            else if(kind=="detail") {await Check<IAvatarDetail>("LoadAvatarDetail");await Check<IEnumerable<IAvatarDetail>>("LoadAllAvatarDetails");}
            else {await Check<IHolon>("LoadHolon");await Check<IEnumerable<IHolon>>("LoadAllHolons");}
            Assert.AreEqual(0L,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.AreEqual("stale avatar",(await backup.LoadAvatarAsync(id)).Result.Username);
            Assert.AreEqual(41L,Convert.ToInt64((await backup.LoadAvatarDetailAsync(id)).Result.XP));
            Assert.AreEqual("stale holon",(await backup.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            using(var response=await container.ReadItemStreamAsync(key,new PartitionKey(key)))
            {
                response.EnsureSuccessStatusCode();using var reader=new StreamReader(response.Content);
                var retained=JObject.Parse(await reader.ReadToEndAsync());
                Assert.AreEqual("not-an-integer",retained["Version"]!.Value<string>());
                Assert.AreEqual(key,retained["Id"]!.Value<string>());
            }
            verified=true;
        }
        finally
        {
            backup.Dispose();
            if(verified) File.Delete(path);
            else _ownsDatabase=false; // Keep failed native/secondary evidence for reconciliation.
        }
    }
}
