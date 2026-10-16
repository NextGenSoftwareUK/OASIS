using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;
using AvatarMask=NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoDefaultTextMasksAsync(MongoDBOASIS mongo,bool synchronous)
    {
        var suffix=Guid.NewGuid().ToString("N");var text="owned.[mask]+(literal)-"+suffix;
        var name=new Holon {Id=Guid.NewGuid(),HolonType=HolonType.Park,Name=text};
        var description=new Holon {Id=Guid.NewGuid(),HolonType=HolonType.Park,Name="description target",Description=text};
        var decoy=new Holon {Id=Guid.NewGuid(),HolonType=HolonType.Park,Name="ownedXmmliteral-"+suffix};
        var avatars=new[] {"FirstName","LastName","Username","Email"}.Select(field=>new Avatar {
            Id=Guid.NewGuid(),Username=field=="Username" ? text:"owned-mask-"+field+suffix,
            Email=field=="Email" ? text+"@example.invalid":field+suffix+"@example.invalid",
            FirstName=field=="FirstName" ? text:"unmatched",LastName=field=="LastName" ? text:"unmatched",
            AvatarType=new EnumValue<AvatarType>(AvatarType.User)}).ToList();
        var holonIds=new[] {name.Id,description.Id,decoy.Id};var avatarIds=avatars.Select(row=>row.Id).ToArray();
        try
        {
            foreach(var row in new[] {name,description,decoy}) (await mongo.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
            foreach(var row in avatars) (await mongo.SaveAvatarAsync(row)).IsError.Should().BeFalse();
            foreach(var masks in new[] {"omitted","empty","selected"})
            foreach(var type in new[] {HolonType.All,HolonType.Park,HolonType.Quest})
            {
                var group=new SearchTextGroup {SearchHolons=true,SearchAvatars=true,SearchQuery=text.ToUpperInvariant(),HolonType=type};
                if(masks!="omitted")
                {
                    group.HolonSearchParams=new SearchHolonParams {Name=masks=="selected"};
                    group.AvatarSearchParams=new AvatarMask {Username=masks=="selected"};
                }
                var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {group}};
                var found=synchronous ? mongo.Search(query,loadChildren:false):await mongo.SearchAsync(query,loadChildren:false);
                found.IsError.Should().BeFalse(found.Exception?.ToString() ?? found.Message);found.IsLoaded.Should().BeTrue();
                var expectedHolons=masks=="empty" || type==HolonType.Quest ? Array.Empty<Guid>()
                    :masks=="selected" ? new[] {name.Id}:new[] {name.Id,description.Id};
                var expectedAvatars=type!=HolonType.All || masks=="empty" ? Array.Empty<Guid>()
                    :masks=="selected" ? new[] {avatars.Single(row=>row.Username==text).Id}:avatarIds;
                found.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(expectedHolons);
                found.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(expectedAvatars);
                found.Result.NumberOfResults.Should().Be(expectedHolons.Length+expectedAvatars.Length);
            }
        }
        finally
        {
            await mongo.Database.Holon.DeleteManyAsync(row=>holonIds.Contains(row.HolonId));
            await mongo.Database.Avatar.DeleteManyAsync(row=>avatarIds.Contains(row.HolonId));
        }
    }
}
