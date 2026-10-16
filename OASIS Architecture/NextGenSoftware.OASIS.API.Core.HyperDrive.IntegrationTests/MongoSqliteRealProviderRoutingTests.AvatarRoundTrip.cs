using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;
using NativeAvatar=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using NativeDetail=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoAvatarIdentityRoundTripAsync(MongoDBOASIS mongo,bool synchronous,IMongoDatabase database)
    {
        var id=Guid.NewGuid();var name="mapping-"+id.ToString("N");var profileVersion=Guid.NewGuid();var detailVersion=Guid.NewGuid();
        var avatars=database.GetCollection<NativeAvatar>("Avatar");var details=database.GetCollection<NativeDetail>("AvatarDetail");
        try
        {
            var profile=new Avatar {Id=id,Username=name,Email=name+"@test.local",FirstName="preserve",VersionId=profileVersion,
                AvatarType=new EnumValue<AvatarType>(AvatarType.User),MetaData=new() {{"profile-marker","retained"},{"integer",42},{"flag",true}}};
            var detail=new AvatarDetail {Id=id,Username=name,Email=profile.Email,VersionId=detailVersion,Karma=101,XP=202,
                MetaData=new() {{"detail-marker","retained"}}};
            (await mongo.SaveAvatarAsync(profile)).IsError.Should().BeFalse();
            (await mongo.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
            (await avatars.Find(row=>row.HolonId==id).SingleAsync()).VersionId.Should().Be(profileVersion);
            (await details.Find(row=>row.HolonId==id).SingleAsync()).VersionId.Should().Be(detailVersion);
            var raw=database.GetCollection<BsonDocument>("Avatar");
            var before=await raw.Find(new BsonDocument("HolonId",id.ToString())).SingleAsync();
            for(var pass=0;pass<2;pass++)
            {
                var loaded=synchronous ? mongo.LoadAvatar(id):await mongo.LoadAvatarAsync(id);
                loaded.IsError.Should().BeFalse(loaded.Message);loaded.Result.MetaData.Should().ContainKey("profile-marker").WhoseValue.Should().Be("retained");
                loaded.Result.MetaData["integer"].ToString().Should().Be("42");loaded.Result.MetaData["flag"].Should().Be(true);
                loaded.Result.VersionId.Should().Be(profileVersion);
                var loadedDetail=synchronous ? mongo.LoadAvatarDetail(id):await mongo.LoadAvatarDetailAsync(id);
                loadedDetail.IsError.Should().BeFalse(loadedDetail.Message);loadedDetail.Result.VersionId.Should().Be(detailVersion);
                loadedDetail.Result.MetaData.Should().ContainKey("detail-marker").WhoseValue.Should().Be("retained");
                loadedDetail.Result.Karma.Should().Be(101);loadedDetail.Result.XP.Should().Be(202);
                // A normal profile/detail edit must not erase identity or unrelated metadata.
                loaded.Result.FirstName="edited";loadedDetail.Result.XP=202;
                (synchronous ? mongo.SaveAvatar(loaded.Result):await mongo.SaveAvatarAsync(loaded.Result)).IsError.Should().BeFalse();
                (synchronous ? mongo.SaveAvatarDetail(loadedDetail.Result):await mongo.SaveAvatarDetailAsync(loadedDetail.Result)).IsError.Should().BeFalse();
            }
            var after=await raw.Find(new BsonDocument("HolonId",id.ToString())).SingleAsync();
            after["MetaData"].Should().Be(before["MetaData"]);after["VersionId"].Should().Be(before["VersionId"]);
            (await details.Find(row=>row.HolonId==id).SingleAsync()).VersionId.Should().Be(detailVersion);
        }
        finally
        {
            await details.DeleteManyAsync(row=>row.HolonId==id);await avatars.DeleteManyAsync(row=>row.HolonId==id);
        }
    }
}
