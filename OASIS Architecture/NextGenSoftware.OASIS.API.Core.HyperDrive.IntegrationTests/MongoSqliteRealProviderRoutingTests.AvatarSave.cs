using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using NativeAvatar=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using NativeDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoAvatarSaveWriteConcernAsync(MongoDBOASIS fault,MongoDBOASIS healthy,SQLLiteDBOASIS sqlite,
        OASISDNA dna,IMongoDatabase admin,string app,bool synchronous,IMongoDatabase database)
    {
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(fault);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        var avatars=database.GetCollection<NativeAvatar>("Avatar");var details=database.GetCollection<NativeDetail>("AvatarDetail");
        foreach(var detail in new[] {false,true})
        foreach(var updating in new[] {false,true})
        {
            var id=Guid.NewGuid();var name="ack-"+id.ToString("N");
            var avatar=new Avatar {Id=id,Username=name,Email=name+"@test.local",IsActive=true,FirstName="retained SQLite profile",AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
            var state=new AvatarDetail {Id=id,Username=name,Email=avatar.Email,Karma=17,XP=13};
            var pairPrepared=false;
            try
            {
                if(detail || updating) (await healthy.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
                if(detail && updating) (await healthy.SaveAvatarDetailAsync(state)).IsError.Should().BeFalse();
                (await sqlite.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
                (await sqlite.SaveAvatarDetailAsync(state)).IsError.Should().BeFalse();
                pairPrepared=true;
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {updating ? "update":"insert"}},{"appName",app},
                        {"writeConcernError",new BsonDocument {{"code",64},{"errmsg","owned avatar save post-command write concern fault"}}}}}});
                avatar.FirstName="native profile write completed";state.Karma=101;state.XP=202;
                var request=new StorageOperationRequest {Operation=detail ? "SaveAvatarDetail":"SaveAvatar",Payload=detail ? state:avatar,PreferredProvider=ProviderType.MongoDBOASIS};
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                Exception? cause;
                if(detail)
                {
                    var result=synchronous ? router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request);
                    result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.Result.Should().BeNull();cause=result.Exception;
                }
                else
                {
                    var result=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                    result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.Result.Should().BeNull();cause=result.Exception;
                }
                var failure=cause.Should().BeOfType<MongoMutationReconciliationException>().Which;
                failure.RecordId.Should().Be(id);failure.OriginalTransactionId.Should().BeEmpty();
                var native=failure.InnerException;
                (native is MongoWriteConcernException || native is MongoWriteException write && write.WriteConcernError?.Code==64).Should().BeTrue();
                Core.Helpers.NativeMutationReconciliation.IsRequired(failure).Should().BeTrue();
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                if(detail)
                {
                    var persisted=await details.Find(row=>row.HolonId==id).SingleAsync();persisted.Karma.Should().Be(101);persisted.XP.Should().Be(202);
                    var retained=(await sqlite.LoadAvatarDetailAsync(id)).Result;retained.Karma.Should().Be(17);retained.XP.Should().Be(13);
                }
                else
                {
                    (await avatars.Find(row=>row.HolonId==id).SingleAsync()).FirstName.Should().Be("native profile write completed");
                    (await sqlite.LoadAvatarAsync(id)).Result.FirstName.Should().Be("retained SQLite profile");
                }
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                await details.DeleteManyAsync(row=>row.HolonId==id);await avatars.DeleteManyAsync(row=>row.HolonId==id);
                if(pairPrepared)
                {
                    var cleanup=await sqlite.DeleteAvatarAsync(id,softDelete:false);
                    cleanup.IsError.Should().BeFalse(cleanup.Exception?.ToString() ?? cleanup.Message);
                }
            }
        }
    }
}
