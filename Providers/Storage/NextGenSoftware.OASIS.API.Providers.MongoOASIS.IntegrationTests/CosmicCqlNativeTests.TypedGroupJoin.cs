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

    public sealed record GroupJoinTypedSummary(string Label,int Count,long Total,bool HasMatches);
    public sealed class GroupJoinTypedReport
    {
        public System.Guid Id {get;set;}
        public string Label {get;set;}
        public int Count {get;set;}
        public System.Collections.Generic.IEnumerable<Holon> Matches {get;set;}
    }

    [Fact]
    public Task NativeGroupJoinMaterializesTypedScalarAndCollectionResults()
        =>WithMongoGraph(async fixture=>
        {
            var manager=Manager(fixture.Provider);
            var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result);
            var root=CosmicLinq.Create(manager,fixture.Provider,source).Result;
            var left=root.Where(h=>h.Name.StartsWith("L-",System.StringComparison.Ordinal)).OrderBy(h=>h.Name);
            var right=root.Where(h=>h.Name.StartsWith("R-",System.StringComparison.Ordinal)).OrderByDescending(h=>h.Name);

            var typed=left.GroupJoin(right,l=>l.Description,r=>r.Description,
                (parent,matches)=>new GroupJoinTypedSummary(parent.Name,matches.Count(),matches.LongCount(),matches.Any()));
            var read=await typed.ToCosmicListAsync();
            if(read.IsError || read.Result.Count!=3 || read.Result[0].Label!="L-A" || read.Result[1].Label!="L-B"
                || read.Result[0].Count!=2 || read.Result[0].Total!=2 || !read.Result[0].HasMatches
                || read.Result[2].Label!="L-C" || read.Result[2].Count!=0 || read.Result[2].Total!=0 || read.Result[2].HasMatches)
                throw new System.InvalidOperationException("Typed GroupJoin construction lost complete or empty groups: "+read.Message);
            var initialized=left.GroupJoin(right,l=>l.Description,r=>r.Description,
                (parent,matches)=>new GroupJoinTypedReport{Id=parent.Id,Label=parent.Name,Count=matches.Count(),Matches=matches});
            var reports=await initialized.ToCosmicListAsync();
            if(reports.IsError || reports.Result.Count!=3 || reports.Result[0].Id==reports.Result[1].Id
                || reports.Result[1].Label!="L-B" || reports.Result[1].Count!=2
                || !reports.Result[1].Matches.Select(match=>match.Name).SequenceEqual(new[]{"R-B","R-A"})
                || reports.Result[2].Matches.Any())
                throw new System.InvalidOperationException("Member-initialized GroupJoin lost identity or ordered matches: "+reports.Message);
            var scalars=await left.GroupJoin(right,l=>l.Description,r=>r.Description,(parent,matches)=>matches.Count())
                .Select(count=>new{Count=count}).ToCosmicListAsync();
            if(scalars.IsError || !scalars.Result.Select(row=>row.Count).SequenceEqual(new[]{2,2,0}))
                throw new System.InvalidOperationException("Direct scalar GroupJoin/reselection differs: "+scalars.Message);
            var collection=await left.GroupJoin(right,l=>l.Description,r=>r.Description,(parent,matches)=>matches)
                .Skip(2).Take(1).ToCosmicListAsync();
            if(collection.IsError || collection.Result.Count!=1 || collection.Result[0].Any())
                throw new System.InvalidOperationException("Direct matching collection GroupJoin/page differs: "+collection.Message);
            var parents=await left.GroupJoin(right,l=>l.Description,r=>r.Description,(parent,matches)=>parent)
                .Skip(1).Take(1).ToCosmicListAsync();
            if(parents.IsError || parents.Result.Count!=1 || parents.Result[0].Id!=reports.Result[1].Id)
                throw new System.InvalidOperationException("Direct parent GroupJoin lost its identity: "+parents.Message);
            var refused=0;
            try{typed.Select(row=>new{row.Label});}
            catch(CosmicLinqException cause){if(cause.Code=="CQL_EXPRESSION_UNSUPPORTED")refused++;}
            try{initialized.SelectMany(row=>row.Matches,(row,match)=>new{row.Label,Match=match.Name});}
            catch(CosmicLinqException cause){if(cause.Code=="CQL_EXPRESSION_UNSUPPORTED")refused++;}
            if(refused!=2)throw new System.InvalidOperationException("Opaque DTO getters must not be inlined into grouped composition.");
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
