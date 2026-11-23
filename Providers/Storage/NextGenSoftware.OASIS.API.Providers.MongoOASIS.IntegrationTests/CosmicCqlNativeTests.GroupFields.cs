using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
    public Task NativeGroupJoinProjectsDeclaredOuterFieldsAndCompleteMatches()
        =>WithMongoGraph(async fixture=>
        {
            var manager=Manager(fixture.Provider);
            var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result);
            var root=CosmicLinq.Create(manager,fixture.Provider,source).Result;
            var left=root.Where(h=>h.Name.StartsWith("L-",System.StringComparison.Ordinal)).OrderBy(h=>h.Name);
            var right=root.Where(h=>h.Name.StartsWith("R-",System.StringComparison.Ordinal)).OrderByDescending(h=>h.Name);
            var query=left.GroupJoin(right,l=>l.Description,r=>r.Description,(parent,matches)=>new{Key=parent.Id,Label=parent.Name,Matches=matches});
            var read=await query.ToCosmicListAsync();
            Assert.False(read.IsError,read.Message+read.Exception);
            var rows=read.Result;
            Assert.Equal(new[]{"L-A","L-B","L-C"},rows.Select(row=>row.Label));
            Assert.NotEqual(rows[0].Key,rows[1].Key);
            foreach(var row in rows.Take(2)){Assert.Equal(new[]{"R-B","R-A"},row.Matches.Select(match=>match.Name));}
            Assert.Empty(rows[2].Matches);
            var page=await query.Skip(1).Take(1).ToCosmicListAsync();
            Assert.False(page.IsError,page.Message+page.Exception);
            Assert.Equal("L-B",Assert.Single(page.Result).Label);
            Assert.Equal(new[]{"R-B","R-A"},page.Result.Single().Matches.Select(match=>match.Name));

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
            Assert.False(flattened.IsError,flattened.Message+flattened.Exception);
            Assert.Equal(4,flattened.Result.Count);
        },configure:root=>
        {
            root.Name="L-A";root.Description="coffee";
            var children=root.Children.ToArray();
            children[0].Name="R-A";children[1].Name="R-B";
            foreach(var child in children)child.Description="coffee";
            var other=children[0].Children.Single();other.Name="L-B";other.Description="coffee";
            root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="L-C",Description="tea"});
        });
}
