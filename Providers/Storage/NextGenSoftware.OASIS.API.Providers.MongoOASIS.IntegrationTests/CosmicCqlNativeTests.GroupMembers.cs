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

    public sealed class ProjectedGroupElement
    {
        public static int ConstructorCalls;
        public string Name {get;}
        public ProjectedGroupElement(string name){ConstructorCalls++;Name=name;}
    }

    [Fact]
    public Task NativeProjectedGroupingCollectionsAreCompleteOrderedAndBounded()
        =>WithMongoGraph(async fixture=>
        {
            var manager=Manager(fixture.Provider);
            var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result);
            var root=CosmicLinq.Create(manager,fixture.Provider,source).Result;
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
            var bounded=CosmicLinq.Create(manager,fixture.Provider,source,maximumResults:1).Result
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
