using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;
using AvatarMask=NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;
using NativeAvatar=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoNumericDateDefaultsAsync(MongoDBOASIS mongo,bool synchronous)
    {
        var created=new DateTime(2020,1,2,0,0,0,DateTimeKind.Utc);var modified=created.AddYears(1);
        var park=new Holon {Id=Guid.NewGuid(),Version=7,Name="owned numeric park",HolonType=HolonType.Park,CreatedDate=created,ModifiedDate=modified};
        var quest=new Holon {Id=Guid.NewGuid(),Version=7,Name="owned numeric quest",HolonType=HolonType.Quest,CreatedDate=created,ModifiedDate=modified};
        var avatar=new Avatar {Id=Guid.NewGuid(),Version=7,Username="owned-numeric-"+Guid.NewGuid().ToString("N"),
            HolonType=HolonType.Avatar,CreatedDate=created,ModifiedDate=modified,AvatarType=new EnumValue<AvatarType>(AvatarType.User)};
        var ids=new[] {park.Id,quest.Id};
        try
        {
            foreach(var row in new[] {park,quest}) (await mongo.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
            (await mongo.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            // Normal saves stamp modification audit time. Seed the synthetic date-query fixture explicitly.
            (await mongo.Database.Holon.UpdateManyAsync(row=>ids.Contains(row.HolonId),
                Builders<NativeHolon>.Update.Set(row=>row.ModifiedDate,modified))).MatchedCount.Should().Be(2);
            (await mongo.Database.Avatar.UpdateOneAsync(row=>row.HolonId==avatar.Id,
                Builders<NativeAvatar>.Update.Set(row=>row.ModifiedDate,modified))).MatchedCount.Should().Be(1);
            foreach(var kind in new[] {"number","created","modified"})
            foreach(var mask in new[] {"omitted","empty","selected"})
            foreach(var type in new[] {HolonType.All,HolonType.Park,HolonType.Quest,HolonType.Avatar})
            {
                ISearchGroupBase group=kind=="number"
                    ? new SearchNumberGroup {Number=7,NumberOperator=SearchOperatorType.EqualTo}
                    :new SearchDateGroup {Date=kind=="created" ? created:modified,DateOperator=SearchOperatorType.EqualTo};
                group.SearchHolons=true;group.SearchAvatars=true;group.HolonType=type;
                if(mask!="omitted")
                {
                    group.HolonSearchParams=new SearchHolonParams {Version=mask=="selected",ModifiedDate=mask=="selected"};
                    group.AvatarSearchParams=new AvatarMask {Version=mask=="selected",ModifiedDate=mask=="selected"};
                }
                var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {group}};
                var result=synchronous ? mongo.Search(query,loadChildren:false):await mongo.SearchAsync(query,loadChildren:false);
                result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);result.IsLoaded.Should().BeTrue();
                var matches=mask!="empty" && (mask!="selected" || kind!="created");
                var expectedHolons=!matches ? Array.Empty<Guid>():new[] {park,quest}.Where(row=>type==HolonType.All || row.HolonType==type).Select(row=>row.Id).ToArray();
                var expectedAvatars=matches && (type==HolonType.All || type==HolonType.Avatar) ? new[] {avatar.Id}:Array.Empty<Guid>();
                result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(expectedHolons,"kind={0}, mask={1}, type={2}",kind,mask,type);
                result.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(expectedAvatars,"kind={0}, mask={1}, type={2}",kind,mask,type);
                result.Result.NumberOfResults.Should().Be(expectedHolons.Length+expectedAvatars.Length);
            }
            var empty=synchronous ? mongo.Search(new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=null!})
                :await mongo.SearchAsync(new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=null!});
            empty.IsError.Should().BeFalse(empty.Exception?.ToString() ?? empty.Message);empty.Result.NumberOfResults.Should().Be(0);
        }
        finally
        {
            await mongo.Database.Holon.DeleteManyAsync(row=>ids.Contains(row.HolonId));
            await mongo.Database.Avatar.DeleteManyAsync(row=>row.HolonId==avatar.Id);
        }
    }
}
