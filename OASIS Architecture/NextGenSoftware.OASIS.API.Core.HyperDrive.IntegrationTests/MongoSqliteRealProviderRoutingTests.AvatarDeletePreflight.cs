using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoAvatarDeletionPreflightAsync(MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,OASISDNA dna,bool synchronous,IMongoDatabase database)
    {
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(mongo);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        await VerifyMongoInvalidAvatarDeleteKeyAsync(mongo,sqlite,manager,router,synchronous);
        var avatars=database.GetCollection<BsonDocument>("Avatar");var details=database.GetCollection<BsonDocument>("AvatarDetail");
        foreach(var state in new[] {"missingAvatar","missingDetail","deletedAvatar","deletedDetail","pairMissingDetail","pairDeletedDetail"})
        {
            var id=Guid.NewGuid();var name="delete-preflight-"+id.ToString("N");
            var avatar=new Avatar {Id=id,Username=name,Email=name+"@test.local",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
            var detail=new AvatarDetail {Id=id,Username=name,Email=avatar.Email,Karma=23,XP=47};
            BsonDocument? avatarFilter=null,detailFilter=null;
            try
            {
                var savedAvatar=await mongo.SaveAvatarAsync(avatar);savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
                var savedDetail=await mongo.SaveAvatarDetailAsync(detail);savedDetail.IsError.Should().BeFalse(savedDetail.Message);
                var key=savedAvatar.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
                avatarFilter=new BsonDocument("_id",ObjectId.Parse(key));
                detailFilter=new BsonDocument("_id",ObjectId.Parse(savedDetail.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]));
                (await sqlite.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
                (await sqlite.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
                if(state=="missingAvatar") await avatars.DeleteOneAsync(avatarFilter);
                if(state is "missingDetail" or "pairMissingDetail") await details.DeleteOneAsync(detailFilter);
                if(state=="deletedAvatar") await avatars.UpdateOneAsync(avatarFilter,Builders<BsonDocument>.Update.Set("DeletedDate",DateTime.UtcNow).Set("IsActive",false));
                if(state is "deletedDetail" or "pairDeletedDetail") await details.UpdateOneAsync(detailFilter,Builders<BsonDocument>.Update.Set("DeletedDate",DateTime.UtcNow).Set("IsActive",false));
                var profileBefore=(await avatars.Find(avatarFilter).FirstOrDefaultAsync())?.ToBson();
                var detailBefore=(await details.Find(detailFilter).FirstOrDefaultAsync())?.ToBson();
                var operations=state is "missingDetail" or "deletedDetail"
                    ? new[] {"DeleteAvatarDetail","DeleteAvatarDetailByUsername","DeleteAvatarDetailByEmail"}
                    :new[] {"DeleteAvatar","DeleteAvatarByUsername","DeleteAvatarByEmail","DeleteAvatarByProviderKey"};
                foreach(var operation in operations)
                foreach(var soft in state.StartsWith("missing",StringComparison.Ordinal) ? new[] {false,true}:new[] {true})
                {
                    var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                    var request=new StorageOperationRequest {Operation=operation,AvatarId=id,Username=name,Email=avatar.Email,ProviderKey=key,SoftDelete=soft,PreferredProvider=ProviderType.MongoDBOASIS};
                    var result=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
                    result.IsError.Should().BeTrue();result.Result.Should().BeFalse();result.IsDeleted.Should().BeFalse();
                    if(state.StartsWith("missing",StringComparison.Ordinal)) {result.ErrorCode.Should().Be(OASISResultErrorCodes.RecordNotFound);result.Exception.Should().BeNull();}
                    else result.ErrorCode.Should().Be(Core.Helpers.StorageLifecycleConflictException.ErrorCode);
                    (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before,"known absence/conflict must never select a stale provider");
                    (await sqlite.LoadAvatarAsync(id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
                    var retained=(await sqlite.LoadAvatarDetailAsync(id)).Result;retained.DeletedDate.Should().Be(DateTime.MinValue);retained.Karma.Should().Be(23);retained.XP.Should().Be(47);
                    var profileAfter=(await avatars.Find(avatarFilter).FirstOrDefaultAsync())?.ToBson();
                    var detailAfter=(await details.Find(detailFilter).FirstOrDefaultAsync())?.ToBson();
                    if(profileBefore==null) profileAfter.Should().BeNull(); else profileAfter.Should().Equal(profileBefore);
                    if(detailBefore==null) detailAfter.Should().BeNull(); else detailAfter.Should().Equal(detailBefore);
                }
            }
            finally
            {
                if(avatarFilter!=null) await avatars.DeleteOneAsync(avatarFilter);
                if(detailFilter!=null) await details.DeleteOneAsync(detailFilter);
                if((await sqlite.LoadAvatarAsync(id)).Result!=null) (await sqlite.DeleteAvatarAsync(id,softDelete:false)).IsError.Should().BeFalse();
            }
        }
        await VerifyMongoSoftDeleteFieldRetentionAsync(mongo,synchronous,database);
    }

    private static async Task VerifyMongoInvalidAvatarDeleteKeyAsync(MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,ProviderManager manager,OASISHyperDrive router,bool synchronous)
    {
        var id=Guid.NewGuid();var name="invalid-native-key-"+id.ToString("N");
        var avatar=new Avatar {Id=id,Username=name,Email=name+"@test.local",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
        try
        {
            var saved=await sqlite.SaveAvatarAsync(avatar);saved.IsError.Should().BeFalse(saved.Message);
            (await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=name,Email=avatar.Email,Karma=37,XP=29})).IsError.Should().BeFalse();
            var key=saved.Result.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS];
            ObjectId.TryParse(key,out _).Should().BeFalse("the SQLite key is deliberately invalid for Mongo");
            (await sqlite.LoadAvatarByProviderKeyAsync(key)).Result.Id.Should().Be(id,"prove this key could target the stale SQLite copy");
            foreach(var invalid in new string?[] {key,"not-an-object-id"," ",string.Empty,null})
            foreach(var soft in new[] {false,true})
            {
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                var request=new StorageOperationRequest {Operation="DeleteAvatarByProviderKey",ProviderKey=invalid,SoftDelete=soft,PreferredProvider=ProviderType.MongoDBOASIS};
                var result=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
                result.IsError.Should().BeTrue();result.Result.Should().BeFalse();result.IsDeleted.Should().BeFalse();
                result.ErrorCode.Should().Be(string.IsNullOrWhiteSpace(invalid) ? "HYPERDRIVE_PROVIDER_KEY_REQUIRED":"HYPERDRIVE_PAYLOAD_INVALID");
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                var direct=synchronous ? mongo.DeleteAvatar(invalid,soft):await mongo.DeleteAvatarAsync(invalid,soft);
                direct.IsError.Should().BeTrue();direct.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");direct.Exception.Should().BeOfType<ArgumentException>();
                (await sqlite.LoadAvatarAsync(id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
                var detail=(await sqlite.LoadAvatarDetailAsync(id)).Result;detail.DeletedDate.Should().Be(DateTime.MinValue);detail.Karma.Should().Be(37);detail.XP.Should().Be(29);
            }
        }
        finally
        {
            if((await sqlite.LoadAvatarAsync(id)).Result!=null) (await sqlite.DeleteAvatarAsync(id,softDelete:false)).IsError.Should().BeFalse();
        }
    }

    private static async Task VerifyMongoSoftDeleteFieldRetentionAsync(MongoDBOASIS mongo,bool synchronous,IMongoDatabase database)
    {
        var avatars=database.GetCollection<BsonDocument>("Avatar");var details=database.GetCollection<BsonDocument>("AvatarDetail");
        foreach(var detailOnly in new[] {false,true})
        {
            var id=Guid.NewGuid();var name="soft-delete-fields-"+id.ToString("N");
            var avatar=new Avatar {Id=id,Username=name,Email=name+"@test.local",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
            var detail=new AvatarDetail {Id=id,Username=name,Email=avatar.Email,Karma=67,XP=89};
            BsonDocument? avatarFilter=null,detailFilter=null;
            try
            {
                var savedAvatar=await mongo.SaveAvatarAsync(avatar);savedAvatar.IsError.Should().BeFalse(savedAvatar.Message);
                var savedDetail=await mongo.SaveAvatarDetailAsync(detail);savedDetail.IsError.Should().BeFalse(savedDetail.Message);
                avatarFilter=new BsonDocument("_id",ObjectId.Parse(savedAvatar.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]));
                detailFilter=new BsonDocument("_id",ObjectId.Parse(savedDetail.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]));
                var unknown=new BsonDocument {{"opaque",new BsonDocument {{"$ne",false}}}};
                await avatars.UpdateOneAsync(avatarFilter,Builders<BsonDocument>.Update.Set("future_schema",unknown));
                await details.UpdateOneAsync(detailFilter,Builders<BsonDocument>.Update.Set("future_schema",unknown));
                var profileBefore=await avatars.Find(avatarFilter).SingleAsync();var detailBefore=await details.Find(detailFilter).SingleAsync();
                var result=detailOnly
                    ? (synchronous ? mongo.DeleteAvatarDetail(id,true):await mongo.DeleteAvatarDetailAsync(id,true))
                    : (synchronous ? mongo.DeleteAvatar(id,true):await mongo.DeleteAvatarAsync(id,true));
                result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);result.IsDeleted.Should().BeTrue();result.Result.Should().BeTrue();
                var profileAfter=await avatars.Find(avatarFilter).SingleAsync();var detailAfter=await details.Find(detailFilter).SingleAsync();
                AssertOnlyMongoDeletionFieldsChanged(detailBefore,detailAfter);
                detailAfter["IsActive"].AsBoolean.Should().BeFalse();detailAfter["DeletedDate"].ToUniversalTime().Should().NotBe(DateTime.MinValue);
                if(detailOnly) profileAfter.ToBson().Should().Equal(profileBefore.ToBson());
                else
                {
                    AssertOnlyMongoDeletionFieldsChanged(profileBefore,profileAfter);
                    profileAfter["DeletedDate"].Should().Be(detailAfter["DeletedDate"]);
                    profileAfter["DeletedDateExactUtcTicks"].Should().Be(detailAfter["DeletedDateExactUtcTicks"]);
                }
            }
            finally
            {
                if(avatarFilter!=null) await avatars.DeleteOneAsync(avatarFilter);
                if(detailFilter!=null) await details.DeleteOneAsync(detailFilter);
            }
        }
    }

    private static void AssertOnlyMongoDeletionFieldsChanged(BsonDocument before,BsonDocument after)
    {
        var lifecycle=new HashSet<string>(StringComparer.Ordinal){"IsActive","DeletedDate","DeletedDateExactUtcTicks","DeletedByAvatarId"};
        foreach(var field in before.Where(field=>!lifecycle.Contains(field.Name)))
        {
            after.Contains(field.Name).Should().BeTrue();after[field.Name].Should().Be(field.Value);
        }
        after.Names.Where(field=>!lifecycle.Contains(field)).Should().BeEquivalentTo(before.Names.Where(field=>!lifecycle.Contains(field)));
    }
}
