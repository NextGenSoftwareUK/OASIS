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

    public sealed class ProjectedGroupElement
    {
        public static int ConstructorCalls;
        public string Name {get;}
        public ProjectedGroupElement(string name){ConstructorCalls++;Name=name;}
    }

    [Fact]
    public async Task NativeProjectedGroupingCollectionsAreCompleteOrderedAndBounded()
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
        var manager=CosmicManager(provider,true);
        var root=CosmicLinq.Create(manager,provider,source).Result;
        var left=root.Where(h=>h.Name.StartsWith("L-",System.StringComparison.Ordinal)).OrderBy(h=>h.Name);
        var right=root.Where(h=>h.Name.StartsWith("R-",System.StringComparison.Ordinal)).OrderByDescending(h=>h.Name);

            var grouped=await left.GroupBy(h=>h.Description,h=>h.Name).ToCosmicListAsync();
            if(grouped.IsError || grouped.Result.Count!=2 || grouped.Result[0].Key!="coffee"
                || !grouped.Result[0].SequenceEqual(new[]{"L-A","L-B"}) || grouped.Result[1].Key!="tea"
                || !grouped.Result[1].SequenceEqual(new[]{"L-C"}))
                throw new System.InvalidOperationException("Complete projected scalar groups differ: "+grouped.Message);
            var anonymous=await left.OrderByDescending(h=>h.Name)
                .GroupBy(h=>h.Description,h=>new{h.Name,h.Version}).ToCosmicListAsync();
            if(anonymous.IsError || anonymous.Result.Count!=2 || anonymous.Result[0].Key!="tea"
                || anonymous.Result[1].Key!="coffee"
                || !anonymous.Result[1].Select(row=>row.Name).SequenceEqual(new[]{"L-B","L-A"}))
                throw new System.InvalidOperationException("Native source/member/first-group order differs: "+anonymous.Message);
            var window=await left.Take(2).GroupBy(h=>h.Description,h=>h.Name).ToCosmicListAsync();
            if(window.IsError || window.Result.Count!=1 || !window.Result[0].SequenceEqual(new[]{"L-A","L-B"}))
                throw new System.InvalidOperationException("Projected group source window differs: "+window.Message);
            var empty=await left.Where(h=>h.Name=="missing-owned-group")
                .GroupBy(h=>h.Description,h=>h.Name).ToCosmicListAsync();
            if(empty.IsError || empty.Result.Count!=0)
                throw new System.InvalidOperationException("Empty projected grouping differs: "+empty.Message);
            ProjectedGroupElement.ConstructorCalls=0;
            var dto=await left.GroupBy(h=>h.Description,h=>new ProjectedGroupElement(h.Name)).ToCosmicListAsync();
            if(dto.IsError || dto.Result.Count!=2 || ProjectedGroupElement.ConstructorCalls!=3
                || !dto.Result[0].Select(row=>row.Name).SequenceEqual(new[]{"L-A","L-B"}))
                throw new System.InvalidOperationException("Projected DTO grouping differs: "+dto.Message);
            ProjectedGroupElement.ConstructorCalls=0;
            var bounded=CosmicLinq.Create(manager,provider,source,maximumResults:1).Result
                .Where(h=>h.Name.StartsWith("L-",System.StringComparison.Ordinal)).OrderBy(h=>h.Name);
            var limited=await bounded.GroupBy(h=>h.Description,h=>new ProjectedGroupElement(h.Name)).ToCosmicListAsync();
            if(!limited.IsError || limited.ErrorCode!="CQL_RESULT_LIMIT" || limited.Result!=null
                || ProjectedGroupElement.ConstructorCalls!=0)
                throw new System.InvalidOperationException("Group bound must reject before application construction.");
            var entities=await left.GroupBy(h=>h.Description).ToCosmicListAsync();
            if(!entities.IsError || entities.ErrorCode!="CQL_GROUPING_UNSUPPORTED")
                throw new System.InvalidOperationException("Whole-entity groups require independent snapshot admission.");
            var cancelled=await left.GroupBy(h=>h.Description,h=>h.Name)
                .ToCosmicListAsync(new System.Threading.CancellationToken(true));
            if(!cancelled.IsError || cancelled.ErrorCode!="CQL_QUERY_CANCELLED")
                throw new System.InvalidOperationException("Projected grouping cancellation differs.");

    }
}
