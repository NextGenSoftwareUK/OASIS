using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeGroupJoinProjectsDeclaredOuterFieldsAndCompleteMatches()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var seed=new[]{
            new Holon{Name="L-A",Description="coffee",IsPublic=true},
            new Holon{Name="L-B",Description="coffee",IsPublic=true},
            new Holon{Name="L-C",Description="tea",IsPublic=true},
            new Holon{Name="R-A",Description="coffee",IsPublic=true},
            new Holon{Name="R-B",Description="coffee",IsPublic=true}};
        (await provider.SaveHolonsAsync(seed,saveChildren:false)).IsError.Should().BeFalse();
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS).Result);
        var root=CosmicLinq.Create(CosmicManager(provider,true),provider,source).Result;
        var left=root.Where(h=>h.Name.StartsWith("L-",System.StringComparison.Ordinal)).OrderBy(h=>h.Name);
        var right=root.Where(h=>h.Name.StartsWith("R-",System.StringComparison.Ordinal)).OrderByDescending(h=>h.Name);
        Assert.Throws<CosmicLinqException>(()=>left.GroupJoin(right,l=>l.Description,r=>r.Description,
            (parent,matches)=>new{Label=parent.Name.ToUpper(),Matches=matches}));
        Assert.Throws<CosmicLinqException>(()=>left.GroupJoin(right,l=>l.Description,r=>r.Description,
            (parent,matches)=>new{Count=matches.Count(),Matches=matches}));
        var query=left.GroupJoin(right,l=>l.Description,r=>r.Description,(parent,matches)=>new{Key=parent.Id,Label=parent.Name,Matches=matches});
        var read=await query.ToCosmicListAsync();
        read.IsError.Should().BeFalse(read.Message+read.Exception);
        read.Result.Select(row=>row.Label).Should().Equal("L-A","L-B","L-C");
        read.Result[0].Key.Should().NotBe(read.Result[1].Key);
        foreach(var row in read.Result.Take(2))row.Matches.Select(match=>match.Name).Should().Equal("R-B","R-A");
        read.Result[2].Matches.Should().BeEmpty();
        var page=await query.Skip(1).Take(1).ToCosmicListAsync();
        page.IsError.Should().BeFalse(page.Message+page.Exception);
        page.Result.Should().ContainSingle().Which.Label.Should().Be("L-B");
        page.Result.Single().Matches.Select(match=>match.Name).Should().Equal("R-B","R-A");
        var flattened=await query.SelectMany(group=>group.Matches,(group,match)=>new{Parent=group.Label,Match=match.Name}).ToCosmicListAsync();
        flattened.IsError.Should().BeFalse(flattened.Message+flattened.Exception);
        flattened.Result.Should().HaveCount(4);
    }
}
