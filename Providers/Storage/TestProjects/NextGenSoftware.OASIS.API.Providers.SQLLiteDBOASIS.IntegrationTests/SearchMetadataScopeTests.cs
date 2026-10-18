using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MetadataFiltersApplyToBothAvatarAndHolonCandidates(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var text="owned-search-metadata-"+Guid.NewGuid().ToString("N");
        var holon=new Holon {Id=Guid.NewGuid(),Name=text,MetaData=new() {{"biome","park"}}};
        var avatar=new Avatar {Id=Guid.NewGuid(),Username=text,MetaData=new() {{"biome","park"}},
            AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)};
        (await provider.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        foreach(var mode in new[] {MetaKeyValuePairMatchMode.All,MetaKeyValuePairMatchMode.Any})
        foreach(var kind in new[] {"match","miss","mixed","empty"})
        {
            var filter=new Dictionary<string,string>();
            if(kind is "match" or "mixed") filter["biome"]="park";
            if(kind is "miss" or "mixed") filter["missing"]="never";
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,FilterByMetaData=filter,MetaKeyValuePairMatchMode=mode,SearchGroups=new() {
                new SearchTextGroup {SearchHolons=true,SearchAvatars=true,HolonType=HolonType.All,SearchQuery=text,
                    HolonSearchParams=new SearchHolonParams {Name=true},AvatarSearchParams=new Core.Objects.Search.Avatrar.SearchAvatarParams {Username=true}}}};
            var result=synchronous ? provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
            var matches=kind is "match" or "empty" || kind=="mixed" && mode==MetaKeyValuePairMatchMode.Any;
            result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(matches ? new[] {holon.Id}:Array.Empty<Guid>(),"metadata {0}/{1}",mode,kind);
            result.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(matches ? new[] {avatar.Id}:Array.Empty<Guid>(),"metadata {0}/{1}",mode,kind);
        }
    }
}
