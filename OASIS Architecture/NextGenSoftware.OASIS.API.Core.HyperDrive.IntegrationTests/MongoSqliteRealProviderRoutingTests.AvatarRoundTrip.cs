using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
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
            var profile=new Avatar {Id=id,Username=name,Email=name+"@test.local",FirstName="preserve",VersionId=profileVersion,IsPublic=true,
                CustomHolonType="owned-profile-type",AvatarType=new EnumValue<AvatarType>(AvatarType.User),MetaData=new() {{"profile-marker","retained"},{"integer",42},{"flag",true}}};
            var detail=new AvatarDetail {Id=id,Username=name,Email=profile.Email,VersionId=detailVersion,Karma=101,XP=202,IsPublic=true,
                CustomHolonType="owned-detail-type",MetaData=new() {{"detail-marker","retained"}},Model3D="model://owned-fixture",DimensionLevel=DimensionLevel.Fifth,SubDimensionLevel=SubDimensionLevel.Seventh,
                Attributes=new AvatarAttributes {Strength=11,Speed=12,Dexterity=13,Toughness=14,Wisdom=15,Intelligence=16,Magic=17,Vitality=18,Endurance=19},
                DimensionLevelIds=new Dictionary<DimensionLevel,Guid> {{DimensionLevel.Third,Guid.NewGuid()},{DimensionLevel.Fifth,Guid.NewGuid()}},
                HeartRateData=new List<NextGenSoftware.OASIS.API.Core.Interfaces.Avatar.IHeartRateEntry> {
                    new HeartRateEntry {HeartRateValue=72,TimeStamp=new DateTime(2026,10,15,10,0,0,DateTimeKind.Utc)},
                    new HeartRateEntry {HeartRateValue=84,TimeStamp=new DateTime(2026,10,15,10,1,0,DateTimeKind.Utc)}}};
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
                loaded.Result.IsPublic.Should().Be(pass==0);
                loaded.Result.CustomHolonType.Should().Be(profile.CustomHolonType);
                var loadedDetail=synchronous ? mongo.LoadAvatarDetail(id):await mongo.LoadAvatarDetailAsync(id);
                loadedDetail.IsError.Should().BeFalse(loadedDetail.Message);loadedDetail.Result.VersionId.Should().Be(detailVersion);
                loadedDetail.Result.MetaData.Should().ContainKey("detail-marker").WhoseValue.Should().Be("retained");
                loadedDetail.Result.Karma.Should().Be(101);loadedDetail.Result.XP.Should().Be(202);
                loadedDetail.Result.IsPublic.Should().Be(pass==0);
                loadedDetail.Result.CustomHolonType.Should().Be(detail.CustomHolonType);
                loadedDetail.Result.Model3D.Should().Be(detail.Model3D);
                loadedDetail.Result.DimensionLevel.Should().Be(detail.DimensionLevel);loadedDetail.Result.SubDimensionLevel.Should().Be(detail.SubDimensionLevel);
                loadedDetail.Result.Attributes.Should().BeEquivalentTo(detail.Attributes);
                loadedDetail.Result.DimensionLevelIds.Should().BeEquivalentTo(detail.DimensionLevelIds);
                loadedDetail.Result.HeartRateData.Should().BeEquivalentTo(detail.HeartRateData,options=>options.WithStrictOrdering());
                // A normal profile/detail edit must not erase identity or unrelated metadata.
                loaded.Result.FirstName="edited";loadedDetail.Result.XP=202;loaded.Result.IsPublic=false;loadedDetail.Result.IsPublic=false;
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
