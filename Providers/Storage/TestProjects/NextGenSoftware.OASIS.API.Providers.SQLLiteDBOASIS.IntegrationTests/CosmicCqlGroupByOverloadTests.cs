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
    public async Task NativeLinqGroupByElementAndResultSelectorsUseNativePlans()
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

            var scalar=left.GroupBy(h=>h.Description,h=>h.Version)
                .Select(group=>new{Key=group.Key,Count=group.Count(),Sum=group.Sum(),Min=group.Min(),Max=group.Max()});
            var values=await scalar.OrderBy(row=>row.Key).ToCosmicListAsync();
            if(values.IsError || values.Result.Count!=2 || values.Result[0].Key!="coffee" || values.Result[0].Count!=2
                || values.Result[0].Sum!=values.Result[0].Min+values.Result[0].Max
                || values.Result[1].Key!="tea" || values.Result[1].Count!=1
                || values.Result[1].Sum!=values.Result[1].Min || values.Result[1].Min!=values.Result[1].Max)
                throw new System.InvalidOperationException("Scalar element grouping differs: "+values.Message);
            var projected=await left.GroupBy(h=>h.Description,h=>new{h.Version,h.Name})
                .Select(group=>new{Key=group.Key,Total=group.Sum(row=>row.Version),Min=group.Min(row=>row.Version)})
                .OrderBy(row=>row.Key).ToCosmicListAsync();
            if(projected.IsError || projected.Result.Count!=2 || projected.Result[0].Total!=values.Result[0].Sum
                || projected.Result[1].Total!=values.Result[1].Sum || projected.Result[0].Min!=values.Result[0].Min)
                throw new System.InvalidOperationException("Anonymous element binding differs: "+projected.Message);
            var direct=await left.GroupBy(h=>h.Description,(key,members)=>new{Key=key,Count=members.Count(),Total=members.Sum(h=>h.Version)})
                .OrderBy(row=>row.Key).ToCosmicListAsync();
            if(direct.IsError || direct.Result.Count!=2 || direct.Result[0].Count!=2 || direct.Result[0].Total!=values.Result[0].Sum)
                throw new System.InvalidOperationException("Direct grouped result selector differs: "+direct.Message);
            var combined=await left.GroupBy(h=>h.Description,h=>h.Version,
                (key,members)=>new{Key=key,Total=members.Sum(),Members=members.LongCount()})
                .Where(row=>row.Members>1).OrderBy(row=>row.Key).Take(1).ToCosmicListAsync();
            if(combined.IsError || combined.Result.Count!=1 || combined.Result[0].Key!="coffee"
                || combined.Result[0].Total!=values.Result[0].Sum || combined.Result[0].Members!=2)
                throw new System.InvalidOperationException("Element/result selector HAVING page differs: "+combined.Message);
            if(left.GroupBy(h=>h.Description,h=>h).Select(group=>new{Key=group.Key,Count=group.Count()}).Count()!=2)
                throw new System.InvalidOperationException("Explicit identity element lost native group cardinality.");
            var comprehension=from entity in left
                              group entity.Version by entity.Description into members
                              select new{Key=members.Key,Total=members.Sum()};
            var syntax=await comprehension.OrderBy(row=>row.Key).ToCosmicListAsync();
            if(syntax.IsError || syntax.Result.Count!=2 || syntax.Result[0].Total!=values.Result[0].Sum)
                throw new System.InvalidOperationException("C# grouping comprehension differs: "+syntax.Message);
            var window=await left.Take(2).GroupBy(h=>h.Description,h=>h.Version,
                (key,members)=>new{Key=key,Count=members.Count()}).ToCosmicListAsync();
            if(window.IsError || window.Result.Count!=1 || window.Result[0].Key!="coffee" || window.Result[0].Count!=2)
                throw new System.InvalidOperationException("Grouped overload lost preceding source window: "+window.Message);
            var refused=0;
            try{left.GroupBy(h=>h.Description,h=>h.Name.ToUpper());}
            catch(CosmicLinqException){refused++;}
            try{left.GroupBy(h=>h.Description,System.StringComparer.Ordinal);}
            catch(CosmicLinqException){refused++;}
            if(refused!=2)throw new System.InvalidOperationException("Unadmitted grouped functions/comparers must not execute on the client.");

    }
}
