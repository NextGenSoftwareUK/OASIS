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
    public async Task NativeLinqGroupByUsesNativeAggregatesHavingAndPaging()
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

            var grouped=left.GroupBy(h=>h.Description);
            var query=grouped.Select(group=>new{Key=group.Key,Count=group.Count(),Total=group.LongCount(),
                Min=group.Min(h=>h.Version),Max=group.Max(h=>h.Version),Sum=group.Sum(h=>h.Version)});
            var read=await query.OrderBy(row=>row.Key).ToCosmicListAsync();
            if(read.IsError || read.Result.Count!=2 || read.Result[0].Key!="coffee" || read.Result[0].Count!=2
                || read.Result[0].Total!=2 || read.Result[0].Sum!=read.Result[0].Min+read.Result[0].Max
                || read.Result[1].Key!="tea" || read.Result[1].Count!=1 || read.Result[1].Total!=1
                || read.Result[1].Sum!=read.Result[1].Min || read.Result[1].Min!=read.Result[1].Max)
                throw new System.InvalidOperationException("Native LINQ aggregate grouping differs from source groups: "+read.Message);
            var page=await query.Where(row=>row.Count>1).OrderByDescending(row=>row.Count).ThenBy(row=>row.Key)
                .Skip(0).Take(1).ToCosmicListAsync();
            if(page.IsError || page.Result.Count!=1 || page.Result[0].Key!="coffee")
                throw new System.InvalidOperationException("Native HAVING/order/page differs: "+page.Message);
            if(query.Count()!=2 || query.Where(row=>row.Count>1).Count()!=1
                || query.Skip(1).Take(1).LongCount()!=1 || !query.Any()
                || query.Where(row=>row.Count>10).FirstOrDefault()!=null)
                throw new System.InvalidOperationException("Grouped terminal cardinality differs from native complete proof.");
            var reselected=await query.Select(row=>new{row.Key,row.Count}).OrderBy(row=>row.Key).ToCosmicListAsync();
            if(reselected.IsError || reselected.Result.Count!=2 || reselected.Result[0].Count!=2)
                throw new System.InvalidOperationException("Grouped anonymous reselection differs: "+reselected.Message);
            var windowed=await left.Take(2).GroupBy(h=>h.Description)
                .Select(group=>new{Key=group.Key,Count=group.Count()}).ToCosmicListAsync();
            if(windowed.IsError || windowed.Result.Count!=1 || windowed.Result[0].Count!=2)
                throw new System.InvalidOperationException("Grouping lost its preceding source window: "+windowed.Message);
            var raw=await grouped.ToCosmicListAsync();
            if(!raw.IsError || raw.ErrorCode!="CQL_GROUPING_UNSUPPORTED")
                throw new System.InvalidOperationException("Unimplemented entity-group materialization must be explicit.");
            var refused=false;
            try{query.Where(row=>row.Count>1).Select(row=>new{row.Key});}
            catch(CosmicLinqException cause){refused=cause.Code=="CQL_GROUPING_UNSUPPORTED";}
            if(!refused)throw new System.InvalidOperationException("Grouped reselection must not discard HAVING.");
            var cancelled=await query.ToCosmicListAsync(new System.Threading.CancellationToken(true));
            if(!cancelled.IsError || cancelled.ErrorCode!="CQL_QUERY_CANCELLED")
                throw new System.InvalidOperationException("Grouped LINQ cancellation was not preserved.");

    }
}
