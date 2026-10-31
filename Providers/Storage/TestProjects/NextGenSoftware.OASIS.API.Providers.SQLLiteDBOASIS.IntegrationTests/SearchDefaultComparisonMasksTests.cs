using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    public async Task ComparisonMasksDistinguishOmittedEmptyAndSelected(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var created=new DateTime(2001,2,3,0,0,0,DateTimeKind.Utc);var modified=created.AddYears(1);
        var holon=new Holon {Id=Guid.NewGuid(),Name="owned-comparison-default",Version=7,CreatedDate=created};
        var avatar=new Avatar {Id=Guid.NewGuid(),Username="owned-comparison-default",Version=7,CreatedDate=created,
            AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)};
        (await provider.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        using(var control=new Persistence.Context.DataContext(TestConnectionString))
        {
            control.Holons.Single(row=>row.Id==holon.Id.ToString()).ModifiedDate=modified;
            control.Avatars.Single(row=>row.Id==avatar.Id.ToString()).ModifiedDate=modified;
            await control.SaveChangesAsync();
        }
        foreach(var kind in new[] {"number","created","modified"})
        foreach(var mask in new[] {"omitted","empty","selected"})
        {
            ISearchGroupBase group=kind=="number" ? new SearchNumberGroup {Number=7,NumberOperator=SearchOperatorType.EqualTo}:
                new SearchDateGroup {Date=kind=="created" ? created:modified,DateOperator=SearchOperatorType.EqualTo};
            group.SearchHolons=true;group.SearchAvatars=true;group.HolonType=HolonType.All;
            if(mask!="omitted")
            {
                group.HolonSearchParams=new SearchHolonParams {Version=mask=="selected"&&kind=="number",CreatedDate=mask=="selected"&&kind=="created",ModifiedDate=mask=="selected"&&kind=="modified"};
                group.AvatarSearchParams=new Core.Objects.Search.Avatrar.SearchAvatarParams {Version=mask=="selected"&&kind=="number",CreatedDate=mask=="selected"&&kind=="created",ModifiedDate=mask=="selected"&&kind=="modified"};
            }
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {group}};
            var result=synchronous ? provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
            result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(mask=="empty" ? Array.Empty<Guid>():new[] {holon.Id},"kind {0}, mask {1}",kind,mask);
            result.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(mask=="empty" ? Array.Empty<Guid>():new[] {avatar.Id},"kind {0}, mask {1}",kind,mask);
        }
    }
}
