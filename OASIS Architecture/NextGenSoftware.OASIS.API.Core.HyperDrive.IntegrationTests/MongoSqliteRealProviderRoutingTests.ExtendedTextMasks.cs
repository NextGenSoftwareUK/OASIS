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
    private static async Task VerifyMongoExtendedTextMasksAsync(MongoDBOASIS mongo,bool synchronous)
    {
        var text="owned-extended-"+Guid.NewGuid().ToString("N");var secret="owned-secret-"+Guid.NewGuid().ToString("N");
        var holon=new Holon {Id=Guid.NewGuid(),Name="plain holon",MetaData=new() {{"selected",text},{"other",text+"-other"}}};
        var avatar=new Avatar {Id=Guid.NewGuid(),Username=Guid.NewGuid().ToString("N"),Title=text,VerificationToken=secret,
            AvatarType=new EnumValue<AvatarType>(AvatarType.User),MetaData=new() {{"selected",text}}};
        try
        {
            (await mongo.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
            (await mongo.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            var cases=new[] {
                (Query:text,Holon:new SearchHolonParams(),Avatar:new AvatarMask {Title=true},Holons:Array.Empty<Guid>(),Avatars:new[] {avatar.Id}),
                (Query:avatar.Id.ToString(),Holon:new SearchHolonParams(),Avatar:new AvatarMask {AvatarId=true},Holons:Array.Empty<Guid>(),Avatars:new[] {avatar.Id}),
                (Query:text,Holon:new SearchHolonParams {MetaData=true,MetaDataKey="selected"},Avatar:new AvatarMask {MetaData=true},Holons:new[] {holon.Id},Avatars:new[] {avatar.Id}),
                (Query:text+"-other",Holon:new SearchHolonParams {MetaData=true,MetaDataKey="selected"},Avatar:new AvatarMask(),Holons:Array.Empty<Guid>(),Avatars:Array.Empty<Guid>()),
                (Query:text,Holon:new SearchHolonParams {SearchAllFields=true},Avatar:new AvatarMask {SearchAllFields=true},Holons:new[] {holon.Id},Avatars:new[] {avatar.Id}),
                (Query:secret,Holon:new SearchHolonParams {SearchAllFields=true},Avatar:new AvatarMask {SearchAllFields=true},Holons:Array.Empty<Guid>(),Avatars:Array.Empty<Guid>())};
            foreach(var item in cases)
            {
                var group=new SearchTextGroup {SearchHolons=true,SearchAvatars=true,HolonType=HolonType.All,SearchQuery=item.Query,
                    HolonSearchParams=item.Holon,AvatarSearchParams=item.Avatar};
                var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {group}};
                var result=synchronous ? mongo.Search(query,loadChildren:false):await mongo.SearchAsync(query,loadChildren:false);
                result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
                result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(item.Holons);
                result.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(item.Avatars);
            }
        }
        finally
        {
            await mongo.Database.Holon.DeleteManyAsync(row=>row.HolonId==holon.Id);
            await mongo.Database.Avatar.DeleteManyAsync(row=>row.HolonId==avatar.Id);
        }
    }
}
