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
            (parent,matches)=>new{Count=matches.Count(match=>true),Matches=matches}));
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

            // Summaries and reselection remain on the native GroupJoin owner; no compiled application delegate.
            var summaries=left.GroupJoin(right,l=>l.Description,r=>r.Description,
                (parent,matches)=>new{Label=parent.Name,Count=matches.Count(),Total=matches.LongCount(),HasMatches=matches.Any()});
            var summaryRead=await summaries.ToCosmicListAsync();
            if(summaryRead.IsError || !summaryRead.Result.Select(row=>row.Count).SequenceEqual(new[]{2,2,0})
                || !summaryRead.Result.Select(row=>row.Total).SequenceEqual(new long[]{2,2,0})
                || !summaryRead.Result.Select(row=>row.HasMatches).SequenceEqual(new[]{true,true,false}))
                throw new System.InvalidOperationException("Native group summaries differ from complete matches: "+summaryRead.Message);
            var selected=await query.Select(row=>new{row.Label,Count=row.Matches.Count(),HasMatches=row.Matches.Any()})
                .Skip(1).Take(2).ToCosmicListAsync();
            if(selected.IsError || selected.Result.Count!=2 || selected.Result[0].Label!="L-B"
                || selected.Result[0].Count!=2 || !selected.Result[0].HasMatches
                || selected.Result[1].Label!="L-C" || selected.Result[1].Count!=0 || selected.Result[1].HasMatches)
                throw new System.InvalidOperationException("Group reselection/page lost complete or empty groups: "+selected.Message);
            if(summaries.Count()!=3 || summaries.Skip(1).Take(1).LongCount()!=1 || !summaries.Any())
                throw new System.InvalidOperationException("Group terminal cardinality differs from parent groups.");
        var flattened=await query.SelectMany(group=>group.Matches,(group,match)=>new{Parent=group.Label,Match=match.Name}).ToCosmicListAsync();
        flattened.IsError.Should().BeFalse(flattened.Message+flattened.Exception);
        flattened.Result.Should().HaveCount(4);
    }
}
