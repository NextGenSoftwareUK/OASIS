using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,1)] [InlineData(false,2)] [InlineData(true,1)] [InlineData(true,2)]
    public async Task HistoricalCollectionsSelectOriginalTypedRootsWithCurrentDescendants(bool synchronous,int depth)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var first=new Holon{Name="first-v1",Version=1,HolonType=HolonType.Planet};
        var second=new Holon{Name="second-v1",Version=1,HolonType=HolonType.Planet};
        var child=new Holon{Name="child-v1",Version=1,HolonType=HolonType.Moon};
        var grand=new Holon{Name="grand-v1",Version=1,HolonType=HolonType.Star};first.Children.Add(child);child.Children.Add(grand);
        (await provider.SaveHolonsAsync(new[]{first,second})).IsError.Should().BeFalse();
        first.Name="first-v2";second.Name="second-v2";child.Name="child-v2";grand.Name="grand-v2";
        foreach(var record in new[]{first,second,child,grand})record.Version=2;
        (await provider.SaveHolonsAsync(new[]{first,second})).IsError.Should().BeFalse();
        var result=synchronous ? provider.LoadAllHolons(HolonType.Planet,maxChildDepth:depth,version:1)
            :await provider.LoadAllHolonsAsync(HolonType.Planet,maxChildDepth:depth,version:1);
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Select(record=>record.Name).Should().BeEquivalentTo("first-v1","second-v1");
        var loaded=result.Result.Single(record=>record.Id==first.Id).Children.Single();loaded.Name.Should().Be("child-v2");
        loaded.Children.Count.Should().Be(depth==1 ? 0:1);
        if(depth==2)loaded.Children.Single().Name.Should().Be("grand-v2");
        (await provider.LoadAllHolonsAsync(HolonType.Planet,version:99)).Result.Should().BeEmpty();
        using var control=new DataContext(TestConnectionString);control.Database.OpenConnection();
        using var plan=control.Database.GetDbConnection().CreateCommand();
        plan.CommandText="EXPLAIN QUERY PLAN SELECT HolonId FROM CosmicHolonRevision WHERE Version=1";
        using var rows=plan.ExecuteReader();var details=new List<string>();while(rows.Read())details.Add(rows.GetString(3));
        details.Should().Contain(detail=>detail.Contains("IX_CosmicHolonRevision_CollectionVersion"));
    }
}
