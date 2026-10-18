using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NativeAvatar=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using NativeDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoAvatarTransactionConflictsAsync(MongoDBOASIS fault,MongoDBOASIS healthy,SQLLiteDBOASIS sqlite,
        OASISDNA dna,IMongoDatabase admin,string app,bool synchronous,IMongoDatabase database)
    {
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(fault);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);var avatars=database.GetCollection<NativeAvatar>("Avatar");var details=database.GetCollection<NativeDetail>("AvatarDetail");
        foreach(var variant in new[] {"pairSoft","pairHard","detailSoft","detailHard","tombstone"})
        {
            var id=Guid.NewGuid();var name="avatar-conflict-"+id.ToString("N");
            Avatar Owner(string first) => new() {Id=id,Username=name,Email=name+"@test.local",FirstName=first,IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
            var detailOnly=variant.StartsWith("detail",StringComparison.Ordinal);var soft=!variant.EndsWith("Hard",StringComparison.Ordinal);
            Task<OASISResult<bool>>? pending=null;
            try
            {
                (await healthy.SaveAvatarAsync(Owner("native profile"))).IsError.Should().BeFalse();
                (await healthy.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=name,Email=name+"@test.local",Karma=11,XP=13})).IsError.Should().BeFalse();
                (await sqlite.SaveAvatarAsync(Owner("stale SQL profile"))).IsError.Should().BeFalse();
                (await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=name,Email=name+"@test.local",Karma=17,XP=19})).IsError.Should().BeFalse();
                var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode",new BsonDocument("times",1)},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {soft ? "update":"delete"}},{"appName",app},{"blockConnection",true},{"blockTimeMS",1500}}}});
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                if(variant=="tombstone")
                {
                    var evidence=new AvatarTombstone(id,DateTime.UtcNow,Guid.NewGuid());
                    pending=synchronous ? Task.Run(()=>fault.ApplyAvatarTombstone(evidence)):fault.ApplyAvatarTombstoneAsync(evidence);
                }
                else
                {
                    var request=new StorageOperationRequest {Operation=detailOnly ? "DeleteAvatarDetail":"DeleteAvatar",AvatarId=id,SoftDelete=soft,PreferredProvider=ProviderType.MongoDBOASIS};
                    pending=synchronous ? Task.Run(()=>router.RouteRequest<bool>(request)):router.RouteRequestAsync<bool>(request);
                }
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"waitForFailPoint","failCommand"},{"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",5000}});
                if(detailOnly) await details.UpdateOneAsync(row=>row.HolonId==id,Builders<NativeDetail>.Update.Set(row=>row.Karma,777));
                else await avatars.UpdateOneAsync(row=>row.HolonId==id,Builders<NativeAvatar>.Update.Set(row=>row.FirstName,"concurrent winner"));
                var result=await pending;
                result.IsError.Should().BeTrue();result.Result.Should().BeFalse();result.IsDeleted.Should().BeFalse();result.ErrorCode.Should().Be(StorageLifecycleConflictException.ErrorCode);
                ContainsMongoWriteConflict(result.Exception).Should().BeTrue();NativeMutationReconciliation.IsRequired(result.Exception).Should().BeFalse();
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                var nativeAvatar=await avatars.Find(row=>row.HolonId==id).SingleAsync();var nativeDetail=await details.Find(row=>row.HolonId==id).SingleAsync();
                nativeAvatar.FirstName.Should().Be(detailOnly ? "native profile":"concurrent winner");nativeDetail.Karma.Should().Be(detailOnly ? 777:11);nativeDetail.XP.Should().Be(13);
                nativeAvatar.DeletedDate.Should().Be(DateTime.MinValue);nativeDetail.DeletedDate.Should().Be(DateTime.MinValue);nativeAvatar.IsActive.Should().BeTrue();nativeDetail.IsActive.Should().BeTrue();
                (await sqlite.LoadAvatarAsync(id)).Result.FirstName.Should().Be("stale SQL profile");
                var retained=(await sqlite.LoadAvatarDetailAsync(id)).Result;retained.Karma.Should().Be(17);retained.XP.Should().Be(19);retained.DeletedDate.Should().Be(DateTime.MinValue);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                if(pending!=null) await pending;
                await details.DeleteManyAsync(row=>row.HolonId==id);await avatars.DeleteManyAsync(row=>row.HolonId==id);
                if((await sqlite.LoadAvatarAsync(id)).Result!=null) (await sqlite.DeleteAvatarAsync(id,softDelete:false)).IsError.Should().BeFalse();
            }
        }
    }
}
