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
    public async Task ParentSearchRejectsReachableCycleInsteadOfReturningPartialMatches(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var text="owned-parent-cycle-"+Guid.NewGuid().ToString("N");
        var parent=new Holon {Id=Guid.NewGuid(),Name=text,Version=1};
        var child=new Holon {Id=Guid.NewGuid(),Name=text,ParentHolonId=parent.Id,Version=3};
        foreach(var record in new[] {parent,child}) (await provider.SaveHolonAsync(record,saveChildren:false)).IsError.Should().BeFalse();
        using(var control=new Persistence.Context.DataContext(TestConnectionString))
        {
            control.Holons.Single(row=>row.Id==parent.Id.ToString()).ParentHolonId=child.Id.ToString();
            await control.SaveChangesAsync();
        }
        foreach(var version in new[] {0,3})
        {
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,ParentId=parent.Id,Recursive=true,SearchGroups=new() {
                new SearchTextGroup {SearchQuery=text,SearchHolons=true,HolonType=HolonType.All,HolonSearchParams=new SearchHolonParams {Name=true}}}};
            var result=synchronous ? provider.Search(query,loadChildren:false,version:version):await provider.SearchAsync(query,loadChildren:false,version:version);
            result.IsError.Should().BeTrue();result.Exception.Should().BeOfType<System.IO.InvalidDataException>();
            result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LegacyAvatarSchemaAddsParentLinkAndPreservesItAcrossReopen(bool synchronous)
    {
        var id=Guid.NewGuid();var parent=Guid.NewGuid();
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned-parent-schema",FirstName="retained",
                AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
        }
        using(var control=new Persistence.Context.DataContext(TestConnectionString))
            await control.Database.ExecuteSqlRawAsync("ALTER TABLE Avatar DROP COLUMN ParentHolonId");
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            var active=synchronous ? provider.ActivateProvider():await provider.ActivateProviderAsync();active.IsError.Should().BeFalse(active.Message);
            var loaded=await provider.LoadAvatarAsync(id);loaded.Result.ParentHolonId.Should().Be(Guid.Empty);loaded.Result.FirstName.Should().Be("retained");
            loaded.Result.ParentHolonId=parent;
            var saved=synchronous ? provider.SaveAvatar(loaded.Result):await provider.SaveAvatarAsync(loaded.Result);saved.IsError.Should().BeFalse(saved.Message);
        }
        using(var provider=new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            var loaded=await provider.LoadAvatarAsync(id);loaded.Result.ParentHolonId.Should().Be(parent);loaded.Result.FirstName.Should().Be("retained");
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ParentSearchUsesCurrentLinksIndependentVersionsAndNoUnrelatedAvatars(bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var text="owned-parent-search-"+Guid.NewGuid().ToString("N");
        Holon Create(int version)=>new() {Id=Guid.NewGuid(),Name=text,Version=version};
        var parent=Create(1);var child=Create(2);var grand=Create(3);var outside=Create(3);
        child.ParentHolonId=parent.Id;grand.ParentHolonId=child.Id;
        foreach(var record in new[] {parent,child,grand,outside}) (await provider.SaveHolonAsync(record,saveChildren:false)).IsError.Should().BeFalse();
        var avatar=new Avatar {Id=Guid.NewGuid(),Username=text,Version=3,AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)};
        (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
        var direct=new Avatar {Id=Guid.NewGuid(),Username=text+"direct",ParentHolonId=parent.Id,Version=3,AvatarType=avatar.AvatarType};
        var linked=new Avatar {Id=Guid.NewGuid(),Username=text+"linked",ParentHolonId=child.Id,Version=3,AvatarType=avatar.AvatarType};
        foreach(var record in new[] {direct,linked})
        {
            (await provider.SaveAvatarAsync(record)).IsError.Should().BeFalse();
            (await provider.LoadAvatarAsync(record.Id)).Result.ParentHolonId.Should().Be(record.ParentHolonId);
        }
        foreach(var version in new[] {0,3})
        foreach(var recursive in new[] {false,true})
        {
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,ParentId=parent.Id,Recursive=recursive,SearchGroups=new() {
                new SearchTextGroup {SearchQuery=text,SearchHolons=true,SearchAvatars=true,HolonType=HolonType.All,
                    HolonSearchParams=new SearchHolonParams {Name=true},AvatarSearchParams=new Core.Objects.Search.Avatrar.SearchAvatarParams {Username=true}}}};
            var result=synchronous ? provider.Search(query,loadChildren:false,version:version):await provider.SearchAsync(query,loadChildren:false,version:version);
            result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);
            var expected=(recursive ? new[] {child,grand}:new[] {child}).Where(row=>version==0||row.Version==version).Select(row=>row.Id);
            result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(expected,"version {0}, recursive {1}",version,recursive);
            result.Result.SearchResultAvatars.Select(row=>row.Id).Should().BeEquivalentTo(recursive ? new[] {direct.Id,linked.Id}:new[] {direct.Id});
        }
    }
}
