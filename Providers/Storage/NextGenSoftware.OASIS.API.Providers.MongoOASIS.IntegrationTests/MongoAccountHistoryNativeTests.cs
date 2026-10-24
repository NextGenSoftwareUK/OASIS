using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Xunit.Abstractions;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using NativeAvatar=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using NativeDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoAccountHistoryNativeTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task AccountRetentionCoversSavesGameplayLifecycleAndReplicaTombstones(bool retain,bool synchronous)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name){IsVersionControlEnabled=!retain};var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);provider.IsVersionControlEnabled=retain;
            var itemId=Guid.NewGuid();
            async Task<(IAvatar Avatar,IAvatarDetail Detail)> Seed(Guid id,bool inventory=true)
            {
                var first=synchronous?provider.SaveAvatar(new Avatar {Id=id,Version=1,VersionId=Guid.NewGuid(),IsActive=true,CreatedDate=DateTime.UtcNow,Username="old-"+id.ToString("N"),Email=id+"@example.invalid",Password="synthetic-old"})
                    :await provider.SaveAvatarAsync(new Avatar {Id=id,Version=1,VersionId=Guid.NewGuid(),IsActive=true,CreatedDate=DateTime.UtcNow,Username="old-"+id.ToString("N"),Email=id+"@example.invalid",Password="synthetic-old"});
                Assert.False(first.IsError,first.Message+first.Exception);
                var profile=new AvatarDetail {Id=id,Version=5,VersionId=Guid.NewGuid(),IsActive=true,CreatedDate=DateTime.UtcNow,Username="detail-"+id.ToString("N"),Inventory=inventory?new List<IInventoryItem>{new InventoryItem{Id=itemId,Name="Seed",GameSource="Our World",Quantity=3,ItemType=InventoryItemType.QuestItem}}:new List<IInventoryItem>()};
                var second=synchronous?provider.SaveAvatarDetail(profile):await provider.SaveAvatarDetailAsync(profile);Assert.False(second.IsError,second.Message+second.Exception);
                return(first.Result,second.Result);
            }
            async Task<byte[]> Bytes(string family,Guid id)=>(await database.GetCollection<BsonDocument>(family).Find(new BsonDocument("HolonId",id.ToString("D"))).SingleAsync()).ToBson();
            var source=Guid.NewGuid();var pair=await Seed(source);
            pair.Avatar.PreviousVersionId=pair.Avatar.VersionId;pair.Avatar.Version=2;pair.Avatar.VersionId=Guid.NewGuid();pair.Avatar.Password="synthetic-new";
            var saved=synchronous?provider.SaveAvatar(pair.Avatar):await provider.SaveAvatarAsync(pair.Avatar);Assert.False(saved.IsError,saved.Message+saved.Exception);
            pair.Detail.PreviousVersionId=pair.Detail.VersionId;pair.Detail.Version=6;pair.Detail.VersionId=Guid.NewGuid();pair.Detail.XP=12;
            var detailSaved=synchronous?provider.SaveAvatarDetail(pair.Detail):await provider.SaveAvatarDetailAsync(pair.Detail);Assert.False(detailSaved.IsError,detailSaved.Message+detailSaved.Exception);
            var history=synchronous?provider.LoadAvatar(source,1):await provider.LoadAvatarAsync(source,1);Assert.False(history.IsError,history.Message+history.Exception);
            Assert.Equal(retain,history.Result!=null);if(retain)Assert.Equal("synthetic-old",history.Result.Password);
            var beforeRetry=await Bytes("Avatar",source);
            var repeated=synchronous?provider.SaveAvatar(saved.Result):await provider.SaveAvatarAsync(saved.Result);Assert.False(repeated.IsError,repeated.Message+repeated.Exception);
            Assert.Equal(beforeRetry,await Bytes("Avatar",source));
            if(retain)
            {
                saved.Result.Username="stale-same-version";
                var rejected=synchronous?provider.SaveAvatar(saved.Result):await provider.SaveAvatarAsync(saved.Result);
                Assert.True(rejected.IsError);Assert.Equal(StorageLifecycleConflictException.ErrorCode,rejected.ErrorCode);Assert.False(rejected.IsSaved);
                Assert.Equal(beforeRetry,await Bytes("Avatar",source));
            }
            var command=new HostedSyncCommandItem {OperationId=Guid.NewGuid(),AvatarId=source,EntityId=source,EntityType=HyperDriveEntityTypes.AvatarGameplay,
                PayloadJson=HyperDriveJson.Serialize(new HyperDriveAvatarGameplayCommand {Action=HyperDriveAvatarGameplayAction.UpdateInventory,InventoryItemId=itemId,
                    InventoryUpdate=new(){Id=itemId,Name="Updated seed",GameSource="Our World",Quantity=2,MaxQuantity=10,ItemType=(int)InventoryItemType.QuestItem,IsStackable=true}})};
            var played=await provider.ApplyAvatarGameplayCommandAsync(command,default);Assert.False(played.IsError,played.Message+played.Exception);
            var afterPlay=await Bytes("AvatarDetail",source);var replay=await provider.ApplyAvatarGameplayCommandAsync(command,default);Assert.False(replay.IsError,replay.Message);
            Assert.Equal(afterPlay,await Bytes("AvatarDetail",source));
            var priorPlay=synchronous?provider.LoadAvatarDetail(source,6):await provider.LoadAvatarDetailAsync(source,6);Assert.False(priorPlay.IsError,priorPlay.Message+priorPlay.Exception);Assert.Equal(retain,priorPlay.Result!=null);
            var target=Guid.NewGuid();await Seed(target,false);
            var transfer=new HostedSyncCommandItem {OperationId=Guid.NewGuid(),AvatarId=source,EntityId=source,EntityType=HyperDriveEntityTypes.AvatarGameplay,
                PayloadJson=HyperDriveJson.Serialize(new HyperDriveAvatarGameplayCommand {Action=HyperDriveAvatarGameplayAction.TransferInventory,InventoryItemId=itemId,TargetAvatarId=target,Amount=1})};
            var moved=await provider.ApplyAvatarGameplayCommandAsync(transfer,default);Assert.False(moved.IsError,moved.Message+moved.Exception);
            var movedSource=await Bytes("AvatarDetail",source);var movedTarget=await Bytes("AvatarDetail",target);
            var movedAgain=await provider.ApplyAvatarGameplayCommandAsync(transfer,default);Assert.False(movedAgain.IsError,movedAgain.Message);
            Assert.Equal(movedSource,await Bytes("AvatarDetail",source));Assert.Equal(movedTarget,await Bytes("AvatarDetail",target));
            if(retain){var previousSource=await provider.LoadAvatarDetailAsync(source,7);var previousTarget=await provider.LoadAvatarDetailAsync(target,5);Assert.False(previousSource.IsError,previousSource.Message);Assert.False(previousTarget.IsError,previousTarget.Message);Assert.Equal(2,Assert.Single(previousSource.Result.Inventory).Quantity);Assert.Empty(previousTarget.Result.Inventory);}
            var clan=new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=target,HolonType=HolonType.Clan,Version=1,VersionId=Guid.NewGuid(),MetaData=new(){
                [ClanManager.ClanStateMetadataKey]=Newtonsoft.Json.JsonConvert.SerializeObject(new ClanManager.ClanPersistentState {OwnerAvatarId=target,MemberIds=new(){target},Inventory=new()})}};
            var clanSaved=await provider.SaveHolonAsync(clan,saveChildren:false);Assert.False(clanSaved.IsError,clanSaved.Message+clanSaved.Exception);
            var clanTransfer=new HostedSyncCommandItem {OperationId=Guid.NewGuid(),AvatarId=target,EntityId=target,EntityType=HyperDriveEntityTypes.AvatarGameplay,
                PayloadJson=HyperDriveJson.Serialize(new HyperDriveAvatarGameplayCommand {Action=HyperDriveAvatarGameplayAction.TransferInventoryToClan,InventoryItemId=itemId,TargetClanId=clan.Id,DestinationInventoryItemId=Guid.NewGuid(),Amount=1})};
            var toClan=await provider.ApplyAvatarGameplayCommandAsync(clanTransfer,default);Assert.False(toClan.IsError,toClan.Message+toClan.Exception);
            var afterClan=await Bytes("AvatarDetail",target);var clanReplay=await provider.ApplyAvatarGameplayCommandAsync(clanTransfer,default);Assert.False(clanReplay.IsError,clanReplay.Message);Assert.Equal(afterClan,await Bytes("AvatarDetail",target));
            if(retain){var priorClan=await provider.LoadAvatarDetailAsync(target,6);Assert.False(priorClan.IsError,priorClan.Message);Assert.Equal(2,Assert.Single(priorClan.Result.Inventory).Quantity);}
            var deleted=synchronous?provider.DeleteAvatar(source,true):await provider.DeleteAvatarAsync(source,true);Assert.False(deleted.IsError,deleted.Message+deleted.Exception);Assert.True(deleted.IsDeleted);
            var oldAvatar=synchronous?provider.LoadAvatar(source,2):await provider.LoadAvatarAsync(source,2);Assert.False(oldAvatar.IsError,oldAvatar.Message+oldAvatar.Exception);
            if(retain){Assert.Equal(DateTime.MinValue,oldAvatar.Result.DeletedDate);var oldDetail=await provider.LoadAvatarDetailAsync(source,8);Assert.False(oldDetail.IsError,oldDetail.Message+oldDetail.Exception);Assert.Equal(DateTime.MinValue,oldDetail.Result.DeletedDate);}
            else Assert.NotEqual(DateTime.MinValue,oldAvatar.Result.DeletedDate);

            var replica=Guid.NewGuid();await Seed(replica);
            var tombstone=new AvatarTombstone(replica,DateTime.UtcNow,Guid.Empty);
            var applied=synchronous?provider.ApplyAvatarTombstone(tombstone):await provider.ApplyAvatarTombstoneAsync(tombstone);Assert.False(applied.IsError,applied.Message+applied.Exception);
            var replicaAvatar=await Bytes("Avatar",replica);var replicaDetail=await Bytes("AvatarDetail",replica);
            var again=synchronous?provider.ApplyAvatarTombstone(tombstone):await provider.ApplyAvatarTombstoneAsync(tombstone);Assert.False(again.IsError,again.Message+again.Exception);
            Assert.Equal(replicaAvatar,await Bytes("Avatar",replica));Assert.Equal(replicaDetail,await Bytes("AvatarDetail",replica));
            if(retain){var old=await provider.LoadAvatarAsync(replica,1);Assert.False(old.IsError,old.Message+old.Exception);Assert.Equal(DateTime.MinValue,old.Result.DeletedDate);}

            var isolated=Guid.NewGuid();await Seed(isolated);var untouchedAvatar=await Bytes("Avatar",isolated);
            var detailDeleted=synchronous?provider.DeleteAvatarDetail(isolated,true):await provider.DeleteAvatarDetailAsync(isolated,true);Assert.False(detailDeleted.IsError,detailDeleted.Message+detailDeleted.Exception);
            Assert.Equal(untouchedAvatar,await Bytes("Avatar",isolated));
            if(retain){var old=await provider.LoadAvatarDetailAsync(isolated,5);Assert.False(old.IsError,old.Message+old.Exception);Assert.Equal(DateTime.MinValue,old.Result.DeletedDate);}
            var hard=synchronous?provider.DeleteAvatar(isolated,false):await provider.DeleteAvatarAsync(isolated,false);Assert.False(hard.IsError,hard.Message+hard.Exception);
            foreach(var family in new[]{"Avatar","AvatarDetail"})Assert.Equal(0,await database.GetCollection<BsonDocument>(family).CountDocumentsAsync(new BsonDocument("HolonId",isolated.ToString("D"))));
            complete=true;
        }
        finally{await provider.DeActivateProviderAsync();if(complete)await client.DropDatabaseAsync(name);else output.WriteLine("Preserved owned account retention fixture for reviewed reconciliation.");}
    }

    [Theory]
    [InlineData(false,false)] [InlineData(false,true)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task HistoricalAccountSelectorsUseArchivedIdentityAndNeverLatestFallback(bool detail,bool synchronous)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;var id=Guid.NewGuid();var other=Guid.NewGuid();
        var originals=new Dictionary<string,List<BsonDocument>>();
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            async Task Seed(bool isDetail,bool extra)
            {
                var collection=isDetail?"AvatarDetail":"Avatar";var oldVersion=extra?8:isDetail?5:1;
                var publicId=extra?other:id;var prefix=(extra?"other-":"")+(isDetail?"detail":"avatar");
                BsonDocument current;
                if(isDetail)current=new NativeDetail {Id=ObjectId.GenerateNewId().ToString(),HolonId=publicId,Version=oldVersion+1,VersionId=Guid.NewGuid(),Username="new-"+prefix,Email="new-"+prefix+"@example.invalid",Karma=456,XP=4}.ToBsonDocument();
                else current=new NativeAvatar {Id=ObjectId.GenerateNewId().ToString(),HolonId=publicId,Version=oldVersion+1,VersionId=Guid.NewGuid(),Username="new-"+prefix,Email="new-"+prefix+"@example.invalid",Password="synthetic-current",VerificationToken="current-token"}.ToBsonDocument();
                var old=current.DeepClone().AsBsonDocument;old["Version"]=oldVersion;old["VersionId"]=Guid.NewGuid().ToString("D");
                old["Username"]="old-"+prefix;old["Email"]="old-"+prefix+"@example.invalid";
                if(isDetail){old["Karma"]=new BsonInt64(123);old["XP"]=3;}else{old["Password"]="synthetic-old";old["VerificationToken"]="retired-token";}
                current["PreviousVersionId"]=old["VersionId"];
                var entry=new BsonDocument{{"bson",old},{"sha256",Convert.ToHexString(SHA256.HashData(old.ToBson()))}};
                current["OasisHolonRevisionArchive"]=new BsonDocument{{"schema",2},{"records",new BsonArray{entry}}};
                await database.GetCollection<BsonDocument>(collection).InsertOneAsync(current);
                if(!originals.ContainsKey(collection))originals[collection]=new();originals[collection].Add(current.DeepClone().AsBsonDocument);
            }
            await Seed(false,false);await Seed(false,true);await Seed(true,false);await Seed(true,true);
            var version=detail?5:1;var label=detail?"detail":"avatar";var family=detail?"AvatarDetail":"Avatar";
            var primary=originals[family][0];var secondary=originals[family][1];
            IHolonBase Check<T>(OASISResult<T> result,bool found,int requested) where T:IHolonBase
            {
                Assert.False(result.IsError,result.Message+result.Exception);
                if(found){Assert.NotNull(result.Result);Assert.Equal(id,result.Result.Id);Assert.Equal(requested==0?version+1:requested,result.Result.Version);}
                else Assert.Null(result.Result);
                if(requested!=0){Assert.Equal(found,result.IsLoaded);Assert.Equal(found?1:0,result.LoadedCount);}
                return result.Result;
            }
            async Task<IHolonBase> Read(int selector,string alias=null,int? requested=null,bool found=true)
            {
                var selectedVersion=requested??version;
                if(detail)return Check(selector switch {
                    0=>synchronous?provider.LoadAvatarDetail(id,selectedVersion):await provider.LoadAvatarDetailAsync(id,selectedVersion),
                    1=>synchronous?provider.LoadAvatarDetailByUsername(alias,selectedVersion):await provider.LoadAvatarDetailByUsernameAsync(alias,selectedVersion),
                    _=>synchronous?provider.LoadAvatarDetailByEmail(alias,selectedVersion):await provider.LoadAvatarDetailByEmailAsync(alias,selectedVersion)},found,selectedVersion);
                return Check(selector switch {
                    0=>synchronous?provider.LoadAvatar(id,selectedVersion):await provider.LoadAvatarAsync(id,selectedVersion),
                    1=>synchronous?provider.LoadAvatarByUsername(alias,selectedVersion):await provider.LoadAvatarByUsernameAsync(alias,selectedVersion),
                    2=>synchronous?provider.LoadAvatarByEmail(alias,selectedVersion):await provider.LoadAvatarByEmailAsync(alias,selectedVersion),
                    _=>synchronous?provider.LoadAvatarByProviderKey(primary["_id"].ToString(),selectedVersion):await provider.LoadAvatarByProviderKeyAsync(primary["_id"].ToString(),selectedVersion)},found,selectedVersion);
            }
            var historical=await Read(0);await Read(1,"old-"+label);await Read(2,"old-"+label+"@example.invalid");
            if(detail){Assert.Equal(123,((IAvatarDetail)historical).Karma);Assert.Equal(3,((IAvatarDetail)historical).XP);}
            else{Assert.Equal("synthetic-old",((IAvatar)historical).Password);await Read(3);}
            await Read(1,"new-"+label,found:false);await Read(0,requested:999,found:false);
            await Read(1,"new-"+label,requested:0);await Read(1,"old-"+label,requested:0,found:false);
            if(detail){var all=synchronous?provider.LoadAllAvatarDetails(version):await provider.LoadAllAvatarDetailsAsync(version);Assert.False(all.IsError,all.Message+all.Exception);Assert.Equal(id,Assert.Single(all.Result).Id);Assert.Equal(1,all.LoadedCount);}
            else
            {
                var all=synchronous?provider.LoadAllAvatars(version):await provider.LoadAllAvatarsAsync(version);Assert.False(all.IsError,all.Message+all.Exception);Assert.Equal(id,Assert.Single(all.Result).Id);Assert.Equal(1,all.LoadedCount);
                var token=synchronous?provider.LoadAvatarByVerificationToken("retired-token",version):await provider.LoadAvatarByVerificationTokenAsync("retired-token",version);Assert.False(token.IsError,token.Message);Assert.Null(token.Result);
            }
            var raw=database.GetCollection<BsonDocument>(family);
            var ambiguous=secondary.DeepClone().AsBsonDocument;var archived=ambiguous["OasisHolonRevisionArchive"]["records"][0]["bson"].AsBsonDocument;
            archived["Version"]=version;archived["Username"]="old-"+label;
            ambiguous["OasisHolonRevisionArchive"]["records"][0]["sha256"]=Convert.ToHexString(SHA256.HashData(archived.ToBson()));
            Assert.Equal(1,(await raw.ReplaceOneAsync(new BsonDocument("_id",secondary["_id"]),ambiguous)).MatchedCount);
            if(detail){var bad=synchronous?provider.LoadAvatarDetailByUsername("old-"+label,version):await provider.LoadAvatarDetailByUsernameAsync("old-"+label,version);Assert.True(bad.IsError);Assert.Equal(OASISResultErrorCodes.AmbiguousIdentity,bad.ErrorCode);Assert.Null(bad.Result);}
            else{var bad=synchronous?provider.LoadAvatarByUsername("old-"+label,version):await provider.LoadAvatarByUsernameAsync("old-"+label,version);Assert.True(bad.IsError);Assert.Equal(OASISResultErrorCodes.AmbiguousIdentity,bad.ErrorCode);Assert.Null(bad.Result);}
            Assert.Equal(1,(await raw.ReplaceOneAsync(new BsonDocument("_id",secondary["_id"]),secondary)).MatchedCount);
            var corrupt=primary.DeepClone().AsBsonDocument;corrupt["OasisHolonRevisionArchive"]["records"][0]["sha256"]=new string('0',64);
            Assert.Equal(1,(await raw.ReplaceOneAsync(new BsonDocument("_id",primary["_id"]),corrupt)).MatchedCount);
            if(detail){var bad=synchronous?provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version);Assert.True(bad.IsError);Assert.Null(bad.Result);Assert.Equal(StorageLifecycleConflictException.ErrorCode,bad.ErrorCode);}
            else{var bad=synchronous?provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version);Assert.True(bad.IsError);Assert.Null(bad.Result);Assert.Equal(StorageLifecycleConflictException.ErrorCode,bad.ErrorCode);}
            corrupt=primary.DeepClone().AsBsonDocument;var wrongIdentity=corrupt["OasisHolonRevisionArchive"]["records"][0]["bson"].AsBsonDocument;
            wrongIdentity["_id"]=ObjectId.GenerateNewId();corrupt["OasisHolonRevisionArchive"]["records"][0]["sha256"]=Convert.ToHexString(SHA256.HashData(wrongIdentity.ToBson()));
            Assert.Equal(1,(await raw.ReplaceOneAsync(new BsonDocument("_id",primary["_id"]),corrupt)).MatchedCount);
            if(detail){var bad=synchronous?provider.LoadAvatarDetail(id,version):await provider.LoadAvatarDetailAsync(id,version);Assert.True(bad.IsError);Assert.Null(bad.Result);Assert.Equal(StorageLifecycleConflictException.ErrorCode,bad.ErrorCode);}
            else{var bad=synchronous?provider.LoadAvatar(id,version):await provider.LoadAvatarAsync(id,version);Assert.True(bad.IsError);Assert.Null(bad.Result);Assert.Equal(StorageLifecycleConflictException.ErrorCode,bad.ErrorCode);}
            Assert.Equal(1,(await raw.ReplaceOneAsync(new BsonDocument("_id",primary["_id"]),primary)).MatchedCount);
            foreach(var pair in originals)foreach(var row in pair.Value)
                Assert.Equal(row.ToBson(),(await database.GetCollection<BsonDocument>(pair.Key).Find(new BsonDocument("_id",row["_id"])).SingleAsync()).ToBson());
            complete=true;
        }
        finally{await provider.DeActivateProviderAsync();if(complete)await client.DropDatabaseAsync(name);else output.WriteLine("Preserved owned account-history selector fixture for reviewed reconciliation.");}
    }
}
