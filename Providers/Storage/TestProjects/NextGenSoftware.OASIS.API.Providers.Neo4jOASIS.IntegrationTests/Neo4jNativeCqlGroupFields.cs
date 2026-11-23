using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [TestMethod]
    public Task NativeGroupJoinProjectsDeclaredOuterFieldsAndCompleteMatches()
        =>WithNativeGraph(async fixture=>
        {
            var manager=GraphManager(fixture.Provider);
            var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.Neo4jOASIS).Result);
            var root=CosmicLinq.Create(manager,fixture.Provider,source).Result;
            var left=root.Where(h=>h.Name.StartsWith("L-",System.StringComparison.Ordinal)).OrderBy(h=>h.Name);
            var right=root.Where(h=>h.Name.StartsWith("R-",System.StringComparison.Ordinal)).OrderByDescending(h=>h.Name);
            var query=left.GroupJoin(right,l=>l.Description,r=>r.Description,(parent,matches)=>new{Key=parent.Id,Label=parent.Name,Matches=matches});
            var read=await query.ToCosmicListAsync();
            Assert.IsFalse(read.IsError,read.Message+read.Exception);
            var rows=read.Result;
            CollectionAssert.AreEqual(new[]{"L-A","L-B","L-C"},rows.Select(row=>row.Label).ToArray());
            Assert.AreNotEqual(rows[0].Key,rows[1].Key);
            foreach(var row in rows.Take(2)){CollectionAssert.AreEqual(new[]{"R-B","R-A"},row.Matches.Select(match=>match.Name).ToArray());}
            Assert.AreEqual(0,rows[2].Matches.Count());
            var page=await query.Skip(1).Take(1).ToCosmicListAsync();
            Assert.IsFalse(page.IsError,page.Message+page.Exception);
            Assert.AreEqual("L-B",page.Result.Single().Label);
            CollectionAssert.AreEqual(new[]{"R-B","R-A"},page.Result.Single().Matches.Select(match=>match.Name).ToArray());

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
            Assert.IsFalse(flattened.IsError,flattened.Message+flattened.Exception);
            Assert.AreEqual(4,flattened.Result.Count);
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
