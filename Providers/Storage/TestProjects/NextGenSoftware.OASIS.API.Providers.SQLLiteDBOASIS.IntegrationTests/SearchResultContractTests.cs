using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task SuccessfulGroupedSearchSetsLoadedFlagsAndCounts(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row=new Holon{Id=Guid.NewGuid(),Name="owned-search-result-flags"};
        (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchHolons=true,HolonType=HolonType.All,SearchQuery=row.Name}}};
        var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        result.IsError.Should().BeFalse(result.Message);result.Result.SearchResultHolons.Should().ContainSingle().Which.Id.Should().Be(row.Id);
        result.IsLoaded.Should().BeTrue();result.LoadedCount.Should().Be(1);result.Result.NumberOfResults.Should().Be(1);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task MetadataOnlySearchPublishesOwnedCandidatesAndAccurateCounts(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();var owner=Guid.NewGuid();var foreign=Guid.NewGuid();
        var ownHolon=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,HolonType=HolonType.Park,Version=7,Name="owned no-group root",MetaData=new(){{"biome","park"}}};
        var otherHolon=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=foreign,HolonType=HolonType.Park,Version=8,Name="foreign no-group root",MetaData=new(){{"biome","park"}}};
        var ownAvatar=new Avatar{Id=Guid.NewGuid(),CreatedByAvatarId=owner,Username="owned-no-group",Version=7,MetaData=new(){{"biome","park"}},AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)};
        var otherAvatar=new Avatar{Id=Guid.NewGuid(),CreatedByAvatarId=foreign,Username="foreign-no-group",Version=8,MetaData=new(){{"biome","park"}},AvatarType=ownAvatar.AvatarType};
        foreach(var row in new[]{ownHolon,otherHolon})(await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
        foreach(var row in new[]{ownAvatar,otherAvatar})(await provider.SaveAvatarAsync(row)).IsError.Should().BeFalse();
        foreach(var kind in new[]{"null","empty","number"})foreach(var scoped in new[]{false,true})foreach(var match in new[]{true,false})
        {
            var query=new SearchParams{SearchOnlyForCurrentAvatar=scoped,AvatarId=owner,FilterByMetaData=new(){{"biome",match?"park":"missing"}},SearchGroups=new()};
            if(kind=="null")query.SearchGroups=null!;
            if(kind=="number")query.SearchGroups.Add(new SearchNumberGroup{HolonType=HolonType.All,Number=7,NumberOperator=SearchOperatorType.EqualTo});
            var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            result.IsError.Should().BeFalse(result.Exception?.ToString()??result.Message);
            var expectedHolons=!match?Array.Empty<Guid>():scoped||kind=="number"?new[]{ownHolon.Id}:new[]{ownHolon.Id,otherHolon.Id};
            var expectedAvatars=!match?Array.Empty<Guid>():scoped||kind=="number"?new[]{ownAvatar.Id}:new[]{ownAvatar.Id,otherAvatar.Id};
            result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(expectedHolons);
            result.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(expectedAvatars);
            result.IsLoaded.Should().BeTrue();result.LoadedCount.Should().Be(expectedHolons.Length+expectedAvatars.Length);
            result.Result.NumberOfResults.Should().Be(result.LoadedCount);
        }
    }
}
