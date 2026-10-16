using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativePointReadsRejectCanonicalIdentityMismatch(bool synchronous)
    {
        var id=Guid.NewGuid();var other=Guid.NewGuid();
        foreach(var detail in new[] {false,true})
        {
            var table=detail ? "OasisAvatarDetails":"OasisAvatars";
            var json=detail ? NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(new AvatarDetail {Id=other,XP=999})
                :NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(new Avatar {Id=other,Username="owned-impostor"});
            await Client.PutItemAsync(new Amazon.DynamoDBv2.Model.PutItemRequest {TableName=table,Item=new() {
                ["id"]=new() {S=id.ToString()},["data_json"]=new() {S=json}}});
            if(detail)
            {
                var result=synchronous ? _provider.LoadAvatarDetail(id):await _provider.LoadAvatarDetailAsync(id);
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
            }
            else
            {
                var result=synchronous ? _provider.LoadAvatar(id):await _provider.LoadAvatarAsync(id);
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
            }
            await Client.DeleteItemAsync(new Amazon.DynamoDBv2.Model.DeleteItemRequest {TableName=table,Key=Key(id)});
        }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task V2SelectorAbsenceDoesNotReturnStaleSecondary(bool synchronous)
    {
        var path=Path.GetFullPath("TestResults/AWSRoutingFiles/delete-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var id=Guid.NewGuid();var username="owned-stale-"+id.ToString("N");var email=username+"@example.invalid";
            Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username=username,Email=email,IsActive=true,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
            Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=username,Email=email,IsActive=true,XP=999})).IsError);
            Assert.AreEqual(id,(await sqlite.LoadAvatarByUsernameAsync(username)).Result.Id);
            Assert.AreEqual(id,(await sqlite.LoadAvatarDetailByEmailAsync(email)).Result.Id);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AWSOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            void Missing<T>(OASISResult<T> result)
            {
                Assert.IsNull(result.Result);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
            }
            foreach(var detail in new[] {false,true})
            foreach(var byEmail in new[] {false,true})
            {
                var request=new Core.StorageOperationRequest {Operation=(detail ? "LoadAvatarDetailBy":"LoadAvatarBy")+(byEmail ? "Email":"Username"),
                    Username=username,Email=email,PreferredProvider=Core.Enums.ProviderType.AWSOASIS};
                if(detail) Missing(synchronous ? router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request));
                else Missing(synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request));
            }
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            var other=Guid.NewGuid();
            foreach(var detail in new[] {false,true})
            {
                var table=detail ? "OasisAvatarDetails":"OasisAvatars";
                var json=detail ? NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(new AvatarDetail {Id=other,XP=888})
                    :NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(new Avatar {Id=other,Username="owned-impostor"});
                await Client.PutItemAsync(new Amazon.DynamoDBv2.Model.PutItemRequest {TableName=table,Item=new() {
                    ["id"]=new() {S=id.ToString()},["data_json"]=new() {S=json}}});
                var request=new Core.StorageOperationRequest {Operation=detail ? "LoadAvatarDetail":"LoadAvatar",AvatarId=id,PreferredProvider=Core.Enums.ProviderType.AWSOASIS};
                void Corrupt<T>(OASISResult<T> result)
                {
                    Assert.IsTrue(result.IsError,"Corrupt primary must not be masked by stale secondary.");Assert.IsNull(result.Result);
                    Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                }
                if(detail) Corrupt(synchronous ? router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request));
                else Corrupt(synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request));
                await Client.DeleteItemAsync(new Amazon.DynamoDBv2.Model.DeleteItemRequest {TableName=table,Key=Key(id)});
            }
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            verified=true;
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }
}
