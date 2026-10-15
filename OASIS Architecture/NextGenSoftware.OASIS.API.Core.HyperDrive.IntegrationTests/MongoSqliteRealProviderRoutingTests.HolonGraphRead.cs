using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.Common;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoHolonGraphReadsAsync(MongoDBOASIS mongo,OASISHyperDrive router,bool synchronous,IMongoCollection<NativeHolon> records)
    {
        var root=new Holon {Id=Guid.NewGuid(),IsActive=true,Version=2,Name="graph root"};
        root.MetaData["nested"]=new Dictionary<string,object> {{"label","preserved nested metadata"},{"count",7}};
        var child=new Holon {Id=Guid.NewGuid(),IsActive=true,Version=2,ParentHolonId=root.Id,Name="provider child"};
        var grand=new Holon {Id=Guid.NewGuid(),IsActive=true,Version=2,ParentHolonId=child.Id,Name="provider grandchild"};
        var embeddedGrand=new Holon {Id=grand.Id,IsActive=true,Version=2,ParentHolonId=child.Id,Name="embedded grandchild"};
        var embeddedChild=new Holon {Id=child.Id,IsActive=true,Version=2,ParentHolonId=root.Id,Name="embedded child",Children=new List<IHolon> {embeddedGrand}};
        root.Children=new List<IHolon> {embeddedChild};
        var ids=new[] {root.Id,child.Id,grand.Id};
        try
        {
            var saved=await mongo.SaveHolonAsync(root,saveChildren:false);saved.IsError.Should().BeFalse(saved.Message);
            var key=saved.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
            (await mongo.SaveHolonAsync(child,saveChildren:false)).IsError.Should().BeFalse();
            (await mongo.SaveHolonAsync(grand,saveChildren:false)).IsError.Should().BeFalse();
            foreach(var byKey in new[] {false,true})
            foreach(var children in new[] {false,true})
            foreach(var recursive in new[] {false,true})
            foreach(var depth in new[] {0,1,2})
            foreach(var fromProvider in new[] {false,true})
            {
                var request=new StorageOperationRequest {Operation=byKey ? "LoadHolonByProviderKey":"LoadHolon",HolonId=root.Id,ProviderKey=key,
                    LoadChildren=children,Recursive=recursive,MaxChildDepth=depth,ChildrenFromProvider=fromProvider,Version=2,PreferredProvider=ProviderType.MongoDBOASIS};
                var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);result.IsLoaded.Should().BeTrue();result.Result.Version.Should().Be(2);
                Newtonsoft.Json.JsonConvert.SerializeObject(result.Result.MetaData["nested"]).Should().Contain("preserved nested metadata");
                if(!children) result.Result.Children.Should().BeEmpty();
                else
                {
                    var projected=result.Result.Children.Should().ContainSingle().Which;
                    projected.Id.Should().Be(child.Id);projected.Name.Should().Be(fromProvider ? child.Name:embeddedChild.Name);
                    if(!recursive || depth==1) projected.Children.Should().BeEmpty();
                    else projected.Children.Should().ContainSingle().Which.Name.Should().Be(fromProvider ? grand.Name:embeddedGrand.Name);
                }
            }
            foreach(var fromProvider in new[] {false,true})
            {
                var request=new StorageOperationRequest {Operation="LoadHolon",HolonId=root.Id,LoadChildren=true,ChildrenFromProvider=fromProvider,Version=3,PreferredProvider=ProviderType.MongoDBOASIS};
                var missing=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                missing.IsError.Should().BeFalse();missing.Result.Should().BeNull();missing.ErrorCode.Should().Be(OASISResultErrorCodes.RecordNotFound);
            }
            var invalid=new StorageOperationRequest {Operation="LoadHolon",HolonId=root.Id,MaxChildDepth=-1,PreferredProvider=ProviderType.MongoDBOASIS};
            var invalidDepth=synchronous ? router.RouteRequest<IHolon>(invalid):await router.RouteRequestAsync<IHolon>(invalid);
            invalidDepth.IsError.Should().BeTrue();invalidDepth.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
            await records.UpdateOneAsync(row=>row.HolonId==root.Id,Builders<NativeHolon>.Update.Set(row=>row.ParentHolonId,grand.Id));
            var cycle=new StorageOperationRequest {Operation="LoadHolon",HolonId=root.Id,LoadChildren=true,ChildrenFromProvider=true,Recursive=true,PreferredProvider=ProviderType.MongoDBOASIS};
            var corrupted=synchronous ? router.RouteRequest<IHolon>(cycle):await router.RouteRequestAsync<IHolon>(cycle);
            corrupted.IsError.Should().BeTrue();corrupted.Result.Should().BeNull();corrupted.Exception.Should().BeOfType<InvalidDataException>();
            corrupted.ErrorCode.Should().Be(Core.Helpers.StorageLifecycleConflictException.ErrorCode);
        }
        finally {await records.DeleteManyAsync(row=>ids.Contains(row.HolonId));}
    }
}
