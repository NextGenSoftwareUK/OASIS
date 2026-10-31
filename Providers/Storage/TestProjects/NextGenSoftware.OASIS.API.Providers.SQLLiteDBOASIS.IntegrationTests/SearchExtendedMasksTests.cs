using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Xunit;
using AvatarMask=NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ExtendedMasksSearchStoredValuesWithoutCredentialMatches(bool synchronous)
    {
        var text="owned-nested-search-"+Guid.NewGuid().ToString("N");var secret="owned-secret-"+Guid.NewGuid().ToString("N");
        var created=new DateTime(2001,2,3,0,0,0,DateTimeKind.Utc);
        var holon=new Holon {Id=Guid.NewGuid(),Name="plain holon",Version=7,CreatedDate=created,
            MetaData=new() {{"score",8181},{"nested",new Dictionary<string,object> {{"value",text}}}}};
        var avatar=new Avatar {Id=Guid.NewGuid(),Username="plain-avatar",Version=7,CreatedDate=created,VerificationToken=secret,
            AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),MetaData=holon.MetaData};
        using(var seed=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await seed.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await seed.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
            (await seed.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        }
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var groups=new ISearchGroupBase[] {
            new SearchNumberGroup {Number=7,NumberOperator=SearchOperatorType.EqualTo,HolonSearchParams=new SearchHolonParams {SearchAllFields=true},AvatarSearchParams=new AvatarMask {SearchAllFields=true}},
            new SearchDateGroup {Date=created,DateOperator=SearchOperatorType.EqualTo,HolonSearchParams=new SearchHolonParams {SearchAllFields=true},AvatarSearchParams=new AvatarMask {SearchAllFields=true}},
            new SearchNumberGroup {Number=8181,NumberOperator=SearchOperatorType.EqualTo,HolonSearchParams=new SearchHolonParams {MetaData=true,MetaDataKey="score"},AvatarSearchParams=new AvatarMask {MetaData=true}},
            new SearchTextGroup {SearchQuery=text,HolonSearchParams=new SearchHolonParams {MetaData=true,MetaDataKey="nested"},AvatarSearchParams=new AvatarMask {MetaData=true}},
            new SearchTextGroup {SearchQuery=avatar.Id.ToString(),HolonSearchParams=new SearchHolonParams(),AvatarSearchParams=new AvatarMask {AvatarId=true}},
            new SearchTextGroup {SearchQuery=secret,HolonSearchParams=new SearchHolonParams {SearchAllFields=true},AvatarSearchParams=new AvatarMask {SearchAllFields=true}}};
        for(var index=0;index<groups.Length;index++)
        {
            var group=groups[index];group.SearchHolons=true;group.SearchAvatars=true;group.HolonType=HolonType.All;
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {group}};
            var result=synchronous ? provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
            result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(index<4 ? new[] {holon.Id}:Array.Empty<Guid>(),"group {0}",index);
            result.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(index<5 ? new[] {avatar.Id}:Array.Empty<Guid>(),"group {0}",index);
        }
    }
}
