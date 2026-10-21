using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;
using NativeAvatar=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using NativeDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoSerializationFailuresAsync(MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,OASISDNA dna,bool synchronous,IMongoDatabase database)
    {
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(mongo);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        foreach(var kind in new[] {"holon","avatar","detail"})
        foreach(var updating in new[] {false,true})
        {
            var id=Guid.NewGuid();var name="serialization-"+id.ToString("N");
            Avatar Owner() => new() {Id=id,Username=name,Email=name+"@test.local",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
            IHolonBase candidate=kind=="holon" ? new Holon {Id=id,IsActive=true,Name="retained native"}
                :kind=="avatar" ? Owner():new AvatarDetail {Id=id,Username=name,Email=name+"@test.local",Karma=41,XP=43};
            candidate.MetaData["retained"]="unchanged";
            var collection=kind=="holon" ? "Holon":kind=="avatar" ? "Avatar":"AvatarDetail";
            var raw=database.GetCollection<BsonDocument>(collection);
            async Task<string?> NativeKey() => kind=="holon"
                ? await database.GetCollection<NativeHolon>(collection).Find(row=>row.HolonId==id).Project(row=>row.Id).SingleOrDefaultAsync()
                :kind=="avatar" ? await database.GetCollection<NativeAvatar>(collection).Find(row=>row.HolonId==id).Project(row=>row.Id).SingleOrDefaultAsync()
                :await database.GetCollection<NativeDetail>(collection).Find(row=>row.HolonId==id).Project(row=>row.Id).SingleOrDefaultAsync();
            async Task<OASISResult<IHolonBase>> Save(bool routed)
            {
                var request=new StorageOperationRequest {Operation=kind=="holon" ? "SaveHolon":kind=="avatar" ? "SaveAvatar":"SaveAvatarDetail",Payload=candidate,SaveChildren=false,PreferredProvider=ProviderType.MongoDBOASIS};
                if(kind=="holon") return NormalizeSerializationResult(routed
                    ? (synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request))
                    :await mongo.SaveHolonAsync((IHolon)candidate,saveChildren:false));
                if(kind=="avatar") return NormalizeSerializationResult(routed
                    ? (synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request))
                    :await mongo.SaveAvatarAsync((IAvatar)candidate));
                return NormalizeSerializationResult(routed
                    ? (synchronous ? router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request))
                    :await mongo.SaveAvatarDetailAsync((IAvatarDetail)candidate));
            }
            try
            {
                if(kind=="holon") (await sqlite.SaveHolonAsync(new Holon {Id=id,IsActive=true,Name="retained SQL"},saveChildren:false)).IsError.Should().BeFalse();
                else
                {
                    if(kind=="detail") (await mongo.SaveAvatarAsync(Owner())).IsError.Should().BeFalse();
                    (await sqlite.SaveAvatarAsync(Owner())).IsError.Should().BeFalse();
                    (await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=name,Email=name+"@test.local",Karma=17,XP=19})).IsError.Should().BeFalse();
                }
                if(updating) (await Save(false)).IsError.Should().BeFalse();
                var keyBefore=await NativeKey();
                var before=keyBefore==null ? null:(await raw.Find(new BsonDocument("_id",ObjectId.Parse(keyBefore))).SingleAsync()).ToBson();
                candidate.MetaData["unsupported"]=new Action(()=>{});
                var sqlBefore=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                var result=await Save(true);
                var keyAfter=await NativeKey();
                if(!result.IsError)
                {
                    keyAfter.Should().NotBeNull();
                    (await raw.Find(new BsonDocument("_id",ObjectId.Parse(keyAfter!))).SingleAsync())["MetaData"]["unsupported"].IsBsonNull
                        .Should().BeFalse("a reported successful save must not silently replace the unsupported value with null");
                }
                result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.Result.Should().BeNull();result.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
                if(kind=="holon")
                {
                    // Holon graph capture now rejects unsupported input before BSON dispatch.
                    result.Exception.Should().BeOfType<Newtonsoft.Json.JsonSerializationException>();
                    result.Exception!.Message.Should().Contain("System.Action");
                }
                else
                {
                    result.Exception.Should().BeAssignableTo<BsonSerializationException>();
                    result.Exception!.GetBaseException().Should().BeOfType<NotSupportedException>();
                }
                Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeFalse("payload serialization fails before a native write is dispatched");
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(sqlBefore);
                if(!updating) keyAfter.Should().BeNull();
                else (await raw.Find(new BsonDocument("_id",ObjectId.Parse(keyAfter!))).SingleAsync()).ToBson().Should().Equal(before);
                if(kind=="holon") (await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name.Should().Be("retained SQL");
                else {var retained=(await sqlite.LoadAvatarDetailAsync(id)).Result;retained.Karma.Should().Be(17);retained.XP.Should().Be(19);}
            }
            finally
            {
                var key=await NativeKey();if(key!=null) await raw.DeleteOneAsync(new BsonDocument("_id",ObjectId.Parse(key)));
                if(kind=="holon") {if((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result!=null) (await sqlite.DeleteHolonAsync(id)).IsError.Should().BeFalse();}
                else
                {
                    var ownerKey=await database.GetCollection<NativeAvatar>("Avatar").Find(row=>row.HolonId==id).Project(row=>row.Id).SingleOrDefaultAsync();
                    if(ownerKey!=null) await database.GetCollection<BsonDocument>("Avatar").DeleteOneAsync(new BsonDocument("_id",ObjectId.Parse(ownerKey)));
                    if((await sqlite.LoadAvatarAsync(id)).Result!=null) (await sqlite.DeleteAvatarAsync(id,softDelete:false)).IsError.Should().BeFalse();
                }
            }
        }
    }

    private static OASISResult<IHolonBase> NormalizeSerializationResult<T>(OASISResult<T> source) where T:IHolonBase
    {
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<T,IHolonBase>(source);result.Result=source.Result;return result;
    }
}
