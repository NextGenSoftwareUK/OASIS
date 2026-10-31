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
using NextGenSoftware.Utilities;
using NativeAvatar=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using NativeDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoAvatarCommitAcknowledgementsAsync(MongoDBOASIS fault,MongoDBOASIS healthy,SQLLiteDBOASIS sqlite,
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
            var id=Guid.NewGuid();var name="avatar-commit-"+id.ToString("N");
            Avatar Owner() => new() {Id=id,Username=name,Email=name+"@test.local",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
            var detailOnly=variant.StartsWith("detail",StringComparison.Ordinal);var soft=!variant.EndsWith("Hard",StringComparison.Ordinal);
            try
            {
                (await healthy.SaveAvatarAsync(Owner())).IsError.Should().BeFalse();
                (await healthy.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=name,Email=name+"@test.local",Karma=11,XP=13})).IsError.Should().BeFalse();
                (await sqlite.SaveAvatarAsync(Owner())).IsError.Should().BeFalse();
                (await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=name,Email=name+"@test.local",Karma=17,XP=19})).IsError.Should().BeFalse();
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {"commitTransaction"}},{"appName",app},
                        {"writeConcernError",new BsonDocument {{"code",64},{"errmsg","owned avatar post-commit acknowledgement fault"}}}}}});
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                var tombstone=new AvatarTombstone(id,DateTime.UtcNow,Guid.NewGuid());
                var request=new StorageOperationRequest {Operation=detailOnly ? "DeleteAvatarDetail":"DeleteAvatar",AvatarId=id,SoftDelete=soft,PreferredProvider=ProviderType.MongoDBOASIS};
                var result=variant=="tombstone"
                    ? (synchronous ? fault.ApplyAvatarTombstone(tombstone):await fault.ApplyAvatarTombstoneAsync(tombstone))
                    : (synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request));
                result.IsError.Should().BeTrue();result.Result.Should().BeFalse();result.IsDeleted.Should().BeFalse();
                var evidence=result.Exception.Should().BeOfType<MongoTransactionReconciliationException>().Which;
                BsonDocument.Parse(evidence.SessionId).Contains("id").Should().BeTrue();evidence.TransactionNumber.Should().BeGreaterThanOrEqualTo(0);
                evidence.InnerException.Should().BeOfType<MongoWriteConcernException>().Which.WriteConcernResult.Response["writeConcernError"]["code"].AsInt32.Should().Be(64);
                NativeMutationReconciliation.IsRequired(evidence).Should().BeTrue();
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                var nativeAvatar=await avatars.Find(row=>row.HolonId==id).SingleOrDefaultAsync();var nativeDetail=await details.Find(row=>row.HolonId==id).SingleOrDefaultAsync();
                if(!soft)
                {
                    nativeDetail.Should().BeNull();
                    if(detailOnly) nativeAvatar.Should().NotBeNull();else nativeAvatar.Should().BeNull();
                }
                else
                {
                    nativeDetail.DeletedDate.Should().NotBe(DateTime.MinValue);nativeDetail.IsActive.Should().BeFalse();nativeDetail.Karma.Should().Be(11);nativeDetail.XP.Should().Be(13);
                    if(detailOnly) nativeAvatar.DeletedDate.Should().Be(DateTime.MinValue);else nativeAvatar.DeletedDate.Should().Be(nativeDetail.DeletedDate);
                    if(variant=="tombstone") nativeDetail.DeletedDate.Should().Be(tombstone.DeletedAtUtc);
                }
                (await sqlite.LoadAvatarAsync(id)).Result.Should().NotBeNull();
                var retained=(await sqlite.LoadAvatarDetailAsync(id)).Result;retained.Karma.Should().Be(17);retained.XP.Should().Be(19);retained.DeletedDate.Should().Be(DateTime.MinValue);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                await details.DeleteManyAsync(row=>row.HolonId==id);await avatars.DeleteManyAsync(row=>row.HolonId==id);
                if((await sqlite.LoadAvatarAsync(id)).Result!=null) (await sqlite.DeleteAvatarAsync(id,softDelete:false)).IsError.Should().BeFalse();
            }
        }
    }
}
