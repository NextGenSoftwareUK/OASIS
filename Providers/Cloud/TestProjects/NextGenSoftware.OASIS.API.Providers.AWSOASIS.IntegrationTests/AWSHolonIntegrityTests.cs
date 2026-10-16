using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false,false)] [DataRow(true,false,false)] [DataRow(false,true,false)] [DataRow(true,true,false)]
    [DataRow(false,false,true)] [DataRow(true,false,true)] [DataRow(false,true,true)] [DataRow(true,true,true)]
    public async Task NativeDeletionProjectionCannotHideOrResurrectRecords(bool synchronous,bool avatar,bool canonicalDeleted)
    {
        var id=Guid.NewGuid();var table=avatar ? "OasisAvatars":"OasisHolons";
        var deleted=canonicalDeleted ? DateTime.UtcNow:DateTime.MinValue;
        var json=avatar ? OasisJson.Serialize(new Avatar {Id=id,Username="owned-deletion-projection",DeletedDate=deleted,IsActive=true})
            :OasisJson.Serialize(new Holon {Id=id,Name="owned-deletion-projection",DeletedDate=deleted,IsActive=true});
        Assert.AreEqual(System.Net.HttpStatusCode.OK,(await Client.PutItemAsync(new PutItemRequest {TableName=table,Item=new() {
            ["id"]=new() {S=id.ToString()},["username"]=new() {S="owned-deletion-projection"},["is_deleted"]=new() {BOOL=!canonicalDeleted},["data_json"]=new() {S=json}}})).HttpStatusCode);
        try
        {
            void Corrupt<T>(OASISResult<T> r) {Assert.IsTrue(r.IsError);Assert.IsNull(r.Result);Assert.IsFalse(r.IsLoaded);Assert.IsInstanceOfType(r.Exception,typeof(InvalidDataException));}
            if(avatar)
            {
                Corrupt(synchronous ? _provider.LoadAvatar(id):await _provider.LoadAvatarAsync(id));
                Corrupt(synchronous ? _provider.LoadAllAvatars():await _provider.LoadAllAvatarsAsync());
                Corrupt(synchronous ? _provider.LoadAvatarByUsername("owned-deletion-projection"):await _provider.LoadAvatarByUsernameAsync("owned-deletion-projection"));
            }
            else
            {
                Corrupt(synchronous ? _provider.LoadHolon(id,loadChildren:false):await _provider.LoadHolonAsync(id,loadChildren:false));
                Corrupt(synchronous ? _provider.LoadAllHolons(loadChildren:false):await _provider.LoadAllHolonsAsync(loadChildren:false));
                Corrupt(synchronous ? _provider.LoadHolonsForParent(Guid.NewGuid(),loadChildren:false):await _provider.LoadHolonsForParentAsync(Guid.NewGuid(),loadChildren:false));
            }
            var raw=(await Client.GetItemAsync(new GetItemRequest {TableName=table,Key=Key(id),ConsistentRead=true})).Item;
            Assert.AreEqual(json,raw["data_json"].S);Assert.AreEqual(!canonicalDeleted,raw["is_deleted"].BOOL);
        }
        finally {await Client.DeleteItemAsync(new DeleteItemRequest {TableName=table,Key=Key(id)});}
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task V2ProjectionCorruptionDoesNotReadStaleSecondary(bool synchronous,bool wrongType)
    {
        var path=Path.GetFullPath("TestResults/AWSRoutingFiles/delete-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained routing evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var id=Guid.NewGuid();var parent=Guid.NewGuid();var nativeParent=wrongType ? parent:Guid.NewGuid();
        var record=new Holon {Id=id,ParentHolonId=parent,Name="owned-projection-mismatch",IsActive=true};
        var json=OasisJson.Serialize(record);var acknowledged=false;var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon {Id=id,ParentHolonId=parent,Name="owned-stale",IsActive=true},saveChildren:false)).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AWSOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            Assert.AreEqual(System.Net.HttpStatusCode.OK,(await Client.PutItemAsync(new PutItemRequest {TableName="OasisHolons",Item=new() {
                ["id"]=new() {S=id.ToString()},["parentHolonId"]=new() {S=nativeParent.ToString()},
                ["holon_type"]=new() {N=((int)record.HolonType+(wrongType ? 1:0)).ToString()},["data_json"]=new() {S=json}}})).HttpStatusCode);
            acknowledged=true;
            void Corrupt<T>(OASISResult<T> result) {Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));}
            var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            foreach(var operation in new[] {"LoadHolon","LoadAllHolons","LoadHolonsForParent"})
            {
                var request=new Core.StorageOperationRequest {Operation=operation,HolonId=operation=="LoadHolonsForParent" ? parent:id,
                    LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.AWSOASIS};
                if(operation=="LoadHolon") Corrupt(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
                else Corrupt(synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request));
            }
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            var raw=(await Client.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(id),ConsistentRead=true})).Item;
            Assert.AreEqual(json,raw["data_json"].S);Assert.AreEqual(nativeParent.ToString(),raw["parentHolonId"].S);
            Assert.AreEqual("owned-stale",(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            verified=true;
        }
        finally
        {
            if(acknowledged) await Client.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(id)});
            await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeHolonProjectionMismatchIsNotSilentlyFiltered(bool synchronous,bool wrongType)
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();
        var record=new Holon {Id=id,ParentHolonId=parent,Name="owned-projection-mismatch",IsActive=true};
        var nativeParent=wrongType ? parent:Guid.NewGuid();var json=OasisJson.Serialize(record);
        await Client.PutItemAsync(new PutItemRequest {TableName="OasisHolons",Item=new() {
            ["id"]=new() {S=id.ToString()},["parentHolonId"]=new() {S=nativeParent.ToString()},
            ["holon_type"]=new() {N=((int)record.HolonType+(wrongType ? 1:0)).ToString()},["data_json"]=new() {S=json}}});
        try
        {
            void Corrupt<T>(OASISResult<T> result) {Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));}
            Corrupt(synchronous ? _provider.LoadHolon(id,loadChildren:false):await _provider.LoadHolonAsync(id,loadChildren:false));
            Corrupt(synchronous ? _provider.LoadAllHolons(loadChildren:false):await _provider.LoadAllHolonsAsync(loadChildren:false));
            Corrupt(synchronous ? _provider.LoadHolonsForParent(nativeParent,loadChildren:false):await _provider.LoadHolonsForParentAsync(nativeParent,loadChildren:false));
            Corrupt(synchronous ? _provider.LoadHolonsForParent(parent,loadChildren:false):await _provider.LoadHolonsForParentAsync(parent,loadChildren:false));
            Assert.AreEqual(json,(await Client.GetItemAsync(new GetItemRequest {TableName="OasisHolons",Key=Key(id),ConsistentRead=true})).Item["data_json"].S);
        }
        finally {await Client.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(id)});}
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeHolonPointCollectionAndParentReadsRejectCorruption(bool synchronous,bool malformed)
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();
        var json=malformed ? "{ invalid canonical JSON":OasisJson.Serialize(new Holon {Id=Guid.NewGuid(),ParentHolonId=parent,Name="owned-wrong-holon",IsActive=true});
        await Client.PutItemAsync(new PutItemRequest {TableName="OasisHolons",Item=new() {
            ["id"]=new() {S=id.ToString()},["parentHolonId"]=new() {S=parent.ToString()},["data_json"]=new() {S=json}}});
        try
        {
            void Corrupt<T>(OASISResult<T> result)
            {
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                if(malformed) Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
            }
            Corrupt(synchronous ? _provider.LoadHolon(id,loadChildren:false):await _provider.LoadHolonAsync(id,loadChildren:false));
            Corrupt(synchronous ? _provider.LoadAllHolons(loadChildren:false):await _provider.LoadAllHolonsAsync(loadChildren:false));
            Corrupt(synchronous ? _provider.LoadHolonsForParent(parent,loadChildren:false):await _provider.LoadHolonsForParentAsync(parent,loadChildren:false));
        }
        finally {await Client.DeleteItemAsync(new DeleteItemRequest {TableName="OasisHolons",Key=Key(id)});}
    }
}
