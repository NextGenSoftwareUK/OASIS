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

    public sealed record GroupJoinTypedSummary(string Label,int Count,long Total,bool HasMatches);
    public sealed class GroupJoinTypedReport
    {
        public System.Guid Id {get;set;}
        public string Label {get;set;}
        public int Count {get;set;}
        public System.Collections.Generic.IEnumerable<Holon> Matches {get;set;}
    }

    [Fact]
    public async Task NativeGroupJoinMaterializesTypedScalarAndCollectionResults()
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

    }
}
