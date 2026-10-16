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
