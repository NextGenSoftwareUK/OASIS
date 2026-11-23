using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicProjectedSetVisibleTupleTests
{
    [Fact]
    public void ReselectedInputsWithDifferentHiddenColumnsPublishOnlyTheirCommonValue()
    {
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var left=root.Select(h=>new{h.Id,h.Name}).Result.Select(row=>row.Name).Result;
        var right=root.Select(h=>new{h.Name,h.Version}).Result.Select(row=>row.Name).Result;
        foreach(var kind in Enum.GetValues<CosmicSetQueryKind>())
        {
            var set=CosmicProjectedSetQuery<Holon,string>.Create(left,right,kind);
            Assert.False(set.IsError,set.Message);
            Assert.Equal(2,set.Result.Left.Columns.Count);Assert.Equal(2,set.Result.Right.Columns.Count);
            Assert.Equal(typeof(string),Assert.Single(set.Result.ValueTypes));
            var output=set.Result.Output;
            Assert.Equal("Value",Assert.Single(output.Columns).Alias);
            Assert.Equal(0,Assert.Single(output.OutputColumnIndices));
            var rows=CosmicProjectionResults.Materialize(output,new[]{new CosmicProjectionRow("same")});
            Assert.False(rows.IsError,rows.Message);Assert.Equal("same",Assert.Single(rows.Result));
            var filtered=set.Result.Where(value=>value=="same");
            Assert.False(filtered.IsError,filtered.Message);
            Assert.Single(filtered.Result.Output.ResultStages);
        }
    }
}
